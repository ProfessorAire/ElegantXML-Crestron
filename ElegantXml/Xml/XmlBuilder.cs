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
        /// Internal value to hold Document property value.
        /// </summary>
        private XDocument document = null;

        /// <summary>
        /// The document class the builder writes to in memory.
        /// </summary>
        public XDocument Document { get { return document; } }

        /// <summary>
        /// The delimiter used when splitting paths.
        /// </summary>
        public char PathDelimiter { get; set; }

        private ushort progress = 0;
        private ushort step = 0;

        public void ResetProgress(ushort stepValue) { progress = 0; step = stepValue; }

        public event EventHandler<ProgressEventArgs> ProgressUpdated;
        private void RaiseProgressUpdated(ushort value)
        {
            ProgressUpdated.Invoke(this, new ProgressEventArgs(value));
        }

        /// <summary>
        /// Creates a new builder.
        /// </summary>
        /// <param name="rootElementName">This is the name of the root element, which contains the whole XML structure.
        /// This is not the ?xml element that denotes an XML document, but the first element immediately following.
        /// There should only be one root element in an XML document.</param>
        public XmlBuilder(string rootElementName, char pathDelimiter)
        {
            document = new XDocument();
            Document.Add(new XElement(rootElementName));
            PathDelimiter = pathDelimiter;
        }

        /// <summary>
        /// Creates a new builder.
        /// </summary>
        /// <param name="rootElementName">This is the name of the root element, which contains the whole XML structure.
        /// This is not the ?xml element that denotes an XML document, but the first element immediately following.
        /// There should only be one root element in an XML document.</param>
        /// <param name="xDocument">The XDocument to write into.</param>
        public XmlBuilder(string rootElementName, XDocument xDocument, char pathDelimiter)
        {
            if (xDocument == null)
            {
                throw new NullReferenceException("Empty XDocument provided to XmlBuilder. Unable to create the object.");
            }

            if (xDocument.Root.Name != rootElementName)
            {
                Debug.PrintLine("Unable to verify XDocument's root element. This document will not be used and a new document will be created.");
                document = new XDocument();
                Document.Add(new XElement(rootElementName));
            }
            else
            {
                document = xDocument;
            }
            PathDelimiter = pathDelimiter;
        }

        /// <summary>
        /// Creates a new builder.
        /// </summary>
        /// <param name="rootElementName">This is the name of the root element, which contains the whole XML structure.
        /// This is not the ?xml element that denotes an XML document, but the first element immediately following.
        /// There should only be one root element in an XML document.</param>
        /// <param name="xDocument">The XDocument to write into.</param>
        public XmlBuilder(string rootElementName, XDocument xDocument)
        {
            if (xDocument == null)
            {
                throw new NullReferenceException("Empty XDocument provided to XmlBuilder. Unable to create the object.");
            }

            if (xDocument.Root.Name != rootElementName)
            {
                Debug.PrintLine("Unable to verify XDocument's root element. This document will not be used and a new document will be created.");
                document = new XDocument();
                Document.Add(new XElement(rootElementName));
            }
            else
            {
                document = xDocument;
            }
        }

        private static void SortXml(XElement node)
        {
            node.ReplaceNodes(node.Elements()
                .OrderBy(x => x.Name, new ElementComparer())
                .ThenBy(x => (x.HasAttributes ? x.Attributes().First().Value : ""), new ElementComparer()));
            foreach (var child in node.Elements())
            {
                SortXml(child);
            }
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
                    if (!Crestron.SimplSharp.CrestronIO.File.Exists(path))
                    {
                        SortXml(Document.Root);
                    }

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
            var markerTime = CrestronEnvironment.GetLocalTime().AddSeconds(20);
            for (var i = 0; i < items.Count; i++)
            {
                if (CrestronEnvironment.GetLocalTime() > markerTime)
                {
                    CrestronEnvironment.AllowOtherAppsToRun();
                    markerTime = CrestronEnvironment.GetLocalTime().AddSeconds(20);
                }
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
            var markerTime = CrestronEnvironment.GetLocalTime().AddSeconds(20);
            for (var i = 0; i < items.Count; i++)
            {
                if (CrestronEnvironment.GetLocalTime() > markerTime)
                {
                    CrestronEnvironment.AllowOtherAppsToRun();
                    markerTime = CrestronEnvironment.GetLocalTime().AddSeconds(20);
                }
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
            var markerTime = CrestronEnvironment.GetLocalTime().AddSeconds(20);
            for (var i = 0; i < items.Count; i++)
            {
                if (CrestronEnvironment.GetLocalTime() > markerTime)
                {
                    CrestronEnvironment.AllowOtherAppsToRun();
                    markerTime = CrestronEnvironment.GetLocalTime().AddSeconds(20);
                }
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
            var markerTime = CrestronEnvironment.GetLocalTime().AddSeconds(20);
            for (var i = 0; i < items.Count; i++)
            {
                if (CrestronEnvironment.GetLocalTime() > markerTime)
                {
                    CrestronEnvironment.AllowOtherAppsToRun();
                    markerTime = CrestronEnvironment.GetLocalTime().AddSeconds(20);
                }
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
        /// Writes a list of SerialPropertyInterlocks to the XML Document in memory.
        /// </summary>
        /// <param name="items">The list of SerialPropertyInterlock items to write.</param>
        /// <returns>True if successful, false if any item fails.</returns>
        public bool WriteSerialPropertyInterlocks(List<SerialPropertyInterlock> items)
        {
            var status = true;
            var markerTime = CrestronEnvironment.GetLocalTime().AddSeconds(20);
            for (var i = 0; i < items.Count; i++)
            {
                if (CrestronEnvironment.GetLocalTime() > markerTime)
                {
                    CrestronEnvironment.AllowOtherAppsToRun();
                    markerTime = CrestronEnvironment.GetLocalTime().AddSeconds(20);
                }
                if (WriteElement(items[i].Element) == false)
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>
        /// Writes a list of AnalogPropertyInterlocks to the XML Document in memory.
        /// </summary>
        /// <param name="items">The list of AnalogPropertyInterlock items to write.</param>
        /// <returns>True if successful, false if any item fails.</returns>
        public bool WriteAnalogPropertyInterlocks(List<AnalogPropertyInterlock> items)
        {
            var status = true;
            var markerTime = CrestronEnvironment.GetLocalTime().AddSeconds(20);
            for (var i = 0; i < items.Count; i++)
            {
                if (CrestronEnvironment.GetLocalTime() > markerTime)
                {
                    CrestronEnvironment.AllowOtherAppsToRun();
                    markerTime = CrestronEnvironment.GetLocalTime().AddSeconds(20);
                }
                if (WriteElement(items[i].Element) == false)
                {
                    status = false;
                }
            }
            return status;
        }

        /// <summary>
        /// Writes a list of SignedAnalogPropertyInterlocks to the XML Document in memory.
        /// </summary>
        /// <param name="items">The list of SignedAnalogPropertyInterlock items to write.</param>
        /// <returns>True if successful, false if any item fails.</returns>
        public bool WriteSignedAnalogPropertyInterlocks(List<SignedAnalogPropertyInterlock> items)
        {
            var status = true;
            var markerTime = CrestronEnvironment.GetLocalTime().AddSeconds(20);
            for (var i = 0; i < items.Count; i++)
            {
                if (CrestronEnvironment.GetLocalTime() > markerTime)
                {
                    CrestronEnvironment.AllowOtherAppsToRun();
                    markerTime = CrestronEnvironment.GetLocalTime().AddSeconds(20);
                }
                if (WriteElement(items[i].Element) == false)
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
            progress += step;

            if (item == null)
            {
                Debug.PrintLine("Item is null! Cannot process null reference.");
                RaiseProgressUpdated(progress);
                return false;
            }
            if (Document == null)
            {
                Debug.PrintLine("Document is null! Cannot process saving XML Document.");
                RaiseProgressUpdated(progress);
                return false;
            }
            else if (Document.Root == null)
            {
                Debug.PrintLine("Document.Root is null! Cannot process saving XML Document.");
                RaiseProgressUpdated(progress);
                return false;
            }

            Debug.PrintLine("Writing path: " + item.AttributePath + " with the value: " + item.GetAttributeValue());

            var parts = item.AttributePath.Split(PathDelimiter);
            XElement newElement = null;
            var currentElement = Document.Root;
            var isElement = parts.Last().Contains("=");

            for (var i = 0; i < (isElement ? parts.Length : parts.Length - 1); i++)
            {
                if (parts[i].Contains(" "))
                {
                    try
                    {
                        var pieces = parts[i].Split(' ');
                        var att = pieces[1].Split('=');
                        att[1] = att[1].Replace("\"", "");
                        var nextElement =
                            (from elem in currentElement.Elements(pieces[0])
                             where elem.Attribute(att[0]).Value == att[1]
                             select elem).FirstOrDefault();

                        if (nextElement != null)
                        {
                            currentElement = nextElement;
                            continue;
                        }

                        newElement = new XElement(pieces[0]);
                        newElement.Add(new XAttribute(att[0], att[1]));
                        currentElement.Add(newElement);
                        currentElement = newElement;
                    }
                    catch (Exception ex)
                    {
                        Debug.PrintLine("Exception occurred while writing complex element to XML Document in memory.");
                        Debug.PrintLine(ex.Message);
                        RaiseProgressUpdated(progress);
                        return false;
                    }
                }
                else
                {
                    try
                    {
                        var nextElement = currentElement.Element(parts[i]);
                        if (nextElement != null)
                        {
                            currentElement = nextElement;
                            continue;
                        }

                        newElement = new XElement(parts[i]);
                        currentElement.Add(newElement);
                        currentElement = newElement;

                    }
                    catch (Exception ex)
                    {
                        Debug.PrintLine("Exception occurred while writing simple element to XML Document in memory.");
                        Debug.PrintLine(ex.Message);
                        RaiseProgressUpdated(progress);
                        return false;
                    }
                }
            }

            if (parts.Last() == "" ||
                parts.Last() == string.Empty)
            {
                isElement = true;
            }

            try
            {
                if (isElement)
                {
                    currentElement.Value = item.GetAttributeValue();
                }
                else
                {
                    currentElement.SetAttributeValue(parts.Last(), item.GetAttributeValue());
                }
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception occurred while adding final attribute to XML element.");
                Debug.PrintLine(ex.Message);
                RaiseProgressUpdated(progress);
                return false;
            }

            RaiseProgressUpdated(progress);
            return true;
        }


    }
}