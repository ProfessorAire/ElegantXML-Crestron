using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace ElegantXml.Xml
{
    /// <summary>
    /// Details for an Analog Element.
    /// </summary>
    public class AnalogElement : IElement
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
        /// The element's ID.
        /// </summary>
        public ushort ID { get; set; }

        /// <summary>
        /// The element's attribute value.
        /// </summary>
        public ushort AttributeValue { get; set; }

        private ushort defaultValue = 0;
        /// <summary>
        /// A default value for the element to possess.
        /// </summary>
        public ushort DefaultValue { get { return defaultValue; } set { defaultValue = value; } }

        /// <summary>
        /// Creates a new element.
        /// </summary>
        /// <param name="id">The 1-based ID of the element (which should match the Simpl+ module's parameter index).</param>
        /// <param name="path">The Simpl+ module's parameter path point to the XML element this represents.</param>
        /// <param name="defaultValue">The default value the attribute should have if none is found in the file.</param>
        public AnalogElement(ushort id, string path, ushort defaultValue)
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
        public AnalogElement(ushort id, string path)
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