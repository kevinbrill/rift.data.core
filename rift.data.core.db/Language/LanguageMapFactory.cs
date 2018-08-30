using System;
using Assets.RiftAssets;

namespace Assets.Language
{
    public static class LanguageMapFactory
    {
        static LanguageMap _languageMap;

        public static void Load(AssetDatabase assetDatabase, Languages language)
        {
            _languageMap = new LanguageMap(assetDatabase, language);
            _languageMap.Load(null);
        }

        public static LanguageMap Get()
        {
            if (_languageMap == null)
                throw new NullReferenceException($"LanguageMap is null.  Make sure to call Load before referencing Get()");

            return _languageMap;
        }
    }
}
