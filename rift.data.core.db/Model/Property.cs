using System;
using Newtonsoft.Json;

namespace rift.data.core.Model
{
    [JsonObject]
    public class Property
    {
        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
