using SiemensApp.Domain;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SiemensApp.Entities
{
    [Table("Properties")]
    public class PropertyEntity
    {
        [Key]
        public int Id { get; set; }
        public Guid SiteId { get; set; }
        public string Name { get; set; }
        public bool IsFunctionProperty { get; set; }

        public static PropertyEntity MapFrom(Property model)
        {
            if (model == null)
                return null;

            return new PropertyEntity()
            {
                Id = model.Id,
                SiteId = model.SiteId,
                Name = model.Name,
                IsFunctionProperty = model.IsFunctionProperty
            };
        }
        public static Property MapTo(PropertyEntity entity)
        {
            if (entity == null)
                return null;

            return new Property()
            {
                Id = entity.Id,
                SiteId = entity.SiteId,
                Name = entity.Name,
                IsFunctionProperty = entity.IsFunctionProperty
            };
        }
    }
}
