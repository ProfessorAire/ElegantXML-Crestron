using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace ElegantXml.Xml
{
    public class SignedAnalogProcessor : ProcessorBase
    {
        /// <summary>
        /// A list of the SignedAnalogElements associated with this processor.
        /// </summary>
        public List<SignedAnalogElement> Elements { get; set; }

        public delegate void ReportValueChangeDelegate(ushort element, short value);
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
        public SignedAnalogProcessor()
            : base()
        {
            Elements = new List<SignedAnalogElement>();
        }

        /// <summary>
        /// Adds an item to the processor's list of elements.
        /// </summary>
        /// <param name="elementID">The 1-based ID of the element, which should match the Simpl+ module parameter's index.</param>
        /// <param name="elementPath">The path provided by the Simpl+ module parameter.</param>
        /// <param name="defaultValue">The default value of the element.</param>
        public void AddValue(ushort elementID, string elementPath, short defaultValue)
        {
            try
            {
                CMonitor.Enter(this);
                var element = new SignedAnalogElement(elementID, elementPath, defaultValue);
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
                short defaultValue = 0;
                if (elementPath.Contains(DefaultValueDelimiter))
                {
                    path = elementPath.Split(DefaultValueDelimiter)[0];
                    Debug.PrintLine("Element " + elementID + "'s Path = " + path);
                    try
                    {
                        defaultValue = short.Parse(elementPath.Split(DefaultValueDelimiter)[1]);
                        Debug.PrintLine("Element " + elementID + "'s DefaultValue = " + defaultValue);
                    }
                    catch { Debug.PrintLine("Couldn't parse default signed analog value from: " + elementPath); }
                }
                var element = new SignedAnalogElement(elementID, path, defaultValue);
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
        /// <param name="value">The new value to use as a short.</param>
        public void UpdateValue(ushort elementID, short value)
        {
            if (!IsInitialized) { return; }
            if (elementID < 1)
            {
                CrestronConsole.PrintLine("Couldn't update value for Signed Analog element. The index was invalid.");
                return;
            }
            try
            {
                if (Elements == null ||
                    Elements.Count <= 0 ||
                    Elements.Where((e) => e.ID == elementID).Count() == 0)
                {
                    Debug.PrintLine("No elements present to update Signed Analog value on.");
                    return;
                }
                CMonitor.Enter(this);
                var element = Elements.Where((e) => e.ID == elementID).First();
                if (element == null)
                {
                    Debug.PrintLine("Couldn't find element to update Signed Analog value on.");
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
        /// Updates the value of an element, both internally as well as to the Simpl+ module.
        /// </summary>
        /// <param name="elementID">The 1-based ID of the element, which should match the Simpl+ module parameter's index.</param>
        /// <param name="value">The new value to use as a string.</param>
        public void UpdateValue(ushort elementID, string value)
        {
            try
            {
                UpdateValue(elementID, short.Parse(value));
                return;
            }
            catch(Exception ex)
            {
                Debug.PrintLine("Exception ocurred while updating Analog value.");
                Debug.PrintLine(ex.Message);
            }
        }

        /// <summary>
        /// Initializes the processor.
        /// </summary>
        /// <param name="managerID">The ID of the manager module this class should associate with.</param>
        public void Initialize(ushort managerID)
        {
            if (IsInitialized) { return; }
            var man = Manager.GetManagerByID(managerID);
            if (man != null)
            {
                man.AddSignedAnalog(this);
                IsInitialized = true;
                ReportIsInitialized(1);
            }

        }


    }
}