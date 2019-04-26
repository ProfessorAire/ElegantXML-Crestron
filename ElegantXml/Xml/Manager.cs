using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.CrestronXml;
using Crestron.SimplSharp.CrestronXmlLinq;
using Crestron.SimplSharp;

namespace ElegantXml.Xml
{
    public class Manager
    {
        /// <summary>
        /// A list of the Manager classes that have been registered in the program.
        /// </summary>
        private static List<Manager> Managers { get; set; }

        /// <summary>
        /// A list of the DigitalProcessors associated with this manager.
        /// </summary>
        private List<DigitalProcessor> DigitalProcessors { get; set; }
        /// <summary>
        /// A list of the AnalogProcessors associated with this manager.
        /// </summary>
        private List<AnalogProcessor> AnalogProcessors { get; set; }
        /// <summary>
        /// A list of the SignedAnalogProcessors associated with this manager.
        /// </summary>
        private List<SignedAnalogProcessor> SignedAnalogProcessors { get; set; }
        /// <summary>
        /// A list of the SerialProcessors associated with this manager.
        /// </summary>
        private List<SerialProcessor> SerialProcessors { get; set; }

        /// <summary>
        /// Private XDocument used to perform operations on the XML file.
        /// </summary>
        private XDocument XmlDoc { get; set; }

        /// <summary>
        /// The name of the root element of the XML file.
        /// </summary>
        private string RootElement { get; set; }

        /// <summary>
        /// The unique ID of the manager class. This allows multiple managers to be used to read different files,
        /// each with their own processors.
        /// </summary>
        public ushort ID { get; set; }

        /// <summary>
        /// The full path (with file name) of the file to read.
        /// </summary>
        private string FilePath { get; set; }

        /// <summary>
        /// Returns the FileName, extracted from the FilePath property.
        /// </summary>
        private string FileName { get { return Path.GetFileName(FilePath); } }

        /// <summary>
        /// Returns ushort values to the Simpl+ module.
        /// </summary>
        /// <param name="state">The ushort value to return.</param>
        public delegate void UShortDelegate(ushort state);

        /// <summary>
        /// Notifies the Simpl+ module of the loading state.
        /// </summary>
        public UShortDelegate IsLoading { get; set; }

        /// <summary>
        /// Notifies the Simpl+ module of the saving state.
        /// </summary>
        public UShortDelegate IsSaving { get; set; }

        /// <summary>
        /// Notifies the Simpl+ module if saving is required.
        /// </summary>
        public UShortDelegate IsSaveRequired { get; set; }

        /// <summary>
        /// Generic delegate that can be used to pass messages that don't require values.
        /// </summary>
        public delegate void NotificationDelegate();

        /// <summary>
        /// Notifies the Simpl+ module that a load operation was successful.
        /// </summary>
        public NotificationDelegate LoadSuccess { get; set; }

        /// <summary>
        /// Notifies the Simpl+ module that a save operation was successful.
        /// </summary>
        public NotificationDelegate SaveSuccess { get; set; }

        /// <summary>
        /// Reports the current operation's progress to the Simpl+ program.
        /// </summary>
        public UShortDelegate ReportProgress { get; set; }

        /// <summary>
        /// Passes progress to the ReportProgress delegate, forcing the Crestron environment to allow other apps to process.
        /// </summary>
        /// <param name="progress"></param>
        private void YieldProgress(ushort progress)
        {
            ReportProgress(progress);
            CrestronEnvironment.Sleep(0);
            CrestronEnvironment.AllowOtherAppsToRun();
        }

        /// <summary>
        /// Returns string values to the Simpl+ module.
        /// </summary>
        /// <param name="message"></param>
        public delegate void PassMessageDelegate(SimplSharpString message);

        /// <summary>
        /// Passes a load failure message to the Simpl+ module.
        /// </summary>
        public PassMessageDelegate LoadFailure { get; set; }

        /// <summary>
        /// Passes a save failure message to the Simpl+ module.
        /// </summary>
        public PassMessageDelegate SaveFailure { get; set; }


        /// <summary>
        /// Provided for Simpl+ compatibility.
        /// Use the Initialize method to prepare the class.
        /// </summary>
        [Obsolete("Provided for Simpl+ compatibility.")]
        public Manager() { }

