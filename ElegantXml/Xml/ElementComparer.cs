using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace ElegantXml.Xml
{
    public class ElementComparer : IComparer<string>
    {

        public int Compare(string s1, string s2)
        {
            if (IsNumber(s1) && IsNumber(s2))
            {
                var n1 = int.Parse(s1);
                var n2 = int.Parse(s2);
                return n1 > n2 ? 1 : (n1 < n2 ? -1 : 0);
            }
            else if (IsNumber(s1) && !IsNumber(s2))
            {
                return -1;
            }
            else if (!IsNumber(s1) && IsNumber(s2))
            {
                return 1;
            }
            else
            {
                return string.Compare(s1, s2, true);
            }
        }

        private static bool IsNumber(object value)
        {
            try
            {
                var val = Convert.ToInt32(value);
                return true;
            }
            catch
            {
                return false;
            }
        }


    }
}