namespace SiemensApp.Domain
{
    public class AttributesObject
    {
        public string DefaultProperty { get; set; }

        public string ObjectId { get; set; }

        public string DisciplineDescriptor { get; set; }

        public long DisciplineId { get; set; }

        public string SubDisciplineDescriptor { get; set; }

        public long SubDisciplineId { get; set; }

        public string TypeDescriptor { get; set; }

        public long TypeId { get; set; }

        public string SubTypeDescriptor { get; set; }

        public long SubTypeId { get; set; }

        public long ManagedType { get; set; }

        public string ManagedTypeName { get; set; }

        public string FunctionName { get; set; }

        public string FunctionDefaultProperty { get; set; }
    }
    
}
