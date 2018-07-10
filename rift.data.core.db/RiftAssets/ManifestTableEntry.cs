using System;
using System.IO;
using Assets.DatParser;

namespace Assets.RiftAssets
{
    class ManifestTableEntry
    {
        public int Offset { get; private set; }
        public int TableSize { get; private set; }
        public int Count { get; private set; }
        public int Stride { get; private set; }
        public string Name { get; private set; }

        public ManifestTableEntry(string name, BinaryReader dis)
        {
            Name = name;
            Offset = dis.readInt();
            TableSize = dis.readInt();
            Count = dis.readInt();
            Stride = dis.readInt();
        }

        override public string ToString()
        {
            int bytes = Stride * Count;
            int extra = TableSize - bytes;
            return (String.Format(
                "\t[" + Name
                        + "]\n\ttableoffset:{0}\n\ttable size in bytes:{1}(extra: {4})\n\tcount:{2}\n\tstride:{3}\n",
                Offset, TableSize, Count, Stride, extra));
        }
    }
}
