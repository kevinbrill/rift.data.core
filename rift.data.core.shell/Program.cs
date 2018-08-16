﻿using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Assets;
using Assets.Database;
using Assets.Language;
using log4net;

namespace rift.data.core.shell
{
    class Program
    {
        static void Main(string[] args)
        {
			Task t = MainAsync(args);
			t.Wait();
       	}

		static async Task MainAsync(string[] args)
		{
			// Configure log4net
			log4net.Config.XmlConfigurator.Configure(LogManager.GetRepository(Assembly.GetEntryAssembly()), new FileInfo("app.config"));

			AssetDatabaseFactory.AssetDirectory = ConfigurationManager.AppSettings["assetDirectory"];
			AssetDatabaseFactory.AssetManifest = ConfigurationManager.AppSettings["assetFile"];

			var manifest = AssetDatabaseFactory.Manifest;
			var db = AssetDatabaseFactory.Database;

			var englishText = new LanguageMap(db, Languages.english);
			englishText.Load();

			var factory = new TelaraDbFactory { AssetDatabase = db, LanguageMap = englishText };

			await factory.Load();

			var repo = new TelaraDbSqliteRepository();

            var entries = repo.GetEntriesForId(3004); // (7629);

			var entry = entries.First().Object;

			var json = entry.ToJson();

			using(var writer = new StreamWriter("/Users/kevin/Desktop/object.json"))
			{
				writer.Write(json);
			}

			using(var writer = new StreamWriter("/Users/kevin/Desktop/english.json"))
			{
				englishText.ToJson(writer);
			}
			//var data = entry.Data;
			//var obj = entry.Object;

			//var frequency = FrequencyLookup.GetFrequency(2204);
		}
    }
}
