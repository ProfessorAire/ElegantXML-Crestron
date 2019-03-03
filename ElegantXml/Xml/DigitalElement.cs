using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace ElegantXml.Xml
{
    public class DigitalElement : IElement
    {
        /// <summary>
        /// The element's attribute path.
        /// </summary>
        public string AttributePath { get; set; }

        /// <summary>
        /// The element's attribute value.
        /// </summary>
        public bool AttributeValue { get; set; }

        /// <summary>
        /// The element's ID.
        /// </summary>
        public ushort ID { get; set; }

        /// <summary>
        /// Creates a new element.
        /// </summary>
        /// <param name="id">The 1-based ID of the element (which should match the Simpl+ module's parameter index).</param>
        /// <param name="path">The Simpl+ module's parameter path point to the XML element this represents.</param>
        public DigitalElement(ushort id, string path)
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
            return AttributeValue.ToString();
        }

       
    }
}