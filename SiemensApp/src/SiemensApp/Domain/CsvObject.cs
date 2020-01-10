using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SiemensApp.Entities;
namespace SiemensApp.Domain
{
    public class CsvObject : AttributesObject
    {
        public int Id { get; set; }

        public int? ParentId { get; set; }

        public int SystemId { get; set; }

        public int ViewId { get; set; }

        public string Descriptor { get; set; }

        public string Designation { get; set; }

        public string Name { get; set; }

        public string SystemName { get; set; }

        public string UnitDescriptor { get; set; }

        public string FunctionProperties { get; set; }

        public static CsvObject Create(SystemObjectEntity systemObject, AttributesObject attrObject)
        {
            return new CsvObject
            {
                Id = systemObject.Id,
                ParentId = systemObject.ParentId,
                SystemId = systemObject.SystemId,
                ViewId = systemObject.ViewId,
                Descriptor = systemObject.Descriptor,
                Designation = systemObject.Designation,
                Name = systemObject.Name,
                SystemName = systemObject.SystemName,
                ObjectId = systemObject.ObjectId,
                DefaultProperty = attrObject.DefaultProperty,
                DisciplineDescriptor = attrObject.DisciplineDescriptor,
                DisciplineId = attrObject.DisciplineId,
                FunctionDefaultProperty = attrObject.FunctionDefaultProperty,
                FunctionName = attrObject.FunctionName,
                ManagedType = attrObject.ManagedType,
                ManagedTypeName = attrObject.ManagedTypeName,
                SubDisciplineDescriptor = attrObject.SubDisciplineDescriptor,
                SubDisciplineId = attrObject.SubDisciplineId,
                SubTypeDescriptor = attrObject.SubTypeDescriptor,
                SubTypeId = attrObject.SubTypeId,
                TypeDescriptor = attrObject.TypeDescriptor,
                TypeId = attrObject.TypeId
            };
        }
    }
}
