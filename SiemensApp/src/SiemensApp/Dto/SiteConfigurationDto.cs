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
            };
        }
    }
}