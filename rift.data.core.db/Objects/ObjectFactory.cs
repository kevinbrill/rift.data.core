using System;
using Assets.DatParser;
using log4net;
using rift.data.core.IO;
using rift.data.core.Model;
using rift.data.core.Objects.Primitives;

namespace rift.data.core.Objects
{
    public static class ObjectFactory
    {
        static readonly ILog logger = LogManager.GetLogger(typeof(ObjectFactory));

        static ObjectFactory()
        {
            DataModel = new DataModel();
        }

        public static DataModel DataModel { get; set; }

        public static CObject Create(SpecializedBinaryReader reader, int extraData)
        {
            //  Read the class code
            int objectClassCode = reader.ReadUnsignedLeb128();

            if (objectClassCode > 0xFFFF || objectClassCode == 0)
            {
                var message = $"Data Type 10 has an out of range value code '{objectClassCode}'";

                logger.Warn(message);

                throw new ArgumentOutOfRangeException("objectClassCode", objectClassCode, message);
            }

            logger.Debug($"Creating child object of type '{objectClassCode}'");


            Class classDefinition = DataModel.Classes.ContainsKey(objectClassCode) ? DataModel.Classes[objectClassCode] : null;

            // Create the generic CObject
            CObject result = CreateCObject(reader, objectClassCode, classDefinition, extraData, 1);

            switch (objectClassCode)
            {
                case 7703:
                    var integerObject = (IntegerObject)result.Members[0];
                    result = new LanguageEntryObject(integerObject.Value, extraData);
                    break;
            }

            // Set the type
            result.Type = objectClassCode;
            result.DataCode = extraData;

            // Next, let's go ahead and set the properties on the 
            //  current object, based on the type
            DataModel.AssignProperties(result);

            return result;
        }

        static CObject CreateCObject(SpecializedBinaryReader reader, int objectClassCode, Class classDefinition, int extraData, int indent)
        {
            BitResult bitResult;
            var obj = new CObject(objectClassCode, new byte[0], extraData, null);

            do
            {
                // Get the data code of the next sequence
                bitResult = reader.ReadAndExtractCode();

                if (bitResult == null)
                {
                    // KLUDGE - Treat as a boolean
                    bitResult = new BitResult(0, 0);
                }
                else if (bitResult.Code == 8)
                {
                    return obj;
                }
            } while (Parser.handleCode(obj, reader, bitResult.Code, bitResult.Data, indent + 2, classDefinition));

            return null;
        }
    }
}
