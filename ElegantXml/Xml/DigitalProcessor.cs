using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace ElegantXml.Xml
{
    public class DigitalProcessor : ProcessorBase
    {
        /// <summary>
        /// A list of DigitalElements associated with this processor.
        /// </summary>
        public List<DigitalElement> Elements { get; set; }

        public delegate void ReportValueChangeDelegate(ushort element, ushort value);
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
        public DigitalProcessor() : base()
        {
            Elements = new List<DigitalElement>();
        }

        /// <summary>
        /// Adds an item to the processor's list of elements.
        /// </summary>
        /// <param name="elementID">The 1-based ID of the element, which should match the Simpl+ module parameter's index.</param>
        /// <param name="elementPath">The path provided by the Simpl+ module parameter.</param>
        /// <param name="defaultValue">The default value of the element.</param>
        public void AddValue(ushort elementID, string elementPath, ushort defaultValue)
        {
            try
            {
                CMonitor.Enter(this);
                var element = new DigitalElement(elementID, elementPath);
                element.DefaultValue = defaultValue > 0 ? true : false;
                Elements.Add(element);
            }
            finally
            {
                CMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Adds an item to the processor's list of elements. Attempts to parse the path for a default value.
        /// </summary>
        /// <param name="elementID">The 1-based ID of the element, which should match the Simpl+ module parameter's index.</param>
        /// <param name="elementPath">The path provided by the Simpl+ module parameter.</param>
        public void AddValueWithDefaultInPath(ushort elementID, string elementPath)
        {
            try
            {
                CMonitor.Enter(this);
                var path = elementPath;
                bool defaultValue = false;
                if (elementPath.Contains(DefaultValueDelimiter))
                {
                    path = elementPath.Split(DefaultValueDelimiter)[0];
                    try
                    {
                        var val = elementPath.Split(DefaultValueDelimiter)[1];
                        if (val.ToLower() == "true" || val.ToLower() == "false")
                        {
                            defaultValue = val.ToLower() == "true" ? true : false;
                        }
                        else if (val == "0" || val == "1")
                        {
                            defaultValue = val == "1" ? true : false;
                        }
                        else
                        {
                            defaultValue = bool.Parse(elementPath.Split(DefaultValueDelimiter)[1]);
                        }
                    }
                    catch { Debug.PrintLine("Couldn't parse default digital value from: " + elementPath); }
                }
                var element = new DigitalElement(elementID, path, defaultValue);
                Elements.Add(element);
            }
            finally
            {
                CMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Flips the value of the element with the ID provided. 0 > 1 and 1 > 0.
        /// </summary>
        /// <param name="elementID">The 1-based ID of the element, which should match the Simpl+ module parameter's index.</param>
        public void ToggleValue(ushort elementID)
        {
            if (!IsInitialized) { return; }
            if (elementID < 1)
            {
                Debug.PrintLine("Couldn't toggle value for element. The index was invalid.");
                return;
            }
            try
            {
                if (Elements == null ||
                    Elements.Count <= 0 ||
                    Elements.Where((e) => e.ID == elementID).Count() == 0)
                {
                    Debug.PrintLine("No elements present to update Digital value on.");
                    return;
                }
                CMonitor.Enter(this);
                var element = Elements.Where((e) => e.ID == elementID).First();
                if (element == null)
                {
                    Debug.PrintLine("Couldn't find element to update Digital value on.");
                    return;
                }
                element.AttributeValue = !element.AttributeValue;
                ReportValueChange(elementID, element.AttributeValue == true ? (ushort)1 : (ushort)0);
                manager.IsSaveRequired(1);
            }
            finally
            {
                CMonitor.Exit(this);
            }

        }

        /// <summary>
        /// Updates the value of an element, both internally as well as to the Simpl+ module.
        /// </summary>
        /// <param name="elementID">The 1-based ID of the element, which should match the Simpl+ module parameter's index.</param>
        /// <param name="value">The new value to use as a boolean.</param>
        public void UpdateValue(ushort elementID, bool value)
        {
            if (!IsInitialized) { return; }
            if (elementID < 1)
            {
                Debug.PrintLine("Couldn't update value for Digital element. The index was invalid.");
                return;
            }
            try
            {
                if (Elements == null ||
                    Elements.Count <= 0 ||
                    Elements.Where((e) => e.ID == elementID).Count() == 0)
                {
                    Debug.PrintLine("No elements present to update Digital value on.");
                    return;
                }
                CMonitor.Enter(this);
                var element = Elements.Where((e) => e.ID == elementID).First();
                if (element == null)
                {
                    Debug.PrintLine("Couldn't find element to update Digital value on.");
                    return;
                }
                element.AttributeValue = value;
                ReportValueChange(elementID, element.AttributeValue == true ? (ushort)1 : (ushort)0);
                manager.IsSaveRequired(1);
            }
            finally
            {
                CMonitor.Exit(this);
            }

        }

        /// <summary>
        /// Updates the value of an element, both internally as well as to the Simpl+ module.
        /// </summary>
        /// <param name="elementID">The 1-based ID of the element, which should match the Simpl+ module parameter's index.</param>
        /// <param name="value">The new value to use as a string.</param>
        public void UpdateValue(ushort elementID, string value)
        {
            if (value.ToLower() == "true" || value.ToLower() == "false")
            {
                UpdateValue(elementID, value.ToLower() == "true" ? true : false);
                return;
            }
            else if (value == "0" || value == "1")
            {
                UpdateValue(elementID, value == "1" ? true : false);
                return;
            }
            else
            {
                try
                {
                    UpdateValue(elementID, bool.Parse(value));
                    return;
                }
                catch (Exception ex)
                {
                    Debug.PrintLine("Exception occurred while updating Digital value.");
                    Debug.PrintLine(ex.Message);
                }
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
        /// Initializes the processor.
        /// </summary>
        /// <param name="managerID">The ID of the manager module this class should associate with.</param>
        public void Initialize(ushort managerID)
        {
            if (IsInitialized == true) { return; }
            var man = Manager.GetManagerByID(managerID);
            if (man != null)
            {
                man.AddDigital(this);
            }
            IsInitialized = true;
            ReportIsInitialized(1);
        }


    }

}