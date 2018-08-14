using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Assets.DatParser
{
	public class CObject
   	{
		public List<CObject> Members = new List<CObject>(10);

		// index of this member in it's parent
		internal int index;

		CObjectConverter _converter;

		public CObject Parent { get; set; }
		public int Type { get; set; }
		public int DataCode { get; set; }
		public byte[] Data { get; set; }

		public System.Object convert()
        {
            try
            {
                return _converter.convert(this);
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public CObject(int type, byte[] data, int datacode, CObjectConverter convertor)
        {
            Type = type;
            DataCode = datacode;
            index = datacode;
			_converter = convertor;
			Data = data.Length == 0 ? null : data;
        }

        public CObject(int type, MemoryStream data, int datacode, CObjectConverter convertor)
        {
            Type = type;
            DataCode = datacode;
            index = datacode;
			_converter = convertor;
            data.Seek(0, SeekOrigin.Begin);
			Data = data.ToArray();
        }

        public CObject[] MembersArray
        {
            get
            {
                var upperBound = Members.Max(x => x.index) + 1;
                var array = new CObject[upperBound];

                foreach(var obj in Members) 
                {
                    array[index] = obj;
                }

                return array;
            }
        }

        public Dictionary<int, CObject> asDict()
        {
            if (Type != 12)
                throw new Exception("datatype[" + Type + "] is not dictionary type 12:" + this);
            Dictionary<int, CObject> dict = new Dictionary<int, CObject>();
            for (int i = 0; i < Members.Count; i+=2)
            {
                int a = getIntMember(i);
                CObject b = getMember(i + 1);
                dict[a] = b;

            }
            return dict;
        }

        internal float getFloatMember(int i, float defaultVal)
        {
            CObject member = getMember(i);
            if (member == null)
                return defaultVal;
            object o = member.convert();
            if (o is float)
                return (float)o;

            return (float)CFloatConvertor.inst.convert(getMember(i));
        }
        
        internal Vector3 getVector3Member(int i)
        {
            return getMember(i).readVec3();
        }

        internal string getStringMember(int i)
        {
            CObject member = getMember(i);
            if (member == null)
                return "";
            object o = member.convert();
            if (o is string)
                return (string)o;
            return (string)CStringConvertor.inst.convert(getMember(i));
        }

        public bool getBoolMember(int i, bool defValue)
        {
            if (!hasMember(i))
                return defValue;
            CObject member = getMember(i);
            object o = member.convert();
            if (o is bool)
                return (bool)o;

            return (bool)CBooleanConvertor.inst.convert(getMember(i)); 

        }
        public int getIntMember(int i)
        {
            CObject member = getMember(i);
            object o = member.convert();
            if (o is int)
                return (int)o;
            if (o is long)
                return (int)((long)o);


            return (int)CIntConvertor.inst.convert(getMember(i));
        }

        public void AddMember(CObject obj, int index)
        {
            Members[index] = obj;
            obj.Parent = this;
        }

        public void addMember(CObject newObj)
        {
            Members.Add(newObj);
            newObj.Parent = this;
        }

        public override String ToString()
        {
            switch (Type)
            {
                case 10:
                case 11:
                    return "array: elements:" + DataCode;

            }
            return "obj: " + Type;
        }
        public bool hasMember(int index)
        {
            return getMember(index) != null;
        }
        public CObject getMember(int index)
        {
            for (int i = 0; i < Members.Count; i++)
                if (Members[i].index == index)
                    return Members[i];
            return null;
        }

        public CObject get(int i)
        {
            return Members[i];
        }

        internal void hintCapacity(int count)
        {
            this.Members.Capacity = count;
        }
        
        public Quaternion readQuat()
        {
            CObject cObject = this;
            if (cObject.Members.Count != 4)
                throw new Exception("Not arrary of 4 was ary of :" + cObject.Members.Count);
            CFloatConvertor conv = CFloatConvertor.inst;
            float a = (float)conv.convert(cObject.Members[0]);
            float b = (float)conv.convert(cObject.Members[1]);
            float c = (float)conv.convert(cObject.Members[2]);
            float d = (float)conv.convert(cObject.Members[3]);
            return new Quaternion(a, b, c, d);
        }

        

        public Vector3 readVec3()
        {
            CObject cObject = this;
            if (cObject.Members.Count != 3)
                throw new Exception("Not arrary of 3 was ary of :" + cObject.Members.Count);
            CFloatConvertor conv = CFloatConvertor.inst;
            try
            {
                return new Vector3((float)conv.convert(cObject.Members[0]), (float)conv.convert(cObject.Members[1]),
                       (float)conv.convert(cObject.Members[2]));
            }
            catch (Exception e)
            {
                return new Vector3();
            }
        }
    }
}
