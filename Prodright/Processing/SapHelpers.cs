using System;
using System.Collections.Generic;
using System.Text;

namespace Prodright.Processing
{
    internal static class SapHelpers
    {
        internal static string FormatSapDate(string isoDate)
        {
            if (DateTime.TryParse(isoDate, out DateTime dt))
            {
                return dt.ToString("dd MMM yyyy 'at' HH:mm:ss 'UTC'");
            }
            return isoDate;
        }

    }
}
