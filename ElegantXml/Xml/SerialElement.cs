using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace ElegantXml.Xml
{
    public class SerialElement : IElement
    {
        /// <summary>
        /// The element's attribute path.
        /// </summary>
        public string AttributePath { get; set; }

        /// <summary>
        /// If true, the value belongs to the element's internal text, not an attribute.
        /// </summary>
        public bool IsElement { get; set; }

        /// <summary>
        /// The element's attribute value.
        /// </summary>
        public string AttributeValue { get; set; }

        /// <summary>
        /// The element's ID.
        /// </summary>
        public ushort ID {get;set;}

                private string defaultValue = "";
        /// <summary>
        /// A default value for the element to possess.
        /// </summary>
        public string DefaultValue { get { return defaultValue; } set { defaultValue = value; } }

        /// <summary>
        /// Creates a new element.
        /// </summary>
        /// <param name="id">The 1-based ID of the element (which should match the Simpl+ module's parameter index).</param>
        /// <param name="path">The Simpl+ module's parameter path point to the XML element this represents.</param>
        /// <param name="defaultValue">The default value the attribute should have if none is found in the file.</param>
        public SerialElement(ushort id, string path, string defaultValue)
        {
            AttributePath = path;
            ID = id;
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// Creates a new element.
        /// </summary>
        /// <param name="id">The 1-based ID of the element (which should match the Simpl+ module's parameter index).</param>
        /// <param name="path">The Simpl+ module's parameter path point to the XML element this represents.</param>
        public SerialElement(ushort id, string path)
        {
            AttributePath = path;
            ID = id;
        }

        /// <summary>
        /// Returns the attribute's value as a string.
        /// </summary>
        /// <returns></returns>
        public string GetAttributeValue()
        {
            return AttributeValue;
        }
    }
}