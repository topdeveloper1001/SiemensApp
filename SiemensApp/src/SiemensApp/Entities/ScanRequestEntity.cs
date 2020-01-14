using SiemensApp.Domain;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SiemensApp.Entities
{
    [Table("ScanRequests")]
    public class ScanRequestEntity
    {
        [Key]
        public int Id { get; set; }
        public Guid SiteId { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public ScanRequestStatus Status { get; set; }

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int NumberOfPoints { get; set; }
        public string Messages { get; set; }

        public static ScanRequestEntity MapFrom(ScanRequest model)
        {
            if (model == null)
                return null;

            return new ScanRequestEntity()
            {
                Id = model.Id,
                SiteId = model.SiteId,
                CreatedBy = model.CreatedBy,
                CreatedAt = model.CreatedAt,
                Status = model.Status,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                NumberOfPoints = model.NumberOfPoints,
                Messages = model.Messages
            };
        }
        public static ScanRequest MapTo(ScanRequestEntity entity)
        {
            if (entity == null)
                return null;

            return new ScanRequest()
            {
                Id = entity.Id,
                SiteId = entity.SiteId,
                CreatedBy = entity.CreatedBy,
                CreatedAt = entity.CreatedAt,
                Status = entity.Status,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                NumberOfPoints = entity.NumberOfPoints,
                Messages = entity.Messages
            };
        }
    }
}
