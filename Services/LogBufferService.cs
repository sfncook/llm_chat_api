using System;
using System.Collections.Concurrent;

public class LogBufferService
{

    private readonly ConcurrentQueue<LogMsg> buffer = new ConcurrentQueue<LogMsg>();

    public void Debug(string _msg) {
        Log(LogLevel.Debug, _msg);
    }

    public void Info(string _msg) {
        Log(LogLevel.Info, _msg);
    }

    public void Warn(string _msg) {
        Log(LogLevel.Warn, _msg);
    }

    public void Error(string _msg) {
        Log(LogLevel.Error, _msg);
    }

    public void Log(LogLevel level, string message) {
        var logMsg = new LogMsg {
            id = Guid.NewGuid().ToString(),
            time = DateTime.UtcNow.ToString("o"),
            level = level,
            msg = message
        };
        buffer.Enqueue(logMsg);
    }


    public bool IsEmpty() {
        return buffer.IsEmpty;
    }

    public bool TryDequeue(out LogMsg logMsg)
    {
        return buffer.TryDequeue(out logMsg);
    }

    public static String LogLevelToString(LogLevel level)
    {
        switch(level) {
            case LogLevel.Debug:
                return "Debug";
            case LogLevel.Info:
                return "Info";
            case LogLevel.Warn:
                return "Warn";
            case LogLevel.Error:
                return "Error";
            case LogLevel.Off:
                return "Off";
            default:
                return "Invalid LogLevel";
        }
    }

    public static LogLevel LogLevelFromString(string levelStr)
    {
        switch(levelStr) {
            case "Debug":
                return LogLevel.Debug;
            case "Info":
                return LogLevel.Info;
            case "Warn":
                return LogLevel.Warn;
            case "Error":
                return LogLevel.Error;
            case "Off":
                return LogLevel.Off;
            default:
                return LogLevel.Error;
        }
    }

    public enum LogLevel : ushort
    {
        Debug = 10,
        Info = 20,
        Warn = 30,
        Error = 40,
        Off = 9999
    }

    public class LogMsg {
        public string id { get; set; }
        public string time { get; set; }
        public LogLevel level { get; set; }
        public string levelStr => level.ToString();
        public string msg { get; set; }
    }

}