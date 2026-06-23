using System.Text;

namespace Emerald.Runtime.Extensions
{
    internal static class StringExtensions
    {
        public static string ToSnakeCase(this string str)
        {
            var sb = new StringBuilder(str.Length + 4);
            foreach (var c in str)
            {
                if (char.IsUpper(c)) { sb.Append('_'); sb.Append(char.ToLowerInvariant(c)); }
                else sb.Append(c);
            }
            return sb.ToString();
        }
    }
}