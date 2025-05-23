using System;
using System.Collections.Generic;

namespace QuackLock.Core.Events
{
    public class DetectionEvent
    {
        public string Source { get; set; } = "";
        public string EventType { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object>? Data { get; set; }
        public EventSeverity Severity { get; set; } = EventSeverity.Info;
    }
}
