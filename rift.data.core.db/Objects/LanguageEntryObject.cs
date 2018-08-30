using System;
using System.Linq;
using Assets.DatParser;
using Assets.Language;
using rift.data.core.IO;
using rift.data.core.Objects.Primitives;

namespace rift.data.core.Objects
{
    public class LanguageEntryObject : BaseObject<LanguageEntry>
    {
        public LanguageEntryObject(CObject genericObject, int extraData) : base(7703, new byte[0], extraData, null)
        {
            var integerObject = genericObject.Members.FirstOrDefault(x => x is IntegerObject) as IntegerObject;

            var entities = LanguageMapFactory.Get().Entries;

            Value = integerObject == null ? 
                        null : 
                        entities[integerObject.Value];
        }
    }
}
