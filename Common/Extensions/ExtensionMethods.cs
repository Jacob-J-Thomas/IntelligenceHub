using System.Text;

namespace OpenAICustomFunctionCallingAPI.Common.Extensions
{
    public static class ExtensionMethods
    {
        public static string ToCommaSeparatedString(this IEnumerable<string> strings)
        {
            if (strings == null)
                throw new ArgumentNullException(nameof(strings));

            // Use StringBuilder for efficient string concatenation
            StringBuilder result = new StringBuilder();

            foreach (string str in strings)
            {
                // Append each string followed by a comma
                result.Append(str);
                result.Append(",");
            }

            // Remove the trailing comma if the list is not empty
            if (result.Length > 0)
                result.Length--;

            return result.ToString();
        }

        public static string[] ToStringArray(this string commaSeparatedString)
        {
            if (commaSeparatedString == null)
                throw new ArgumentNullException(nameof(commaSeparatedString));

            // Split the input string using commas
            string[] result = commaSeparatedString.Split(',');

            // Trim each element to remove leading and trailing whitespaces
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = result[i].Trim();
            }

            return result;
        }
    }
}
