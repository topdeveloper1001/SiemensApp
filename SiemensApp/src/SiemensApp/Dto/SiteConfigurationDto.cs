using SiemensApp.Domain;
using System;

namespace SiemensApp.Dto
{
    public class SiteConfigurationDto
    {
        public Guid SiteId { get; set; }
        public string Url { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int? Status { get; set; }
        public int? MaxThreads { get; set; }
        public static SiteConfigurationDto Create(Guid siteId)
        {
            return new SiteConfigurationDto()
            {
                SiteId = siteId
            };
        }
        public static SiteConfiguration MapTo(SiteConfigurationDto model)
        {
            if (model == null)
                return null;

            return new SiteConfiguration()
            {
                SiteId = model.SiteId,
                Url = model.Url,
                UserName = model.UserName,
                Password = model.Password,
                CreatedAt = DateTime.UtcNow,
                Status = model.Status.HasValue ? model.Status.Value : 0,
                MaxThreads = model.MaxThreads.HasValue ? model.MaxThreads.Value : 0
            };
        }
    }
}