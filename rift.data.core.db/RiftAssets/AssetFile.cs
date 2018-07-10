using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ionic.Zlib;

namespace Assets.RiftAssets
{
	public class AssetFile
    {
        public string file;
        public bool is64;
        Dictionary<string, AssetEntry> assets = new Dictionary<string, AssetEntry>();


        public AssetFile(string file) 
        {
		    this.file = file;
            is64 = (file.Contains("64"));
        }

        public List<AssetEntry> getEntries()
        {
            return assets.Values.ToList();
        }

        public void AddAsset(AssetEntry assetEntry)
        {
            if (assets.ContainsKey(assetEntry.StringId))
                throw new Exception(
                        "Asset key [" + assetEntry + "] already exists in db[" + assets[assetEntry.StringId] + "]");
            assets[assetEntry.StringId] = assetEntry;
        }

        public bool Contains(string id)
        {
            return assets.ContainsKey(id);
        }

        public bool Contains(byte[] id)
        {
            string strID = Util.bytesToHexString(id);
            return Contains(strID);
        }

        public AssetEntry getEntry( string strID)
        {
            return assets[strID];
        }

        public AssetEntry getEntry( byte[] id)
        {
            return getEntry(Util.bytesToHexString(id));
        }


      
        //static AssetCache cache = AssetCache.inst;
        /**
         * Attempt to extract the given assetentry into a byte array. Because the content may be compressed the returned byte array
         * may be larger than the requested max bytes.
         *
         * @param entry The entry to read
         * @param maxBytesToRead The maximum bytes to read from the source
         * @return The bytes read, may be larger than requested if the data is compressed
         */
        public byte[] extractPart(AssetEntry entry, int maxBytesToRead, Stream os,
                 bool nodecomp)
        {

            if (entry.File != this)
                throw new Exception(
                        "Extract called on wrong asset file[" + file + "] for asset:" + entry);

             byte []data = extractPart1(entry, maxBytesToRead,  nodecomp);
            if (os != null)
                os.Write(data, 0, data.Length);
            return data;
        }

        byte[] unzipCache = new byte[0];
        System.Object unzipLock = new System.Object();
        private byte[] extractPart1(AssetEntry entry, int maxBytesToRead,  bool nodecomp)
        {

            try
            {
                
                if (nodecomp || !entry.Compressed)
                {
                    using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        // if not compressed
                        
                        byte[] data = new byte[maxBytesToRead];
                        stream.Seek(entry.Offset, SeekOrigin.Begin);
                        long bytesRead = stream.Read(data, 0, maxBytesToRead);
                        if (entry.Size >= maxBytesToRead && bytesRead != maxBytesToRead)
                            throw new Exception("Not enough bytes read, expected [" + maxBytesToRead + "], got: " + bytesRead);
                      
                        return data;
                    }
                }
                else
                {
                    // COMPRESSED

                    // NOTE: entry.size doesn't indicate the size of the uncompressed data

                    // Check if we want to read all the data or only a little
                    bool readAll = maxBytesToRead >= entry.Size;

                    using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        long pos = stream.Seek(entry.Offset, SeekOrigin.Begin);

                        lock (unzipLock)
                        {
                           // Debug.Log("extract asset:" + entry);
                            //Debug.Log("decompress asset:" + entry.strID + ", size:" + entry.size + ", sizeD:" + entry.sizeD);
                            if (unzipCache.Length < entry.SizeD)
                            {
                                //Debug.Log("Increasing unzip cache size from " + unzipCache.Length + " to " + entry.sizeD);
                                unzipCache = new byte[entry.SizeD];
                            }
                            int writeIndex = 0;
                            using (ZlibStream ds = new ZlibStream(stream, Ionic.Zlib.CompressionMode.Decompress))
                            {
                                int numRead;
                                while ((numRead = ds.Read(unzipCache, writeIndex, unzipCache.Length - writeIndex)) > 0)
                                {
                                    //Debug.Log("read " + numRead + " into array at " + writeIndex + ", we tried to read " + (unzipCache.Length - writeIndex) + " bytes");
                                    writeIndex += numRead;
                                    if (!readAll && writeIndex >= maxBytesToRead)
                                        break;
                                }
                                //if (writeIndex != entry.sizeD)
                                   // Debug.LogWarning("expected to read " + entry.sizeD + " bytes, but only got " + writeIndex);
                                /*
                                //Copy the decompression stream into the output file.
                                byte[] buffer = new byte[4096];
                                while ((numRead = ds.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    totalRead += numRead;
                                    if (!readAll && totalRead >= maxBytesToRead)
                                        break;
                                    decompressed.Write(buffer, 0, numRead);

                                }
                                */
                                byte[] outData = new byte[writeIndex];
                                Array.Copy(unzipCache, outData, outData.Length);
                                return outData;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("failure in file " + file + ", @ " + entry.Offset + ", id:"
                        + entry.StringId + ", compressed?" + entry.Compressed + ", filesize:" + entry.Size + "\n\t",
                        ex);
            }
        }

        /** Attempt to extract the given assetentry into a byte array */
        public byte[] extract( AssetEntry entry)
        {
            return extractPart(entry, entry.Size, null, false);
        }

        public byte[] extract( byte[] id)
        {
            String strID = Util.bytesToHexString(id);
            return extract(assets[strID]);

        }

        public void extract( AssetEntry entry,  Stream fos)
        {
            extractPart(entry, entry.Size, fos, false);
        }

        public byte[] extractNoDecomp( AssetEntry entry)
        {
            return extractPart(entry, entry.Size, null, true);
        }
    }
}
