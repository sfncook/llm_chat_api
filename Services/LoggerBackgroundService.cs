using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using static LogBufferService;

public class LoggerBackgroundService : BackgroundService
{

    private readonly LogBufferService logBufferService;
    private readonly MySettings mySettings;
    private readonly LogLevel level;
    private readonly Container logsContainer;

    public LoggerBackgroundService(
        LogBufferService logBufferService,
        IOptions<MySettings> mySettings,
        CosmosDbService cosmosDbService
    )
    {
        this.logBufferService = logBufferService;
        this.mySettings = mySettings.Value;
        level = LogLevelFromString(this.mySettings.LogLevel);
        logsContainer = cosmosDbService.LogsContainer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try{
                if(!logBufferService.IsEmpty()){
                    Dictionary<LogLevel, List<LogMsg>> logMsgsByLevel = new Dictionary<LogLevel, List<LogMsg>>
                    {
                        { LogLevel.Debug, new List<LogMsg>() },
                        { LogLevel.Info, new List<LogMsg>() },
                        { LogLevel.Warn, new List<LogMsg>() },
                        { LogLevel.Error, new List<LogMsg>() }
                    };

                    while(logBufferService.TryDequeue(out LogMsg logMsg)) {
                        if(logMsg.level >= level) {
                            logMsgsByLevel[logMsg.level].Add(logMsg);
                        }
                    }

                    foreach (KeyValuePair<LogLevel, List<LogMsg>> kvp in logMsgsByLevel)
                    {
                        LogLevel logLevel = kvp.Key;
                        List<LogMsg> logMsgs = kvp.Value;

                        if(mySettings.PrintLogsStdOut) {
                            logMsgs.ForEach(logMsg => {
                                Console.WriteLine($"SBLog {logMsg.time} [{LogLevelToString(logMsg.level)}] {logMsg.msg}");
                            });
                        }

                        if(mySettings.WriteLogsCosmos) {
                            PartitionKey partitionKey = new PartitionKey(LogLevelToString(logLevel));
                            TransactionalBatch batch = logsContainer.CreateTransactionalBatch(partitionKey);
                            logMsgs.ForEach(logMsg => {
                                batch.CreateItem(logMsg);
                            });

                            Stopwatch stopwatch = Stopwatch.StartNew();
                            using TransactionalBatchResponse response = await batch.ExecuteAsync();
                            stopwatch.Stop();
                            Console.WriteLine($"METRICS (COSMOS) Logs write: {stopwatch.ElapsedMilliseconds} ms");
                        }
                    }
                } else {
                    // When there is no message waiting then sleep between polls
                    await Task.Delay(mySettings.LogBufferPollRateMs, stoppingToken);
                }
            } catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }
    }

}