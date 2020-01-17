using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using SiemensApp.Domain;

using SiemensApp.Entities;

namespace SiemensApp.Mapping
{
    public class ScanRequestMappingProfile : Profile
    {
        public ScanRequestMappingProfile()
        {
            CreateMap<ScanRequestEntity, ScanRequest>()
                .ForMember(p => p.StatusString, p => p.MapFrom(q => q.Status.ToString()));
                
        }
    }
}
