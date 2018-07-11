using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Assets.Language;
using Assets.RiftAssets;
using log4net;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Assets.Database
{
    public class TelaraDbFactory
    {
        static readonly ILog logger = LogManager.GetLogger(typeof(TelaraDbFactory));


        Task loadTask;
        CancellationToken token;
        CancellationTokenSource tokenSource;

        public AssetDatabase AssetDatabase { get; set; }
        public LanguageMap LanguageMap { get; set; }

        public async Task Load()
        {
            if (loadTask != null && loadTask.Status == TaskStatus.Running)
            {
                logger.Warn("Scanner is already running");

                return;
            }

            try
            {
                tokenSource = new CancellationTokenSource();
                token = tokenSource.Token;

                // Load the database
                loadTask = await Task.Factory.StartNew(LoadDatabase, token);
            }
            catch (Exception ex)
            {
                logger.Error("An unexpected error occurred loading the database", ex);
            }
        }

        async Task LoadDatabase()
        {
            var stopwatch = new System.Diagnostics.Stopwatch();

            stopwatch.Start();

            stopwatch.Stop();

            logger.Debug($"Loaded the TelaraDB in {stopwatch.Elapsed}");
        }

        static DB ReadDatabase(byte[] telaraDBData, string outSQLDb, Action<String> progress = null)
        {
            DB db = new DB();

            try
            {
                byte[] key = Convert.FromBase64String("IoooW3zsQgm22XaVQ0YONAKehPyJqEyaoQ7sEqf1XDc=");

                BinaryReader reader = new BinaryReader(new MemoryStream(telaraDBData));
                logger.Debug("get page size");
                reader.BaseStream.Seek(16, SeekOrigin.Begin);
                UInt16 pageSize = (UInt16)IPAddress.NetworkToHostOrder(reader.ReadInt16());
                logger.Debug("go page size:" + pageSize);

                reader.BaseStream.Seek(0, SeekOrigin.Begin);

                MemoryStream decryptedStream = new MemoryStream(telaraDBData.Length);

                int pageCount = telaraDBData.Length / pageSize;
                for (int i = 1; i < pageCount + 1; i++)
                {
                    byte[] iv = GetIV(i);
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
                    progress?.Invoke("Decoding db " + i + "/" + pageCount);
                }
                decryptedStream.Seek(0, SeekOrigin.Begin);

                File.WriteAllBytes(outSQLDb, decryptedStream.ToArray());

                //processSQL(db, outSQLDb, progress);

                logger.Debug("finished processing");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
            return db;
        }

        static byte[] GetIV(int i)
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
    }
}
