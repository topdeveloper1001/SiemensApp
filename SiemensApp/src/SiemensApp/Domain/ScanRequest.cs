using System;

namespace SiemensApp.Domain
{
    public class ScanRequest
    {
        public int Id { get; set; }
        public Guid SiteId { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public ScanRequestStatus Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int NumberOfPoints { get; set; }
        public string Messages { get; set; }
        public string StatusString { get; set; }
    }

    public enum ScanRequestStatus
    {
        Requested = 0,
        Running = 1,
        Completed = 2,
        Failed = 3
    }
}
