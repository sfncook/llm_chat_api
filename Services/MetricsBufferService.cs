using System;
using System.Collections.Concurrent;

public class MetricsBufferService
{

    private readonly ConcurrentQueue<MetricEvent> buffer = new ConcurrentQueue<MetricEvent>();

    public void Duration(string _event_id, long _value, string tag = null) {
        Log(_event_id, EventType.DurationMs, _value, tag);
    }

    public void Count(string _event_id, int _value, string tag = null) {
        Log(_event_id, EventType.Count, _value, tag);
    }

    public void Inc(string _event_id, string tag = null) {
        Log(_event_id, EventType.Increment, 1, tag);
    }

    private void Log(string _event_id, EventType _type, long _value, string _tag) {
        var _event = new MetricEvent {
            id = Guid.NewGuid().ToString(),
            event_id = _event_id,
            event_type = _type,
            value = _value,
            tag = _tag
        };
        buffer.Enqueue(_event);
    }

    public bool IsEmpty() {
        return buffer.IsEmpty;
    }

    public bool TryDequeue(out MetricEvent _event)
    {
        return buffer.TryDequeue(out _event);
    }

    public enum EventType : ushort
    {
        DurationMs = 1,
        Count = 2,
        Increment = 3
    }

    public class MetricEvent {
        public string id { get; set; }
        public string event_id { get; set; }
        public EventType event_type { get; set; }
        public long value { get; set; }
        public string tag { get; set; }
    }

}