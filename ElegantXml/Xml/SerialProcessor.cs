using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace ElegantXml.Xml
{
    public class SerialProcessor : ProcessorBase
    {
        /// <summary>
        /// A list of the SerialElements associated with this processor.
        /// </summary>
        public List<SerialElement> Elements { get; set; }

        public delegate void ReportValueChangeDelegate(ushort element, SimplSharpString value);
        /// <summary>
        /// Reports a changed value back to the Simpl+ module.
        /// </summary>
        public ReportValueChangeDelegate ReportValueChange { get; set; }

        public delegate void ReportIsInitializedDelegate(ushort state);
        /// <summary>
        /// Reports that the class is initialized back to the Simpl+ module.
        /// </summary>
        public ReportIsInitializedDelegate ReportIsInitialized { get; set; }

        /// <summary>
        /// Used by Simpl+.
        /// </summary>
        public SerialProcessor()
            : base()
        {
            Elements = new List<SerialElement>();
        }

        /// <summary>
        /// Adds an item to the processor's list of elements. Attempts to parse the path for a default value.
        /// </summary>
        /// <param name="elementID">The 1-based ID of the element, which should match the Simpl+ module parameter's index.</param>
        /// <param name="elementPath">The path provided by the Simpl+ module parameter.</param>
        /// <param name="defaultValue">The default value of the element.</param>
        public void AddValue(ushort elementID, string elementPath, string defaultValue)
        {
            try
            {
                CMonitor.Enter(this);
                var path = elementPath;
                string defVal = "";
                if (elementPath.Contains(DefaultValueDelimiter))
                {
                    path = elementPath.Split(DefaultValueDelimiter)[0];
                    Debug.PrintLine("Element " + elementID + "'s Path = " + path);
                    try
                    {
                        defVal = elementPath.Split(DefaultValueDelimiter)[1];
                        Debug.PrintLine("Element " + elementID + "'s DefaultValue = " + defVal);
                    }
                    catch
                    {
                        Debug.PrintLine("Couldn't parse default serial value from: " + elementPath);
                        defVal = defaultValue;
                    }
                }
                else
                {
                    defVal = defaultValue;
                }
                var element = new SerialElement(elementID, path, defVal);
                Elements.Add(element);
            }
            finally
            {
                CMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Sorts the list of elements by their path.
        /// </summary>
        public void Sort()
        {
            Elements = Elements.OrderBy((o) => o.AttributePath).ToList();
        }

        /// <summary>
        /// Updates the value of an element, both internally as well as to the Simpl+ module.
        /// </summary>
        /// <param name="elementID">The 1-based ID of the element, which should match the Simpl+ module parameter's index.</param>
        /// <param name="value">The new value to use as a string.</param>
        public void UpdateValue(ushort elementID, string value)
        {
            if (!IsInitialized) { return; }
            if (elementID < 1)
            {
                Debug.PrintLine("Couldn't update value for String element. The index was invalid.");
                return;
            }
            try
            {
                if (Elements == null ||
                    Elements.Count <= 0 ||
                    Elements.Where((e) => e.ID == elementID).Count() == 0)
                {
                    Debug.PrintLine("No elements present to update Serial value on.");
                    return;
                }
                CMonitor.Enter(this);
                var element = Elements.Where((e) => e.ID == elementID).First();
                if (element == null)
                {
                    Debug.PrintLine("Couldn't find element to update Serial value on.");
                    return;
                }
                element.AttributeValue = value;
                ReportValueChange(elementID, element.AttributeValue);
                manager.IsSaveRequired(1);
            }
            finally
            {
                CMonitor.Exit(this);
            }
        }


        /// <summary>
        /// Initializes the processor.
        /// </summary>
        /// <param name="managerID">The ID of the manager module this class should associate with.</param>
        public void Initialize(ushort managerID)
        {
            if (IsInitialized == true) { return; }
            var man = Manager.GetManagerByID(managerID);
            if (man != null)
            {
                man.AddSerial(this);
            }
            else
            {
                return;
            }
            IsInitialized = true;
            ReportIsInitialized(1);
        }


    }
}