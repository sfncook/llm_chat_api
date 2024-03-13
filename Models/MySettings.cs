public class MySettings
{
    // Cosmos DB
    public string CosmosUri { get; set; }
    public string TableCompanies { get; set; }
    public string TableConversations { get; set; }
    public string TableMessages { get; set; }
    public string TableRefinements { get; set; }
    public string TableUsers { get; set; }
    public string TableLogs { get; set; }
    public string TableMetrics { get; set; }

    // Queues
    public string QueueEmails { get; set; }

    // Logging
    public bool PrintLogsStdOut { get; set; }
    public bool WriteLogsCosmos { get; set; }
    public int LogBufferPollRateMs { get; set; } = 10000;
    public string LogLevel { get; set; }

    // Metrics
    public bool WriteMetricsCosmos { get; set; }
    public int MetricsBufferPollRateMs { get; set; }
}
