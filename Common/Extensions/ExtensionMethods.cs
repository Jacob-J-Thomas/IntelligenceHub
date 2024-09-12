using System.Text;

namespace IntelligenceHub.Common.Extensions
{
    public static class ExtensionMethods
    {
        public static  byte[] EncodeToBinary(this float[] vectors)
        {
            if (vectors is null) throw new ArgumentNullException(nameof(vectors));
            using (MemoryStream stream = new())
            {
                using (BinaryWriter writer = new(stream)) foreach (float v in vectors) writer.Write(v);
                return stream.ToArray();
            }
        }

        public static string ToCommaSeparatedString(this IEnumerable<string> strings)
        {
            if (strings is null) throw new ArgumentNullException(nameof(strings));
            StringBuilder result = new StringBuilder(); // Use StringBuilder for efficient string concatenation
            foreach (string str in strings) result.Append(str + ",");
            if (result.Length > 0) result.Length--;
            return result.ToString();
        }

        public static string[] ToStringArray(this string commaSeparatedString)
        {
            if (commaSeparatedString is null) throw new ArgumentNullException(nameof(commaSeparatedString));
            string[] result = commaSeparatedString.Split(',');
            for (int i = 0; i < result.Length; i++) result[i] = result[i].Trim(); // Trim each element to remove leading and trailing whitespaces
            return result;
        }
    }
}
