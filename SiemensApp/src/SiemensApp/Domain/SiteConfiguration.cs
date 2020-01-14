using System;

namespace SiemensApp.Domain
{
    public class SiteConfiguration
    {
        public Guid SiteId { get; set; }
        public string Url { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Status { get; set; }
        public int MaxThreads { get; set; }
    }
}
