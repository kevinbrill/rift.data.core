using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Assets;
using Assets.Database;
using Assets.DatParser;
using Assets.Language;
using log4net;
using Newtonsoft.Json;
using rift.data.core.Model;
using rift.data.core.Objects;

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

            LanguageMapFactory.Load(db, Languages.english);

            var factory = new TelaraDbFactory { AssetDatabase = db, LanguageMap = LanguageMapFactory.Get() };

			await factory.Load();

			var repo = new TelaraDbSqliteRepository();

            var dataModel = new DataModel("/Users/kevin/Desktop/rift_datamodel.txt");
            Parser.DataModel = dataModel;
            ObjectFactory.DataModel = dataModel;

            var entries = repo.GetEntriesForId(10850); // (3004); //(7710);

			var entry = entries.First().Object;

			//var json = entry.ToJson();

            //using (var writer = new StreamWriter("/Users/kevin/Desktop/rift_datamodel.json"))
            //{
            //    dataModel.ToJson(writer);
            //}

			using(var writer = new StreamWriter("/Users/kevin/Desktop/object.json"))
			{
                //writer.Write(JsonConvert.SerializeObject(entries.Select(e => e.Object), Formatting.Indented));

                writer.Write(entry.ToJson());
			}

			//using(var writer = new StreamWriter("/Users/kevin/Desktop/english.json"))
			//{
			//	englishText.ToJson(writer);
			//}
		}
    }
}
