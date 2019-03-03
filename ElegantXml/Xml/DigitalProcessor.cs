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

        private bool isInitialized = false;
        /// <summary>
        /// Returns true when the module has initialized correctly.
        /// </summary>
        public bool IsInitialized { get { return isInitialized; } set { isInitialized = value; } }

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
        public DigitalProcessor()
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
            using (var secure = new CCriticalSection())
            {
                var element = new DigitalElement(elementID, elementPath);
                element.AttributeValue = defaultValue > 0 ? true : false;
                Elements.Add(element);
            }
        }

        /// <summary>
        /// Flips the value of the element with the ID provided. 0 > 1 and 1 > 0.
        /// </summary>
        /// <param name="elementID">The 1-based ID of the element, which should match the Simpl+ module parameter's index.</param>
        public void ToggleValue(ushort elementID)
        {
            if (elementID < 1)
            {
                Debug.PrintLine("Couldn't toggle value for element. The index was invalid.");
                return;
            }
            using (var secure = new CCriticalSection())
            {
                var element = Elements.Where((e) => e.ID == elementID).First();
                if (element != null)
                {
                    element.AttributeValue = !element.AttributeValue;
                }
                ReportValueChange(elementID, element.AttributeValue == true ? (ushort)1 : (ushort)0);
                manager.IsSaveRequired(1);
            }

        }

        /// <summary>
        /// Updates the value of an element, both internally as well as to the Simpl+ module.
        /// </summary>
        /// <param name="elementID">The 1-based ID of the element, which should match the Simpl+ module parameter's index.</param>
        /// <param name="value">The new value to use as a boolean.</param>
        public void UpdateValue(ushort elementID, bool value)
        {
            if (elementID < 1)
            {
                Debug.PrintLine("Couldn't update value for Digital element. The index was invalid.");
                return;
            }
            using (var secure = new CCriticalSection())
            {
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