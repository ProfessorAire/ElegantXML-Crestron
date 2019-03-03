using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronXml;
using Crestron.SimplSharp.CrestronXmlLinq;

namespace ElegantXml.Xml
{
    /// <summary>
    /// Handles building out the XML document to write to a file.
    /// </summary>
    public class XmlBuilder
    {
        /// <summary>
        /// The document class the builder writes to in memory.
        /// </summary>
        public XDocument Document { get; set; }

        /// <summary>
        /// Creates a new builder.
        /// </summary>
        /// <param name="rootElementName">This is the name of the root element, which contains the whole XML structure.
        /// This is not the ?xml element that denotes an XML document, but the first element immediately following.
        /// There should only be one root element in an XML document.</param>
        public XmlBuilder(string rootElementName)
        {
            Document = new XDocument();
            Document.Add(new XElement(rootElementName));
        }

        /// <summary>
        /// Save the document to the path provided. Requires the document have been built first.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool Save(string path)
        {
            if (Document.Root.Elements().Count() > 0)
            {
                try
                {
                    Document.Save(path);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.PrintLine("Exception encountered saving XML file!");
                    Debug.PrintLine(ex.Message);
                }
            }
            else
            {
                Debug.PrintLine("Couldn't write empty XML file: " + path);
                Debug.PrintLine("Ensure the document is being built before attempting to save!");
            }
            return false;
        }

