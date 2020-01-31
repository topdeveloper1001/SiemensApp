using System;

namespace SiemensApp.Domain
{
    public class Property
    {
        public int Id { get; set; }
        public Guid SiteId { get; set; }
        public string Name { get; set; }
        public bool IsFunctionProperty { get; set; }
    }

}
