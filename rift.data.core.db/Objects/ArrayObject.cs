using System;
using System.Collections.Generic;
using Assets.DatParser;
using Newtonsoft.Json;
using rift.data.core.IO;

namespace rift.data.core.Objects
{
    public class ArrayObject : BaseObject<List<CObject>>
    {
        public ArrayObject(BitResult bitResult, int indent, SpecializedBinaryReader reader, int position) : base(11, new byte[0], bitResult.Data, null)
        {
            for (var i = 0; i < bitResult.Data; i++)
            {
                Parser.handleCode(this, reader, bitResult.Code, i, indent);
            }

            DataCode = position;
            Value = Members;
        }

		[JsonProperty("length")]
		public int Length => Value.Count;

		[JsonProperty("position")]
		public override int Position => index;

		public override string ToString()
        {
            return $"ArrayObject: {Value.Count} element(s)";
        }
    }
}
