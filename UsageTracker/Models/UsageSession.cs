using System;

namespace UsageTracker.Models
{
    public class UsageSession
    {
        public int Id { get; set; }
        public string SolutionName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationSeconds { get; set; }
        public int DebugCount { get; set; }

        public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);
    }
}