        /// <summary>
        /// Must be run before any other functions are called!
        /// </summary>
        /// <param name="id">The unique ID of this manager class.</param>
        /// <param name="fileName">The file name of the config file, without a path.</param>
        /// <param name="rootElement">The name of the root element in the XML file.</param>
        public void Initialize(ushort id, string fileName, string rootElement)
        {
            Debug.PrintLine("Initializing XML Manager: " + id);
            RootElement = rootElement;

            if (fileName.StartsWith(@"\") == false)
            {
                string path = Path.Combine(@"\User\Config\App" + InitialParametersClass.ApplicationNumber, fileName);
                FilePath = path;
                
                path = Path.Combine(@"\ROMDISK\Config\App" + InitialParametersClass.ApplicationNumber, fileName);
                if (File.Exists(path))
                {
                    FilePath = path;
                }
            }
            else
            {
                FilePath = fileName;
            }

            if (!File.Exists(FilePath))
            {
                try
                {
                    using (var f = File.Create(FilePath))
                    {
                        Debug.PrintLine("Couldn't find configuration file: " + fileName);
                        Debug.PrintLine("Created new blank configuration file");
                        SaveFile();
                    }
                }
                catch (Exception ex)
                {
                    Debug.PrintLine("Error creating configuration file: " + fileName);
                    Debug.PrintLine("Message reads: " + ex.Message);
                }
            }

            ID = id;

            Manager.AddManager(this);
            Debug.PrintLine("Initialized XML Manager: " + id + " with path: " + FilePath);
        }

        /// <summary>
        /// Adds an Analog processor to this manager instance.
        /// </summary>
        /// <param name="proc">The processor to add.</param>
        public void AddAnalog(AnalogProcessor proc)
        {
            using (var secure = new CCriticalSection())
            {
                if (AnalogProcessors == null)
                {
                    AnalogProcessors = new List<AnalogProcessor>();
                }
                AnalogProcessors.Add(proc);
                Debug.PrintLine("Added analog XML processor.");
            }
        }

        /// <summary>
        /// Adds a SignedAnalog processor to this manager instance.
        /// </summary>
        /// <param name="proc">The processor to add.</param>
        public void AddSignedAnalog(SignedAnalogProcessor proc)
        {
            using (var secure = new CCriticalSection())
            {
                if (SignedAnalogProcessors == null)
                {
                    SignedAnalogProcessors = new List<SignedAnalogProcessor>();
                }
                SignedAnalogProcessors.Add(proc);
                Debug.PrintLine("Added signed analog XML processor.");
            }
        }

        /// <summary>
        /// Adds a Digital processor to this manager instance.
        /// </summary>
        /// <param name="proc">The processor to add.</param>
        public void AddDigital(DigitalProcessor proc)
        {
            using (var secure = new CCriticalSection())
            {
                if (DigitalProcessors == null)
                {
                    DigitalProcessors = new List<DigitalProcessor>();
                }
                DigitalProcessors.Add(proc);
                Debug.PrintLine("Added digital XML processor.");
            }
        }

        /// <summary>
        /// Adds a Serial processor to this manager instance.
        /// </summary>
        /// <param name="proc">The processor to add.</param>
        public void AddSerial(SerialProcessor proc)
        {
            using (var secure = new CCriticalSection())
            {
                if (SerialProcessors == null)
                {
                    SerialProcessors = new List<SerialProcessor>();
                }
                SerialProcessors.Add(proc);
                Debug.PrintLine("Added serial XML processor.");
            }
        }

        /// <summary>
        /// Loads data from the file and overwrites any existing values.
        /// </summary>
        public void LoadFile()
        {
            ReportProgress(0);
            IsLoading(1);
            ushort step = (ushort)(65535 / GetTotalElements());
            ushort current = 0;
            CrestronConsole.PrintLine(DateTime.Now.ToShortDateString() + "|" + DateTime.Now.ToLongTimeString() + "|Loading xml file: " + FilePath);
            using (var secure = new CCriticalSection())
            {
                XDocument doc = null;
                try
                {
                    var content = File.ReadToEnd(FilePath, Encoding.UTF8);
                    if (content == "")
                    {
                        Debug.PrintLine("Empty file found.\nSaving defaults to file to create structure.");
                        this.SaveFile();
                        Debug.PrintLine("Default file created.");
                        return;
                    }
                    var start = content.IndexOf("<?xml", 0);
                    if (start < 0)
                    {
                        Debug.PrintLine("Couldn't find xml element to parse in the file.");
                        CrestronConsole.PrintLine(DateTime.Now.ToShortDateString() + "|" + DateTime.Now.ToLongTimeString() + "| Failed to load xml file.");
                        LoadFailure("Invalid XML file couldn't be loaded.");
                        IsLoading(0);
                        return;
                    }
                    content = content.Substring(start);
                    using (var reader = new XmlReader(content, null))
                    {
                        reader.MoveToContent();
                        doc = XDocument.Load(reader);
                    }
                }
                catch (Exception ex)
                {
                    Debug.PrintLine("Exception occurred when loading XML document into XDocument variable.");
                    Debug.PrintLine(ex.Message);
                    CrestronConsole.PrintLine(DateTime.Now.ToShortDateString() + "|" + DateTime.Now.ToLongTimeString() + "| Failed to load xml file.");
                    LoadFailure("Couldn't correctly parse the XML document provided.");
                    IsLoading(0);
                    return;
                }
                if (doc == null || doc.Root == null || doc.Root.IsEmpty)
                {
                    CrestronConsole.PrintLine("Couldn't load XML document. No document exists, or the document is empty.");
                    CrestronConsole.PrintLine(DateTime.Now.ToShortDateString() + "|" + DateTime.Now.ToLongTimeString() + "| Failed to load xml file.");
                    LoadFailure("The XML document provided is empty, missing, or invalid.");
                    IsLoading(0);
                    return;
                }
                var value = "";
                if (DigitalProcessors != null)
                {
                    for (var i = 0; i < DigitalProcessors.Count; i++)
                    {
                        for (ushort j = 0; j < DigitalProcessors[i].Elements.Count; j++)
                        {
                            value = FindValue(DigitalProcessors[i].Elements[j].AttributePath, ref doc);
                            try
                            {
                                DigitalProcessors[i].UpdateValue(DigitalProcessors[i].Elements[j].ID, value);
                            }
                            catch
                            {
                                LoadFailure("Error processing digital value for path: " + DigitalProcessors[i].Elements[j].AttributePath);
                                Debug.PrintLine("Error processing digital value for path: " + DigitalProcessors[i].Elements[j].AttributePath);
                            }
                            current += step;
                            YieldProgress(current);
                        }
                    }
                }
                if (AnalogProcessors != null)
                {
                    for (var i = 0; i < AnalogProcessors.Count; i++)
                    {
                        for (ushort j = 0; j < AnalogProcessors[i].Elements.Count; j++)
                        {
                            value = FindValue(AnalogProcessors[i].Elements[j].AttributePath, ref doc);
                            try
                            {
                                AnalogProcessors[i].UpdateValue(AnalogProcessors[i].Elements[j].ID, ushort.Parse(value));
                            }
                            catch
                            {
                                LoadFailure("Error processing analog value for path: " + DigitalProcessors[i].Elements[j].AttributePath);
                                Debug.PrintLine("Error processing analog value for path: " + DigitalProcessors[i].Elements[j].AttributePath);
                            }
                            current += step;
                            YieldProgress(current);
                        }
                    }
                }
                if (SignedAnalogProcessors != null)
                {
                    for (var i = 0; i < SignedAnalogProcessors.Count; i++)
                    {
                        for (ushort j = 0; j < SignedAnalogProcessors[i].Elements.Count; j++)
                        {
                            value = FindValue(SignedAnalogProcessors[i].Elements[j].AttributePath, ref doc);
                            try
                            {
                                SignedAnalogProcessors[i].UpdateValue(SignedAnalogProcessors[i].Elements[j].ID, short.Parse(value));
                            }
                            catch
                            {
                                LoadFailure("Error processing signed analog value for path: " + DigitalProcessors[i].Elements[j].AttributePath);
                                Debug.PrintLine("Error processing signed analog value for path: " + DigitalProcessors[i].Elements[j].AttributePath);
                            }
                            current += step;
                            YieldProgress(current);
                        }
                    }
                }
                if (SerialProcessors != null)
                {
                    for (var i = 0; i < SerialProcessors.Count; i++)
                    {
                        for (ushort j = 0; j < SerialProcessors[i].Elements.Count; j++)
                        {
                            value = FindValue(SerialProcessors[i].Elements[j].AttributePath, ref doc);
                            try
                            {
                                SerialProcessors[i].UpdateValue(SerialProcessors[i].Elements[j].ID, value);
                            }
                            catch
                            {
                                LoadFailure("Error processing serial value for path: " + DigitalProcessors[i].Elements[j].AttributePath);
                                Debug.PrintLine("Error processing serial value for path: " + DigitalProcessors[i].Elements[j].AttributePath);
                            }
                            current += step;
                            YieldProgress(current);
                        }
                    }
                }
                XmlDoc = doc;
            }
            CrestronConsole.PrintLine(DateTime.Now.ToShortDateString() + "|" + DateTime.Now.ToLongTimeString() + "|Finished loading xml file:" + FileName);
            LoadSuccess();
            IsLoading(0);
            IsSaveRequired(0);
        }

        /// <summary>
        /// Used publicly to find the value of a path element provided by a companion Simpl+ module.
        /// </summary>
        private string FindValue(string path, ref XDocument doc)
        {
            XElement element = doc.Elements().First();
            var parts = path.Split('.');
            string[] slice = null;
            var name = "";
            var attribute = "";
            var value = "";
            try
            {
                for (var i = 0; i < parts.Length - 1; i++)
                {
                    if (element == null)
                    {
                        Debug.PrintLine("Couldn't find value for the path: " + path);
                        return "";
                    }

                    if (parts[i].Contains(" "))
                    {
                        slice = parts[i].Split(' ');
                        name = slice[0].Replace(" ", "");
                        attribute = slice[1].Replace(" ", "");
                        value = attribute.Split('=')[1].Replace("=", "").Replace("\"", "");
                        attribute = attribute.Split('=')[0].Replace("=", "");
                        element = element.Elements().Where((e) => e.Name.ToLower() == name.ToLower() &&
                            e.Attributes().Where((a) => a.Name.ToLower() == attribute.ToLower()
                                && a.Value.ToLower() == value.ToLower()).FirstOrDefault() != null).FirstOrDefault();
                    }
                    else
                    {
                        name = parts[i];
                        element = element.Elements().Where((e) => e.Name.ToLower() == name.ToLower()).FirstOrDefault();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Error finding element when loading xml file: " + FileName);
                Debug.PrintLine(ex.Message);
                return null;
            }

            try
            {
                value = element.Attributes().Where((a) => a.Name.ToLower() == parts[parts.Length - 1].ToLower()).FirstOrDefault().Value;
                return value;
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Error parsing attribute value: " + path + " while loading configuration file: ");
                Debug.PrintLine(ex.Message);
                return "";
            }

        }

        /// <summary>
        /// Used to save the file. If the construction of the XML file fails, it won't overwrite the existing file.
        /// </summary>
        public void SaveFile()
        {
            IsSaving(1);
            CrestronConsole.PrintLine(DateTime.Now.ToShortDateString() + "|" + DateTime.Now.ToLongTimeString() + "|Starting XML file saving for file: " + FilePath);

            using (var secure = new CCriticalSection())
            {
                XmlBuilder builder = null;
                if (XmlDoc != null)
                {
                    builder = new XmlBuilder(RootElement, XmlDoc);
                }
                else
                {
                    builder = new XmlBuilder(RootElement);
                }
                if (DigitalProcessors != null && DigitalProcessors.Count > 0)
                {
                    if (!builder.WriteDigitals(DigitalProcessors))
                    {
                        SaveFailure("Unable to write all Digital elements.");
                        Debug.PrintLine("Unable to write all Digital elements.");
                        IsSaving(0);
                        return;

                    }
                }
                if (AnalogProcessors != null && AnalogProcessors.Count > 0)
                {
                    if (!builder.WriteAnalogs(AnalogProcessors))
                    {
                        SaveFailure("Unable to write all Analog elements.");
                        Debug.PrintLine("Unable to write all Analog elements.");
                        IsSaving(0);
                        return;
                    }
                }
                if (SignedAnalogProcessors != null && SignedAnalogProcessors.Count > 0)
                {
                    if (!builder.WriteSignedAnalogs(SignedAnalogProcessors))
                    {
                        SaveFailure("Unable to write all Signed Analog elements.");
                        Debug.PrintLine("Unable to write all Signed Analog elements.");
                        IsSaving(0);
                        return;
                    }
                }
                if (SerialProcessors != null && SerialProcessors.Count > 0)
                {
                    if (!builder.WriteSerials(SerialProcessors))
                    {
                        SaveFailure("Unable to write all Serial elements.");
                        Debug.PrintLine("Unable to write all Serial elements.");
                        IsSaving(0);
                        return;
                    }
                }
                try
                {
                    if (builder.Save(FilePath))
                    {
                        CrestronConsole.PrintLine(DateTime.Now.ToShortDateString() + "|" + DateTime.Now.ToLongTimeString() + "|XML file: " + FileName + " saved!");
                        SaveSuccess();
                        IsSaving(0);
                        IsSaveRequired(0);
                        XmlDoc = builder.Document;
                        return;
                    }
                    else
                    {
                        CrestronConsole.PrintLine(DateTime.Now.ToShortDateString() + "|" + DateTime.Now.ToLongTimeString() + "|XML file: " + FileName + " not saved!");
                        SaveFailure("Error while saving.");
                        IsSaving(0);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.PrintLine("Exception encountered while saving XML file to disk.");
                    Debug.PrintLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// Adds a manager to the public static list of managers.
        /// </summary>
        /// <param name="processor"></param>
        public static void AddManager(Manager processor)
        {
            using (var secure = new CCriticalSection())
            {
                if (Managers == null)
                {
                    Managers = new List<Manager>();
                }
                Managers.Add(processor);
            }
        }

        /// <summary>
        /// Returns a Manager object, if a manager with a matching ID exists, otherwise returns null.
        /// </summary>
        /// <param name="ID">The ID of the manager to return.</param>
        /// <returns></returns>
        public static Manager GetManagerByID(ushort ID)
        {
            if (Managers == null || Managers.Count <= 0) { return null; }
            using (var secure = new CCriticalSection())
            {
                var man = Managers.Where((p) => p.ID == ID).First();
                return man;
            }
        }

        /// <summary>
        /// Returns a total number of elements in all the manager's associated processors.
        /// Primarly used for calculating a progress percentage for save/load operaations.
        /// </summary>
        /// <returns></returns>
        public ushort GetTotalElements()
        {
            using (var secure = new CCriticalSection())
            {
                var total = 0;
                if (DigitalProcessors != null)
                {
                    for (var i = 0; i < DigitalProcessors.Count; i++)
                    {
                        total += DigitalProcessors[i].Elements.Count;
                    }
                }
                if (AnalogProcessors != null)
                {
                    for (var i = 0; i < AnalogProcessors.Count; i++)
                    {
                        total += AnalogProcessors[i].Elements.Count;
                    }
                }
                if (SignedAnalogProcessors != null)
                {
                    for (var i = 0; i < SignedAnalogProcessors.Count; i++)
                    {
                        total += SignedAnalogProcessors[i].Elements.Count;
                    }
                }
                if (SerialProcessors != null)
                {
                    for (var i = 0; i < SerialProcessors.Count; i++)
                    {
                        total += SerialProcessors[i].Elements.Count;
                    }
                }
                if (total == 0)
                {
                    total = 1;
                }
                return (ushort)total;
            }
        }

        /// <summary>
        /// Enables printing Debug messages to the console.
        /// </summary>
        public void DebugOn()
        {
            Debug.isDebugEnabled = true;
        }

        /// <summary>
        /// Disables printing Debug messages to the console. Certain messages are printed even if this is off.
        /// </summary>
        public void DebugOff()
        {
            Debug.isDebugEnabled = false;
        }

    }
}
