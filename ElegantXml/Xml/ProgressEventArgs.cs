using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace ElegantXml.Xml
{
    public class ProgressEventArgs : EventArgs
    {

        public ushort Progress { get; set; }

        public ProgressEventArgs(ushort progress)
        {
            Progress = progress;
        }

    }
}