        /// <summary>
        /// Writes all the DigitalElements from a list of DigitalProcessors to the XML Document in memory.
        /// </summary>
        /// <param name="items">A list of DigitalProcessors.</param>
        /// <returns>True if successful, false if it fails.</returns>
        public bool WriteDigitals(List<DigitalProcessor> items)
        {
            var status = true;
            for (var i = 0; i < items.Count; i++)
            {
                if (WriteDigitals(items[i]) == false)
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>
        /// Writes a single DigitalElement to the XML Document in memory.
        /// </summary>
        /// <param name="item">The DigitalElement to write.</param>
        /// <returns>True if successful, false if it fails.</returns>
        public bool WriteDigitals(DigitalProcessor item)
        {
            var status = true;
            for (var i = 0; i < item.Elements.Count; i++)
            {
                if (WriteElement(item.Elements[i]) == false)
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>
        /// Writes all the AnalogElements from a list of AnalogProcessors to the XML Document in memory.
        /// </summary>
        /// <param name="items">A list of AnalogProcessors.</param>
        /// <returns>True if successful, false if it fails.</returns>
        public bool WriteAnalogs(List<AnalogProcessor> items)
        {
            var status = true;
            for (var i = 0; i < items.Count; i++)
            {
                if (WriteAnalogs(items[i]) == false)
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>
        /// Writes a single AnalogElement to the XML Document in memory.
        /// </summary>
        /// <param name="item">The AnalogElement to write.</param>
        /// <returns>True if successful, false if it fails.</returns>
        public bool WriteAnalogs(AnalogProcessor item)
        {
            var status = true;
            for (var i = 0; i < item.Elements.Count; i++)
            {
                if (WriteElement(item.Elements[i]) == false)
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>
        /// Writes all the SignedAnalogElements from a list of SignedAnalogProcessors to the XML Document in memory.
        /// </summary>
        /// <param name="items">A list of SignedAnalogProcessors.</param>
        /// <returns>True if successful, false if it fails.</returns>
        public bool WriteSignedAnalogs(List<SignedAnalogProcessor> items)
        {
            var status = true;
            for (var i = 0; i < items.Count; i++)
            {
                if (WriteSignedAnalogs(items[i]) == false)
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>
        /// Writes a single SignedAnalogElement to the XML Document in memory.
        /// </summary>
        /// <param name="item">The SignedAnalogElement to write.</param>
        /// <returns>True if successful, false if it fails.</returns>
        public bool WriteSignedAnalogs(SignedAnalogProcessor item)
        {
            var status = true;
            for (var i = 0; i < item.Elements.Count; i++)
            {
                if (WriteElement(item.Elements[i]) == false)
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>
        /// Writes all the SerialElements from a list of SerialProcessors to the XML Document in memory.
        /// </summary>
        /// <param name="items">A list of SerialProcessors.</param>
        /// <returns>True if successful, false if it fails.</returns>
        public bool WriteSerials(List<SerialProcessor> items)
        {
            var status = true;
            for (var i = 0; i < items.Count; i++)
            {
                if (WriteSerials(items[i]) == false)
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>
        /// Writes a single SerialElement to the XML Document in memory.
        /// </summary>
        /// <param name="item">The SerialElement to write.</param>
        /// <returns>True if successful, false if it fails.</returns>
        public bool WriteSerials(SerialProcessor item)
        {
            bool status = true;
            for (var i = 0; i < item.Elements.Count; i++)
            {
                if (WriteElement(item.Elements[i]) == false)
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>
        /// Writes an element to the XML document in memory.
        /// </summary>
        /// <param name="item">An IElement to write to the document.</param>
        /// <returns>True if successful, false if it fails.</returns>
        public bool WriteElement(IElement item)
        {
            var name = "";
            var parts = item.AttributePath.Split('.');
            var attribute = "";
            var value = "";
            string[] slice = null;
            XElement element = null;
            var current = Document.Root;
            for (var i = 0; i < parts.Length - 1; i++)
            {
                element = null;
                name = "";
                attribute = "";
                value = "";
                if (parts[i].Contains(" "))
                {
                    //This is a complex element. It has an inline attribute to identify the element. (eg: Element id="Element1")
                    try
                    {
                        slice = parts[i].Split(' ');
                        name = slice[0].Replace(" ", "");
                        value = slice[1].Split('=')[1].Replace("=", "").Replace("\"", "");
                        attribute = slice[1].Split('=')[0].Replace("=", "").Replace(" ", "");

                        if (current.Elements().Where((e) => e.Name.ToLower() == name.ToLower() &&
                            e.Attributes().Where((a) => a.Name.ToLower() == attribute.ToLower() &&
                                a.Value.ToLower() == value.ToLower()).Count() == 1).Count() > 0)
                        {
                            current = current.Elements().Where((e) => e.Name.ToLower() == name.ToLower() &&
                            e.Attributes().Where((a) => a.Name.ToLower() == attribute.ToLower() &&
                                a.Value.ToLower() == value.ToLower()).Count() == 1).First();
                            continue;
                        }

                        if (element == null)
                        {
                            element = new XElement(name);
                            element.Add(new XAttribute(attribute, value));
                        }

                        current.Add(element);
                        current = element;
                    }
                    catch (Exception ex)
                    {
                        Debug.PrintLine("Exception occurred while writing complex element to XML Document in memory.");
                        Debug.PrintLine(ex.Message);
                        return false;
                    }
                }
                else
                {
                    //This should be a simple element. It has no line attribute to identify the element. (eg: Element)
                    try
                    {
                        name = parts[i];

                        if (current.Elements().Where((e) => e.Name.ToLower() == name.ToLower()).Count() > 0)
                        {
                            current = current.Elements().Where((e) => e.Name.ToLower() == name.ToLower()).First();
                            continue;
                        }

                        if (element == null)
                        {
                            element = new XElement(name);
                        }

                        current.Add(element);
                        current = element;
                    }
                    catch (Exception ex)
                    {
                        Debug.PrintLine("Exception occurred while writing simple element to XML Document in memory.");
                        Debug.PrintLine(ex.Message);
                        return false;
                    }
                }
            }
            try
            {
                attribute = parts.Last().Replace(" ", "");
                current.Add(new XAttribute(attribute, item.GetAttributeValue()));
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception occurred while adding final attribute to XML element.");
                Debug.PrintLine(ex.Message);
                return false;
            }
            return true;

        }

    }
}