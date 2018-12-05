namespace DAOGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class MiscHelper
    {
        public static string ReformatString(string value)
        {
            StringBuilder sb = new StringBuilder();
            string[] raws = value.Split('_');
            foreach (var raw in raws)
            {
                sb.Append(UppercaseFirst(raw.ToLower()));
            }
            return sb.ToString();
        }

        public static string ReformatFieldString(string value)
        {
            StringBuilder sb = new StringBuilder();
            string[] raws = value.Split('_');
            raws[0] = raws[0].ToLower();
            sb.Append(raws[0]);
            for (int i = 1; i < raws.Length; i++)
            {
                sb.Append(UppercaseFirst(raws[i].ToLower()));
            }
            return sb.ToString();
        }

        public static string UppercaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        public static string GenerateColumnsSelection(string[] columns, bool isParameter = false)
        {
            var sb = new StringBuilder();
                        
            foreach (var column in columns)
            {
                if (isParameter)
                {
                    sb.Append(string.Format("[@{0}]", column));
                }
                else
                {
                    sb.Append(string.Format("[{0}]", column));
                }
                
            }

            return sb.ToString().Replace("][", ", ").Replace("[", "").Replace("]", "");
        }

        public static string GenerateColumnsUpdate(string[] columns)
        {
            var sb = new StringBuilder();

            foreach(var column in columns)
            {
                sb.Append(string.Format("[{0} = @{0}]", column));
            }

            return sb.ToString().Replace("][", ", ").Replace("[", "").Replace("]", "");
        }
    }
}
