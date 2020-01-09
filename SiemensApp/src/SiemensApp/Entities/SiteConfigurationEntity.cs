﻿using SiemensApp.Domain;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SiemensApp.Entities
{
    [Table("SiteConfigurations")]
    public class SiteConfigurationEntity
    {
        [Key]
        public Guid SiteId { get; set; }

        [Required(AllowEmptyStrings = true)]
        [MaxLength(1024)]
        public string Url { get; set; }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(256)]
        public string UserName { get; set; }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(1024)]
        public string Password { get; set; }

        public static SiteConfigurationEntity MapFrom(SiteConfiguration model)
        {
            if (model == null)
                return null;

            return new SiteConfigurationEntity()
            {
                SiteId = model.SiteId,
                Url = model.Url,
                UserName = model.UserName,
                Password = model.Password,
            };
        }
    }
}
