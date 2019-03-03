using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace ElegantXml.Xml
{
    public static class Debug
    {
        /// <summary>
        /// Set to true to always enable debug messages to be passed to the console.
        /// </summary>
        internal static bool isDebugEnabled = false;

        /// <summary>
        /// Print a debug message to the console.
        /// </summary>
        /// <param name="message"></param>
        public static void PrintLine(string message)
        {
            if (isDebugEnabled)
            {
                CrestronConsole.PrintLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + " | " + message);
            }
#if DEBUG
            // If using a dll compiled in Debug, this ensures the messages are always printed.
            else
            {
                CrestronConsole.PrintLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + " | " + message);
            }
#endif
        }

    }
}