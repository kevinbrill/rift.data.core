using System;
using Assets;

namespace rift.data.core.shell
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            AssetDatabaseFactory.AssetDirectory = "/Users/kevin/development/rift/assets";
            AssetDatabaseFactory.AssetManifest = "/Users/kevin/development/rift/assets64.manifest";

 			var manifest = AssetDatabaseFactory.Manifest;
			var db = AssetDatabaseFactory.Database;
        }
    }
}
