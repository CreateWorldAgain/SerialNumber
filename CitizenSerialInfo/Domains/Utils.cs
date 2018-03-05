using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CitizenSerialInfo.Domains
{
    public class Utils
    {
        public static string GetFullError(Exception ex)
        {
            StringBuilder result = new StringBuilder();
            if (ex != null)
            {
                do
                {
                    result.Append(ex.Message);
                    ex = ex.InnerException;
                    result.Append(GetFullError(ex));
                }
                while (ex != null);
            }

            return result.ToString();
        }

    }
}
