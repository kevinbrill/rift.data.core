using System.IO;
using Assets.DatParser;

namespace Assets.RiftAssets
{
	public class ManifestEntry
    {
        public string idStr { get; }
        public string filenameHashStr { get; }
        public  byte[] id { get; }
        public  byte[] filenameHash { get; }
        public  int pakOffset { get; }
        public  int compressedSize { get; }
        public  int size { get; }
        public  short pakIndex { get; }
        public  short w2 { get; }
        public  short w3 { get; }
        public  int w4 { get; }
        public  int lang { get; }
        public  byte[] shahash { get; }
        public  int unk { get; }
        public string shaStr { get; }

        public ManifestEntry(BinaryReader reader) 
        {
            // read the ID of the entry
            id = new byte[8];
		    reader.readFully(id);

		    // read the filename hash of the entry
		    filenameHash = new byte[4];
		    reader.readFully(filenameHash);
		    ArrayUtils.reverse(filenameHash);

		    // store the ID and filename hash into a map for easy lookup
		    idStr = Util.bytesToHexString(id);
		    filenameHashStr = Util.bytesToHexString(filenameHash);

		    pakOffset = reader.readInt();
		    compressedSize = reader.readInt();
		    size = reader.readInt();
		    pakIndex = reader.readShort();

		    //if (w1 > 2193)
		    //	System.out.println(w1);
		    w2 = reader.readShort();
		    w3 = reader.readShort();
		    w4 = reader.readByte();
		    lang = reader.readByte();
		    shahash = new byte[20];
		    reader.readFully(shahash);
		    unk = reader.readInt();
		    shaStr = Util.bytesToHexString(shahash);
	    }

    
    override public string ToString()
    {
        return ("[namehash]" + filenameHashStr + ":[id]:" + idStr + ":[pakoffset]" +
               ("" + pakOffset).PadLeft(10, ' ') + ":[compressedSize]" +
                ("" + compressedSize).PadLeft(10, ' ') + ":[filesize]"
                + ("" + size).PadLeft(10, ' ')
                + ":"
                + "[PAKIndex]" + ("" + pakIndex).PadLeft(4, ' ') + ":[unkw2]"
                + ("" + w2).PadLeft(6, ' ') + ":[lang]" + lang + ""
                + ":[unk]" + unk
                + ":[hash]:" + Util.bytesToHexString(shahash) + ":"
                + unk);
    }

}
}
