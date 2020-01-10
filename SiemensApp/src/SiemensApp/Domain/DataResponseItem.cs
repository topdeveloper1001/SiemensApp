using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SiemensApp.Domain
{
    public class DataItem
    {
        public int SystemId { get; set; }

        public int ViewId { get; set; }

        public string Descriptor { get; set; }

        public string Designation { get; set; }

        public string Name { get; set; }

        public string SystemName { get; set; }

        public string ObjectId { get; set; }

        [JsonProperty("_links")]
        public List<DataLink> Links = new List<DataLink>();

        public JObject Attributes { get; set; }

        public JArray Properties { get; set; }

        public JArray FunctionProperties { get; set; }

        [JsonIgnore]
        public List<DataItem> ChildrenItems = new List<DataItem>();

    }

    public class DataLink
    {
        public string Rel { get; set; }

        public string Href { get; set; }

        public bool IsTemplated { get; set; }
    }

    public class PropertyValueResponse
    {
        public string DataType { get; set; }

        public PropertyValueObject Value { get; set; }

        public string OriginalObjectOrPropertyId { get; set; }

        public string ObjectId { get; set; }

        public string PropertyName { get; set; }

        public string AttributeId { get; set; }

        public int ErrorCode { get; set; }

        public bool IsArray { get; set; }
    }

    public class PropertyValueObject
    {
        public string Value { get; set; }

        public string Quality { get; set; }

        public bool QualityGood { get; set; }

        public string Timestamp { get; set; }
    }

    public class PropertyResponse
    {
        public int ErrorCode { get; set; }
        public string ObjectId { get; set; }
        public List<PropertyObject> Properties { get; set; } = new List<PropertyObject>();
    }

    public class PropertyObject
    {
        public int Order { get; set; }
        public string PropertyName { get; set; }
        public string Descriptor { get; set; }
        public string Type { get; set; }
        public int Usage { get; set; }
        public string UnitDescriptor { get; set; }
        public int UnitId { get; set; }
        public int Resolution { get; set; }
        public bool PropertyAbsent { get; set; }
        public bool IsArray { get; set; }
    }

}
