using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using Assets;
using Assets.Database;
using Assets.Language;
using Assets.RiftAssets;
using log4net;

namespace rift.data.core.shell
{
    class Program
    {
        static void Main(string[] args)
        {
            // Configure log4net
            log4net.Config.XmlConfigurator.Configure(LogManager.GetRepository(Assembly.GetEntryAssembly()), new FileInfo("app.config"));

			AssetDatabaseFactory.AssetDirectory = ConfigurationManager.AppSettings["assetDirectory"];
			AssetDatabaseFactory.AssetManifest = ConfigurationManager.AppSettings["assetFile"];

 			var manifest = AssetDatabaseFactory.Manifest;
			var db = AssetDatabaseFactory.Database;

			var englishText = new LanguageMap(db, Languages.english);
			englishText.Load();

            var sql = DBInst.inst;

            var appearanceSets = sql.getEntriesForID(7638);
       	}
    }
}
