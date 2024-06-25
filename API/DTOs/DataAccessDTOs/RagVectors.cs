//namespace OpenAICustomFunctionCallingAPI.API.DTOs.DataAccessDTOs
//{
//    public class RagVectors
//    {
//        public int Id { get; set; }
//        public float VectorNorm { get; set; }
//        public byte[] Vectors { get; set; }

//        // Method to serialize a float array to byte array
//        public static byte[] SerializeVector(float[] vector)
//        {
//            using (MemoryStream ms = new MemoryStream())
//            {
//                using (BinaryWriter writer = new BinaryWriter(ms))
//                {
//                    foreach (float value in vector)
//                    {
//                        writer.Write(value);
//                    }
//                }
//                return ms.ToArray();
//            }
//        }

//        // Method to deserialize a byte array to a float array
//        public static float[] DeserializeVector(byte[] binaryVector)
//        {
//            using (MemoryStream ms = new MemoryStream(binaryVector))
//            {
//                using (BinaryReader reader = new BinaryReader(ms))
//                {
//                    int vectorLength = binaryVector.Length / sizeof(float);
//                    float[] vector = new float[vectorLength];
//                    for (int i = 0; i < vectorLength; i++)
//                    {
//                        vector[i] = reader.ReadSingle();
//                    }
//                    return vector;
//                }
//            }
//        }
//    }

//}
