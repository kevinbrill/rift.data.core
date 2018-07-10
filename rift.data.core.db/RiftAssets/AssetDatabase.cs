using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;

namespace Assets.RiftAssets
{
	public class AssetDatabase
    {
		static readonly ILog logger = LogManager.GetLogger(typeof(AssetDatabase));

        List<AssetFile> assets = new List<AssetFile>();
        Dictionary<string, List<AssetFile>> assetFiles;
        System.Object locko = new System.Object();
        internal string overrideDirectory;

        public AssetDatabase(Manifest manifest)
        {
            Manifest = manifest;
        }

        public Manifest Manifest { get; private set; }

        public void Add(AssetFile assetFile)
        {
            assets.Add(assetFile);
        }

        List<AssetEntry> getEntries()
        {
            List<AssetEntry> entries = new List<AssetEntry>();
            foreach (AssetFile file in assets)
            {
                entries.AddRange(file.getEntries());
            }
            return entries;
        }

        private AssetFile findAssetFileForID(byte[] id)
        {
            return FindAssetFileForId(Util.bytesToHexString(id));
        }

        private AssetFile FindAssetFileForId(string id)
        {
            if (assetFiles == null)
            {
                lock (locko)
                {
                    if (assetFiles == null)
                    {
                        assetFiles = new Dictionary<string, List<AssetFile>>();
                        foreach (AssetFile file in assets)
                        {
                            foreach (AssetEntry ae in file.getEntries())
                            {
                                List<AssetFile> list;
                                if (!assetFiles.TryGetValue(ae.StringId, out list))
                                {
                                    list = new List<AssetFile>();
                                    assetFiles.Add(ae.StringId, list);
                                }
                                list.Add(file);
                            }
                        }
                    }
                }
            }
            if (id == null)
                return null;
            List<AssetFile> holders;
            if (!assetFiles.TryGetValue(id, out holders))
                return null;

            if (holders.Count == 0)
                return null;

            if (holders.Count > 1)
            {
                try
                {
                    // we have a 32 and 64 bit one pick the right one
                    return Manifest.Is64 ?
                               holders.First(f => f.is64) :
                               holders.First(f => !f.is64);
                }
                catch (Exception ex)
                {
                    logger.Warn($"An unexpected exception occurred finding a holder: {ex.Message}", ex);
                }
            }

            return holders[0];
        }
       
        public bool filenameExists( string filename)
        {
            return Manifest.containsHash(Util.hashFileName(filename));
        }

        public enum RequestCategory
        {
            NONE,
            MAP,
            PHYSICS,
            TEXTURE,
            SHADER,
            SHADER_FORWARD,
            GEOMETRY,
            CHARACTER,
            PARTICLE,
            VFX,
            UIFONT,
            UIFLASH,
            MOVIE,
            AUDIO,
            PROPERTYCLASSDATA,
            GAMEDATA,
            ENGLISH,
            PATCH,
        }

