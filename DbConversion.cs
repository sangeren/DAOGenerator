namespace DAOGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class DbConversion
    {
        public static bool ConvertToBoolean(object obj)
        {
            bool Result = false;

            try
            {
                string value = obj.ToString();

                if ("1".Equals(value))
                    Result = true;
            }
            catch (Exception ex)
            {
                
            }
            return Result;
        }

        public static string ConvertToString(object obj)
        {
            string Result = null;

            try
            {
                Result = obj.ToString();

                if (string.IsNullOrEmpty(Result))
                {
                    Result = null;
                }
            }
            catch (Exception ex)
            {
                
            }

            return Result;
        }

        public static int? ConvertToInt(object obj)
        {
            int? Result = null;

            try
            {
                int temp = 0;

                string tempHold = obj.ToString();
                if (!string.IsNullOrEmpty(tempHold))
                {
                    int.TryParse(tempHold, out temp);
                    Result = temp;
                }
            }
            catch (Exception ex)
            {
                
            }

            return Result;
        }
    }
}
