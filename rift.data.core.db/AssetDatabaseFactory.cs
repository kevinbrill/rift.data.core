using Assets.RiftAssets;
using System.IO;
using System;
using log4net;

namespace Assets
{
	public static class AssetDatabaseFactory
	{
		static readonly ILog logger = LogManager.GetLogger(typeof(AssetDatabaseFactory));

		static AssetDatabase _database;
		static Manifest _manifest;
        
		public static string AssetDirectory { get; set; }
		public static string AssetManifest { get; set; }
		public static string AssetOverrideDirectory { get; set; }

		public static AssetDatabase Database
		{
			get
			{
				if (_database == null) {
					_database = InitializeDatabase();
				}

				return _database;
			}
        }

		public static Manifest Manifest
		{
			get 
			{
				if (_manifest == null) {
					_manifest = InitializeManifest();
				}

				return _manifest;
			}
		}

		static Manifest InitializeManifest()
		{
			if (AssetDirectory == null)
                throw new Exception("Assets directory was null");

            if (AssetManifest == null)
                throw new Exception("Assets manifest was null");

            var manifestFile = GetManifestFile();

            return new Manifest(manifestFile);
		}

		static AssetDatabase InitializeDatabase()
		{
			if (AssetDirectory == null)
                throw new Exception("Assets directory was null");

            if (AssetManifest == null)
                throw new Exception("Assets manifest was null");

            return AssetProcessor.buildDatabase(Manifest, AssetDirectory, AssetOverrideDirectory);
		}

        static string GetManifestFile()
		{
			if(AssetOverrideDirectory == null) {
				return AssetManifest;
			}

            logger.Info($"Verifying overidden asset directory {AssetOverrideDirectory}");

            var manifestName = Path.GetFileName(AssetManifest);

            var overridenManifestFile = Path.Combine(AssetOverrideDirectory, Path.GetFileName(AssetManifest));

            logger.Debug($"Verifying presence of overridden manifest file {overridenManifestFile}");

			return File.Exists(overridenManifestFile) ? overridenManifestFile : AssetManifest;
		}     
    }
}
