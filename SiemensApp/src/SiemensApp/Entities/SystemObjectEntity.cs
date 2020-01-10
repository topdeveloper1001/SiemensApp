using SiemensApp.Domain;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
namespace SiemensApp.Entities
{
    [Table("SystemObjects")]
    public class SystemObjectEntity
    {
        [Key]
        public int Id { get; set; }

        public int? ParentId { get; set; }

        [ForeignKey(nameof(ParentId))]
        public SystemObjectEntity Parent { get; set; }

        [InverseProperty(nameof(Parent))]
        public List<SystemObjectEntity> Children { get; set; } = new List<SystemObjectEntity>();

        public int SystemId { get; set; }

        public int ViewId { get; set; }

        public string Descriptor { get; set; }

        public string Designation { get; set; }

        public string Name { get; set; }

        public string SystemName { get; set; }

        public string ObjectId { get; set; }

        public string Attributes { get; set; }

        public string Properties { get; set; }

        public string FunctionProperties { get; set; }

        [NotMapped]
        public bool HasChildren { get; set; }

        public string DisplayName => Descriptor ?? Name ?? SystemName ?? ObjectId ?? $"Object {Id}";
    }

    public class ChildrenExistResult
    {
        public int Id { get; set; }

        public bool Exist { get; set; }
    }
}