        private AssetEntry getEntryForFileName( string filename, RequestCategory requestCategory = RequestCategory.NONE)
        {
            //logger.Debug("get entry for filename:" + filename + " with request category " + requestCategory);
            List<ManifestEntry> entries = Manifest.getEntriesForFilenameHash(Util.hashFileName(filename));

            if (entries.Count() == 0)
            {
                // lets see if the filename is actually a hash (this shouldn't happen, but whatevers)
                entries = Manifest.getEntriesForFilenameHash(filename);
                if (entries.Count() == 0)
                    throw new Exception("Filename hash not found in manifest: '" + filename + "'");
                logger.Warn("Using filename[" + filename + "] as hash");
            }

            // strip out duplicate patch paks
            entries.RemoveAll(e => {
                return Manifest.getPAKName(e.pakIndex).Contains("patch") && entries.Any(x => x != e && x.idStr.Equals(e.idStr));
            });

            // logger.Debug("found " + entries.Count() + " entries in manifest that match");
            string id = "";
            if (entries.Count() == 1)
            {
                // if there was only one result, then use it
                id = entries.First().idStr;
            }
            else
            {
                
                ManifestEntry finalEntry = null;

                // work out which one we want based on the category
                string requestStr = requestCategory.ToString().ToLower();
                //logger.Debug("multiple ids found for " + filename + ", using request category " + requestStr);
                    
                foreach (ManifestEntry entry in entries)
                {
                    //logger.Debug("[" + filename + "]: considering entry:" + entry + " :" + manifest.getPAKName(entry.pakIndex));
                    ManifestPAKFileEntry pak = Manifest.getPAK(entry.pakIndex);
                    string pakName = pak.name;
                    if (pakName.Contains(requestStr))
                    {
                        finalEntry = entry;
                        break;
                    }
                }


                if (finalEntry == null)
                {
                    // if we were still unable to break the tie
                    logger.Error("tiebreak for " + filename + " no id match");

                    // one final check on the language, if an english one exists, use that over any other non-english one
                    IEnumerable< ManifestEntry> engUni = entries.Where(e => e.lang == 0 || e.lang == 1) ;
                    // if the number of english entries is different to the number of entries, then we should choose an english one and assume it is that one
                    if (engUni.Count() > 0 && engUni.Count() != entries.Count())
                    {
                        logger.Debug("tie broken with english language choice: " + finalEntry + " :" + Manifest.getPAKName(finalEntry.pakIndex));
                        finalEntry = engUni.First();
                    }
                    else
                    {
                        // fail?
                        String str = "";
                        foreach (ManifestEntry entry in entries)
                        {
                            str += "\t" + entry + " :" + Manifest.getPAKName(entry.pakIndex) + "\n";
                        }
                        string errStr = ("Multiple ids match the filename [" + filename + "] but no request category was given, unable to determine which to return.\n" + str);
                        throw new Exception(errStr);
                    }
                }
                id = finalEntry.idStr;
                //logger.Debug("settled on entry:" + finalEntry + " :" + manifest.getPAKName(finalEntry.pakIndex));

            }
            //logger.Debug("find asset file for id:" + id);
            AssetFile assetFile = FindAssetFileForId(id);
            //logger.Debug("result:" + assetFile);
            if (assetFile == null)
                throw new Exception(
                        "Filename found in manifest but unable to locate ID[" + id + "] in assets: '" + filename
                                + "'");
            //logger.Debug("found with id:" + id);
            return assetFile.getEntry(id);
            
        }

        /** Attempt to extract the asset with the given filename */
        public byte[] extractUsingFilename( string filename, RequestCategory requestCategory = RequestCategory.NONE)
        {
            if (overrideDirectory != null)
            {
                string bfilename = Path.GetFileName(filename);
                string overriddenFilename1 = overrideDirectory + Path.DirectorySeparatorChar + bfilename;
                string overriddenFilename2 = overrideDirectory + Path.DirectorySeparatorChar + Util.hashFileName(bfilename);

                if (File.Exists(overriddenFilename1))
                {
                    logger.Debug("read override file:" + filename + " => " + overriddenFilename1);
                    return File.ReadAllBytes(overriddenFilename1);
                }
                else if (File.Exists(overriddenFilename2))
                {
                    logger.Debug("read override file: " + filename + " => " + overriddenFilename2);
                    return File.ReadAllBytes(overriddenFilename2);
                }
            }

            byte[] data = extract(getEntryForFileName(filename, requestCategory));
            if (true)
            {
                //File.WriteAllBytes(@"L:\RIFT_VIEW\data\" + filename, data);
            }
            return data;
        }

        /** Attempt to extract the asset with the given filename */
        /*
        public void extractToFilename( String filename,  String outputfilename)
        {
            try 
		{
                using (FileStream fos = new FileStream(outputfilename, FileMode.Truncate))
                {
                    using (BufferedStream bi = new BufferedStream(fos))
                    {
                        byte[] data = extract(getEntryForFileName(filename));
                        bi.Write(data, 0, data.Length);
                    }
                }
            } catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        */

        private byte[] extract( AssetEntry ae)
        {
            return ae.File.extract(ae);
        }

        private byte[] extractPart( AssetEntry ae,  int size)
        {
            AssetFile af = ae.File;
            if (af != ae.File)
                throw new Exception("Incorrect af found for asset[" + ae + "]");
            return af.extractPart(ae, size, null, false);
        }

        internal string getHash(string v)
        {
            AssetEntry ae = getEntryForFileName(v);
            return BitConverter.ToString(ae.Hash);
        }

        private void extract( AssetEntry ae,  Stream fos)
        {
            byte[] data = extract(ae);
            fos.Write(data, 0, data.Length);
            fos.Flush();
        }

        private AssetEntry getEntryForID( byte[] id)
        {
            AssetFile file = findAssetFileForID(id);
            if (file != null)
                return file.getEntry(id);
            return null;
        }

        private AssetFile getAssetFile( AssetEntry ae)
        {
            return ae.File;
        }

       
    }
}
