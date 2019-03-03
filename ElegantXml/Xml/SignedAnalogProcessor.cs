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

        private bool isInitialized = false;
        /// <summary>
        /// Returns true when the module has initialized correctly.
        /// </summary>
        public bool IsInitialized { get { return isInitialized; } set { isInitialized = value; } }

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
            using (var secure = new CCriticalSection())
            {
                var element = new SignedAnalogElement(elementID, elementPath);
                element.AttributeValue = defaultValue;
                Elements.Add(element);
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
            if (elementID < 1)
            {
                CrestronConsole.PrintLine("Couldn't update value for element, due to null values.");
                return;
            }
            using (var secure = new CCriticalSection())
            {
                var element = Elements.Where((e) => e.ID == elementID).First();
                if (element == null)
                {
                    //CrestronConsole.PrintLine("Couldn't find element to update Signed Analog value on.");
                    return;
                }
                element.AttributeValue = value;
                ReportValueChange(elementID, element.AttributeValue);
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
            try
            {
                UpdateValue(elementID, short.Parse(value));
            }
            catch
            {
                //CrestronConsole.PrintLine("Couldn't update Signed Analog value.");
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