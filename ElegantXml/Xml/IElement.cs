using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace ElegantXml.Xml
{
    /// <summary>
    /// Interface for elements to be parsed from XML files and passed to Simpl+.
    /// </summary>
    public interface IElement
    {
        string GetAttributeValue();
        string AttributePath { get; set; }
        bool IsElement { get; set; }
    }
}