﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Assets.DatParser;
using Assets.RiftAssets;
using log4net;
using Microsoft.Data.Sqlite;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Assets.Database
{
	public delegate void ProgressCallback(string message);
    public delegate void LoadedCallback(DB db);
    public static class DBInst
    {
		static readonly ILog logger = LogManager.GetLogger(typeof(DBInst));

        private static System.Threading.Thread loadThread;
        static object lockObj = new object();

        public static CObject toObj(this DB db, long ds, long key)
        {
            entry e = db.getEntry(ds, key);
            MemoryStream str = new MemoryStream(e.decompressedData);
            return Parser.processStreamObject(str);
        }

        public static bool loaded = false;
        public static bool loading {  get { return loadThread.IsAlive; } }
        static private DBLang langdb;
        static private DB db;
        public static DB inst   { get {
                while (db == null) ;
                lock (lockObj)
                {
                    return db;
                }
            }
        }

        public static DBLang lang_inst
        {
            get
            {
                lock (lockObj)
                {
                    return langdb;
                }
            }
        }


        public static event ProgressCallback progress = delegate { };
        public static event LoadedCallback isloadedCallback = delegate { };

        /**
         * If the db is loaded, call the callback immediately, otherwise, register the callback
         */ 
        public static void loadOrCallback(LoadedCallback loadCallback)
        {
            lock (isloadedCallback)
            {
                if (db != null)
                    loadCallback.Invoke(db);
                else
                    isloadedCallback += loadCallback;
            }
        }

        static DBInst()
        {
            loadThread = new System.Threading.Thread(new System.Threading.ThreadStart(loadDatabase_));
            loadThread.Start();
        }

        private static void cleanupOld()
        {
            string tempPath = System.IO.Path.GetTempPath();
            foreach(string file in Directory.GetFiles(tempPath, "telaraflydb*"))
            {
                try
                {
                    logger.Debug("delete old DB file:" + file);
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    logger.Debug("\tUnable to delete file:" + file + " :" + ex.Message);
                }
            }
        }
        
        private static void loadDatabase_()
        {
            try
            {
                //Profiler.BeginSample("loadDatabase_");
                lock (lockObj)
                {
                    logger.Debug("get asset database inst");
					AssetDatabase adb = AssetDatabaseFactory.Database;
                    logger.Debug("get telara.db");
                    logger.Debug("done get telara.db");

                    //string entryHash = Util.bytesToHexString(ae.hash);

                    string namePath = System.IO.Path.GetTempPath() + "telaraflydb" + Guid.NewGuid();
                    string compressedSQLDB = namePath + ".db3";
                    //string dbHashname = namePath + ".hash";
                    cleanupOld();
                    AppDomain.CurrentDomain.ProcessExit += (s, e) =>
                    {
                        if (origc != null)
                        {
                            origc.Close();
                            origc = null;
                        }
                    };
                    AppDomain.CurrentDomain.DomainUnload += (s, e) =>
                    {
                        logger.Debug("domain unloading");
                        if (origc != null)
                        {
                            origc.Close();
                            origc = null;
                        }

                    };
                    DB db = readDB(adb.extractUsingFilename("telara.db"), compressedSQLDB, (s) => { progress.Invoke("[Phase 1 of 2]" + s); });

                    progress.Invoke("[Phase 1 of 2] Reading language database");
                    langdb = new DBLang(adb, "english", (s) => { progress.Invoke("[Phase 1 of 2]" + s); });

                    DBInst.db = db;
                    if (db != null)
                    {
                        loaded = true;
                        lock (isloadedCallback)
                        {
                            isloadedCallback.Invoke(db);
                        }
                        logger.Debug("db and lang done");
                        progress.Invoke("");
                    }

                }
            }
            catch (Exception ex)
            {
				logger.Error(ex);
                progress.Invoke("Error while loading:" + ex);
                throw ex;
            }
            finally
            {
                //Profiler.EndSample();
            }
        }
        private static DB readDB(byte[] telaraDBData, string outSQLDb, Action<String> progress)
        {
            logger.Debug("get new DB");

            DB db = new DB();

            try
            {
                byte[] key = System.Convert.FromBase64String("IoooW3zsQgm22XaVQ0YONAKehPyJqEyaoQ7sEqf1XDc=");

                BinaryReader reader = new BinaryReader(new MemoryStream(telaraDBData));
                logger.Debug("get page size");
                reader.BaseStream.Seek(16, SeekOrigin.Begin);
                UInt16 pageSize = (UInt16)IPAddress.NetworkToHostOrder(reader.readShort());
                logger.Debug("go page size:" + pageSize);

                reader.BaseStream.Seek(0, SeekOrigin.Begin);

                MemoryStream decryptedStream = new MemoryStream(telaraDBData.Length);

                int pageCount = telaraDBData.Length / pageSize;
                for (int i = 1; i < pageCount + 1; i++)
                {
                    byte[] iv = getIV(i);
                    BufferedBlockCipher cipher = new BufferedBlockCipher(new OfbBlockCipher(new AesEngine(), 128));
                    ICipherParameters cparams = new ParametersWithIV(new KeyParameter(key), iv);
                    cipher.Init(false, cparams);

                    byte[] bdata = reader.ReadBytes(pageSize);
                    byte[] ddata = new byte[pageSize];
                    cipher.ProcessBytes(bdata, 0, bdata.Length, ddata, 0);
                    // bytes 16-23 on the first page are NOT encrypted, so we need to replace them once we decrypt the page
                    if (i == 1)
                        for (int x = 16; x <= 23; x++)
                            ddata[x] = bdata[x];
                    decryptedStream.Write(ddata, 0, ddata.Length);
                    progress.Invoke("Decoding db " + i + "/" + pageCount);
                }
                decryptedStream.Seek(0, SeekOrigin.Begin);

                File.WriteAllBytes(outSQLDb, decryptedStream.ToArray());
                processSQL(db, outSQLDb,  progress);
                logger.Debug("finished processing");
            }
            catch(Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            return db;
        }

        private static byte[] getIV(int i)
        {
            byte[] iv = new byte[16];
            MemoryStream str = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(str))
            {
                writer.Write((long)i);
                writer.Write(0L);
                writer.Flush();
                str.Seek(0, SeekOrigin.Begin);
                return str.ToArray();
            }
        }
        static SqliteConnection origc;

        static Dictionary<long, HuffmanReader> dsHuffmanreaders = new Dictionary<long, HuffmanReader>();

        private static byte[] getEntry(long id, long key)
        {
            string datasetQ = "select * from dataset where datasetId=" + id + " and datasetKey=" + key;
            using (var datasetQcmd = new SqliteCommand(datasetQ, origc))
            {
                using (SqliteDataReader reader = datasetQcmd.ExecuteReader(System.Data.CommandBehavior.Default))
                {
                    while (reader.Read())
                    {
                        byte[] compressedData = (byte[])reader.GetValue(5);

                        // cache the huffman readers to save having to compute the huffman trees every time and also we don't have to read the frequency tables every time
                        HuffmanReader huffreader;
                        if (!dsHuffmanreaders.TryGetValue(id, out huffreader))
                        {
                            huffreader = new HuffmanReader(getFreqData(origc, id));
                            dsHuffmanreaders[id] = huffreader;
                        }
                        byte[] data = getData(compressedData, huffreader);
                        return data;
                    }
                }
            }
            logger.Error("Failed to get result for " + id + ":" + key);
            return new byte[0];
        }

        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[32768];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
            }
        }
        private static byte[] getData(byte[] compressedData, byte[] freq)
        {
            HuffmanReader reader = new HuffmanReader(freq);
            return getData(compressedData, reader);
        }
        private static byte[] getData(byte[] compressedData, HuffmanReader reader)
        {
            MemoryStream mdata = new MemoryStream(compressedData);
            int uncompressedSize = RiftAssets.Util.readUnsignedLeb128_X(mdata);
            byte[] dataOutput = new byte[uncompressedSize];

            MemoryStream compressedD = new MemoryStream();
            CopyStream(mdata, compressedD);
            compressedD.Seek(0, SeekOrigin.Begin);

            byte[] newCompressed = compressedD.ToArray();

            return reader.read(newCompressed, newCompressed.Length, uncompressedSize);
        }
        
        private static void processSQL(DB db, string compressedSQLDB,  Action<String> progress)
        {

            logger.Debug("Connect.");
            origc = new SqliteConnection("URI=file:" + compressedSQLDB);
            
            origc.Open();


            string datasetQ = "select * from dataset";
            string datasetCQ = "select count(*) from dataset";
            long totalCount = 0;
            using (SqliteCommand datasetQcmd = new SqliteCommand(datasetCQ, origc))
            {
                using (SqliteDataReader reader = datasetQcmd.ExecuteReader(System.Data.CommandBehavior.Default))
                {
                    reader.Read();
                    totalCount = reader.GetInt64(0);
                }
            }
            
            using (SqliteCommand datasetQcmd = new SqliteCommand(datasetQ, origc))
            {
                using (SqliteDataReader reader = datasetQcmd.ExecuteReader(System.Data.CommandBehavior.Default))
                {
                    int i = 0;
                    while (reader.Read())
                    {
                        progress.Invoke(i + "/" + totalCount);
                        long datasetId = reader.GetInt64(0);
                        long datasetKey = reader.GetInt64(1);
                        string name = "";
                        if (!reader.IsDBNull(4))
                            name = reader.GetString(4);
                        entry e = new entry();
                        e.id = datasetId;
                        e.key = datasetKey;
                        e.name = name;
                        e.getData = getEntry;
                        db.Add(e);
                        i++;
                    }
                }
            }
        }


        static byte[] getFreqData(SqliteConnection c, long id)
        {
            try
            {
                using (SqliteCommand datasetQcmd = new SqliteCommand("select frequencies from dataset_compression where datasetId=" + id, c))
                {
                    using (SqliteDataReader reader = datasetQcmd.ExecuteReader(System.Data.CommandBehavior.SingleResult))
                    {
                        if (reader.Read())
                        {
                            var obj = reader.GetValue(0);
                            return (byte[])obj;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Debug(ex);
            }
            return new byte[0];
        }

    }
}
