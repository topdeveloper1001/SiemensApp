using SiemensApp.Domain;
using System;

namespace SiemensApp.Dto
{
    public class ScanRequestDto
    {
        public Guid SiteId { get; set; }
        public static ScanRequest MapTo(ScanRequestDto model)
        {
            if (model == null)
                return null;

            return new ScanRequest()
            {
                Id = 0,
                SiteId = model.SiteId,
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Status = ScanRequestStatus.Requested,
                NumberOfPoints = 0
            };
        }
        public static ScanRequest Create(Guid siteId)
        {
            return new ScanRequest()
            {
                Id = 0,
                SiteId = siteId,
                CreatedBy = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Status = ScanRequestStatus.Requested,
                NumberOfPoints = 0
            };
        }
    }
}