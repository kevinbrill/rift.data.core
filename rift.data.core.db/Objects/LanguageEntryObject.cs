using System;
using Assets.Language;
using rift.data.core.IO;

namespace rift.data.core.Objects
{
    public class LanguageEntryObject : BaseObject<LanguageEntry>
    {
        public LanguageEntryObject(int textEntryId, int extraData) : base(7703, new byte[0], extraData, null)
        {
            var entry = LanguageMapFactory.Get().Entries[textEntryId];

            Value = entry;
        }
    }
}
