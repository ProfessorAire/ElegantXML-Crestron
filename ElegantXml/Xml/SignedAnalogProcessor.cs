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

        /// <summary>
        /// Used by Simpl+.
        /// </summary>
        public SignedAnalogProcessor()
            : base()
        {
            Elements = new List<SignedAnalogElement>();
        }

        public ushort Initialize(ushort managerID)
        {
            if (IsInitialized) { return 1; }
            try
            {
                if (Manager.AddProcessorToManager(managerID, this))
                {
                    ManagerId = managerID;
                    IsInitialized = true;
                }
                else
                {
                    Debug.PrintLine("Couldn't add Signed Analog Processor to manager, returning 0.");
                }
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while initializing Processor.");
                Debug.PrintLine(ex.ToString());
                return 0;
            }
            if (IsInitialized)
            {
                ReportIsInitialized(1);
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Adds an item to the processor's list of elements. Attempts to parse the path for a default value.
        /// </summary>
        /// <param name="elementID">The 1-based ID of the element, which should match the Simpl+ module parameter's index.</param>
        /// <param name="elementPath">The path provided by the Simpl+ module parameter.</param>
        /// <param name="defaultValue">The default value of the element.</param>
        public void AddValue(ushort elementID, string elementPath, short defaultValue)
        {
            try
            {
                var path = elementPath;
                short defVal = 0;
                if (elementPath.Contains(DefaultValueDelimiter))
                {
                    path = elementPath.Split(DefaultValueDelimiter)[0];
                    //Debug.PrintLine("Element " + elementID + "'s Path = " + path);
                    try
                    {
                        defVal = short.Parse(elementPath.Split(DefaultValueDelimiter)[1]);
                        //Debug.PrintLine("Element " + elementID + "'s DefaultValue = " + defVal);
                    }
                    catch
                    {
                        Debug.PrintLine("Couldn't parse default signed analog value from: " + elementPath);
                        defVal = defaultValue;
                    }
                }
                else
                {
                    defVal = defaultValue;
                }
                var element = new SignedAnalogElement(elementID, path, defVal);
                Elements.Add(element);
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while adding Signed Analog value.");
                Debug.PrintLine(ex.Message);
            }
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
                var element = Elements.Where((e) => e.ID == elementID).First();
                if (element == null)
                {
                    Debug.PrintLine("Couldn't find element to update Signed Analog value on.");
                    return;
                }
                element.AttributeValue = value;
                ReportValueChange(elementID, element.AttributeValue);
                Manager.SetManagerUpdateRequired(ManagerId, true);
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while updating Signed Analog value.");
                Debug.PrintLine(ex.Message);
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
                Debug.PrintLine("Exception ocurred while updating Signed Analog value.");
                Debug.PrintLine(ex.Message);
            }
        }

    }
}