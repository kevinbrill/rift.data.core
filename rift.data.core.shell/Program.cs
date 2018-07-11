using System;
using System.Configuration;
using Assets;
using Assets.Database;
using Assets.Language;
using Assets.RiftAssets;

namespace rift.data.core.shell
{
    class Program
    {
        static void Main(string[] args)
        {
			AssetDatabaseFactory.AssetDirectory = ConfigurationManager.AppSettings["assetDirectory"];
			AssetDatabaseFactory.AssetManifest = ConfigurationManager.AppSettings["assetFile"];

 			var manifest = AssetDatabaseFactory.Manifest;
			var db = AssetDatabaseFactory.Database;

			var englishText = new LanguageMap(db, Languages.english);
			englishText.Load();
       	}
    }
}
