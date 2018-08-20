using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace rift.data.core.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Class
    {
        public Class()
        {
            Properties = new List<Property>();
        }

        public Class(int code, string name) : this()
        {
            Code = code;
            Name = name;
        }

        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("properties")]
        public List<Property> Properties { get; set; }
    }
}
