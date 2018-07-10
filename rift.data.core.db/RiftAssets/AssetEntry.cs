using System;

namespace Assets.RiftAssets
{
    public class AssetEntry
    {
        public AssetFile File;
        public byte[] Hash;
        public int Offset;
        public int Size;
        public bool Compressed;
        public byte[] Id;
        public String StringId;
        public int SizeD;

        public AssetEntry(byte[] id, int offset, int size, int sizeD, bool compressed, byte[] hash, AssetFile file)
        {
            Id = id;
            StringId = Util.bytesToHexString(id);
            SizeD = sizeD;
            Offset = offset;
            Size = size;
            Compressed = compressed;
            Hash = hash;
            File = file;
        }

        override public string ToString()
        {
            return $"{StringId} @ {Offset}:{File.file}, Size[{Size}], Compressed[{Compressed}], SizeD:{SizeD}";
        }
    }
}
