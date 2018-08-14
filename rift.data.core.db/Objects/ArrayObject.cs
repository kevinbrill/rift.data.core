using System;
using System.Collections.Generic;
using Assets.DatParser;
using rift.data.core.IO;

namespace rift.data.core.Objects
{
    public class ArrayObject : BaseObject<List<CObject>>
    {
        public ArrayObject(BitResult bitResult, int indent, SpecializedBinaryReader reader) : base(11, new byte[0], bitResult.Data, null)
        {
            for (var i = 0; i < bitResult.Data; i++)
            {
                Parser.handleCode(this, reader, bitResult.Code, i, indent);
            }

            Value = Members;
        }

        public override string ToString()
        {
            return $"ArrayObject: {Value.Count} element(s)";
        }
    }
}
