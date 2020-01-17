using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace ElegantXml.Xml
{
    public class SignedAnalogPropertyInterlock : ProcessorBase
    {
        /// <summary>
        /// A list of strings that are used to represent values in an XML attribute.
        /// </summary>
        public short[] PropertyValues { get; set; }

        /// <summary>
        /// The single SerialElement this interlock represents.
        /// </summary>
        public SignedAnalogElement Element { get; set; }
       
        public delegate void ReportValueChangeDelegate(ushort element, short value);
        /// <summary>
        /// Reports the selected element index back to the Simpl+ module.
        /// </summary>
        public ReportValueChangeDelegate ReportValueChange { get; set; }

        /// <summary>
        /// Used by Simpl+.
        /// </summary>
        public SignedAnalogPropertyInterlock()
            : base()
        {
        }

        public void SetSize(ushort quantity)
        {
            PropertyValues = new short[quantity];
        }

        public ushort Initialize(ushort managerID, string elementPath)
        {
            if (PropertyValues == null) { return 0; }
            if (IsInitialized) { return 1; }
            try
            {
                var path = elementPath;
                short defVal = 0;
                if (elementPath.Contains(DefaultValueDelimiter))
                {
                    path = elementPath.Split(DefaultValueDelimiter)[0];
                    try
                    {
                        defVal = short.Parse(elementPath.Split(DefaultValueDelimiter)[1]);
                    }
                    catch
                    {
                        Debug.PrintLine("Couldn't parse default signed analog value from: " + elementPath);
                    }
                }
                Element = new SignedAnalogElement(1, path, defVal);
                if (Manager.AddProcessorToManager(managerID, this))
                {
                    ManagerId = managerID;
                    IsInitialized = true;
                }
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while initializing Signed Analog Property Interlock.");
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

        public void AddPropertyValue(ushort propertyIndex, short propertyValue)
        {
            if (PropertyValues == null) { return; }
            PropertyValues[propertyIndex - 1] = propertyValue;
        }

        public void SelectValueByIndex(ushort propertyIndex)
        {
            if (!IsInitialized) { return; }
            if (propertyIndex < 1 || propertyIndex > PropertyValues.Count())
            {
                Debug.PrintLine("Couldn't update value for Signed Analog Property Interlock. The index value was out of range.");
                return;
            }
            try
            {
                if (Element.AttributeValue != PropertyValues[propertyIndex - 1])
                {
                    Element.AttributeValue = PropertyValues[propertyIndex - 1];
                    ReportValueChange(propertyIndex, Element.AttributeValue);
                }

            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while updating Signed Analog Property Interlock.");
                Debug.PrintLine(ex.Message);
            }
        }

        public void SelectValueByString(string value)
        {
            try
            {
                var newValue = short.Parse(value);
                SelectValue(newValue);
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while updating Signed Analog Property Interlock.");
                Debug.PrintLine(ex.Message);
            }
        }

        public void SelectValue(short value)
        {
            if (!IsInitialized) { return; }
            try
            {
                for (ushort i = 0; i < PropertyValues.Count(); i++)
                {
                    if (PropertyValues[i] == value)
                    {
                        SelectValueByIndex((ushort)(i + 1));
                        return;
                    }
                }
                Element.AttributeValue = value;
                ReportValueChange(0, value);
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while updating Signed Analog Property Interlock.");
                Debug.PrintLine(ex.Message);
            }
        }

        public void ClearValue()
        {
            if (!IsInitialized) { return; }
            try
            {
                Element.AttributeValue = 0;
                ReportValueChange(0, 0);
            }
            catch
            {
                Debug.PrintLine("Unable to clear values from Signed Analog Property Interlock.");
            }
        }
    }
}