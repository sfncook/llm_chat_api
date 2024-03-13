using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using static MetricsBufferService;

public class MetricsBackgroundService : BackgroundService
{

    private readonly MetricsBufferService metricsBufferService;
    private readonly MySettings mySettings;
    private readonly Container metricsContainer;

    public MetricsBackgroundService(
        MetricsBufferService metricsBufferService,
        IOptions<MySettings> mySettings,
        CosmosDbService cosmosDbService
    )
    {
        this.metricsBufferService = metricsBufferService;
        this.mySettings = mySettings.Value;
        metricsContainer = cosmosDbService.MetricsContainer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (mySettings.WriteMetricsCosmos && !stoppingToken.IsCancellationRequested)
        {
            Console.WriteLine("Poll metrics");
            try{
                if(!metricsBufferService.IsEmpty()){
                    Console.WriteLine("Sending metrics");
                    Dictionary<string, List<MetricEvent>> metricsByEventId = new Dictionary<string, List<MetricEvent>>();

                    while(metricsBufferService.TryDequeue(out MetricEvent _event)) {
                        if(!metricsByEventId.ContainsKey(_event.event_id)) {
                            metricsByEventId[_event.event_id] = new List<MetricEvent>();
                        }
                        metricsByEventId[_event.event_id].Add(_event);
                    }

                    Stopwatch stopwatch = Stopwatch.StartNew();
                    List<Task<TransactionalBatchResponse>> tasks = new List<Task<TransactionalBatchResponse>>();
                    foreach (KeyValuePair<string, List<MetricEvent>> kvp in metricsByEventId)
                    {
                        string event_id = kvp.Key;
                        List<MetricEvent> events = kvp.Value;
                        if (mySettings.WriteMetricsCosmos)
                        {
                            PartitionKey partitionKey = new PartitionKey(event_id);
                            TransactionalBatch batch = metricsContainer.CreateTransactionalBatch(partitionKey);
                            events.ForEach(_event => {
                                batch.CreateItem(_event);
                            });
                            tasks.Add(batch.ExecuteAsync());
                        }
                    }
                    await Task.WhenAll(tasks);
                    foreach (var task in tasks)
                    {
                        using TransactionalBatchResponse response = await task;
                        Console.WriteLine($"response.StatusCode:{response.StatusCode}");
                    }
                    stopwatch.Stop();
                    Console.WriteLine($"METRICS (COSMOS) Metrics write: {stopwatch.ElapsedMilliseconds} ms");
                } else {
                    // When there is no message waiting then sleep between polls
                    await Task.Delay(mySettings.MetricsBufferPollRateMs, stoppingToken);
                }
            } catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }
    }

}