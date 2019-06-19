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
        /// The path delimiter to use when splitting paths.
        /// </summary>
        public char PathDelimiter { get; set; }

        /// <summary>
        /// Sets or retrieves the delimiter used to separate the path from the default value in the Simpl+ parameter.
        /// </summary>
        public char DefaultValueDelimiter { get; set; }

        public void SetDefaultValueDelimiter(string DelimiterCharacter)
        {
            DefaultValueDelimiter = DelimiterCharacter[0];
        }

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

        private object _crit = new Object();

        /// <summary>
        /// Provided for Simpl+ compatibility.
        /// Use the Initialize method to prepare the class.
        /// </summary>
        [Obsolete("Provided for Simpl+ compatibility.")]
        public Manager() { PathDelimiter = '.'; }


        /// <summary>
        /// Must be run before any other functions are called!
        /// </summary>
        /// <param name="id">The unique ID of this manager class.</param>
        /// <param name="fileName">The file name of the config file, without a path.</param>
        /// <param name="rootElement">The name of the root element in the XML file.</param>
        /// <param name="delimiter">The default path delimiter to use when parsing the file.</param>
        public void InitializeWithDelimiters(ushort id, string fileName, string rootElement, string pathDelimiter, string valueDelimiter)
        {
            if (pathDelimiter != "")
            {
                PathDelimiter = pathDelimiter[0];
            }
            SetDefaultValueDelimiter(valueDelimiter);
            Initialize(id, fileName, rootElement);
        }

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
            try
            {
                CMonitor.Enter(_crit);
                if (AnalogProcessors == null)
                {
                    AnalogProcessors = new List<AnalogProcessor>();
                }
                AnalogProcessors.Add(proc);
                Debug.PrintLine("Added analog XML processor.");
            }
            finally
            {
                CMonitor.Exit(_crit);
            }
        }

        /// <summary>
        /// Adds a SignedAnalog processor to this manager instance.
        /// </summary>
        /// <param name="proc">The processor to add.</param>
        public void AddSignedAnalog(SignedAnalogProcessor proc)
        {
            try
            {
                CMonitor.Enter(_crit);
                if (SignedAnalogProcessors == null)
                {
                    SignedAnalogProcessors = new List<SignedAnalogProcessor>();
                }
                SignedAnalogProcessors.Add(proc);
                Debug.PrintLine("Added signed analog XML processor.");
            }
            finally
            {
                CMonitor.Exit(_crit);
            }
        }

        /// <summary>
        /// Adds a Digital processor to this manager instance.
        /// </summary>
        /// <param name="proc">The processor to add.</param>
        public void AddDigital(DigitalProcessor proc)
        {
            try
            {
                CMonitor.Enter(_crit);
                if (DigitalProcessors == null)
                {
                    DigitalProcessors = new List<DigitalProcessor>();
                }
                DigitalProcessors.Add(proc);
                Debug.PrintLine("Added digital XML processor.");
            }
            finally
            {
                CMonitor.Exit(_crit);
            }
        }

        /// <summary>
        /// Adds a Serial processor to this manager instance.
        /// </summary>
        /// <param name="proc">The processor to add.</param>
        public void AddSerial(SerialProcessor proc)
        {
            try
            {
                CMonitor.Enter(_crit);
                if (SerialProcessors == null)
                {
                    SerialProcessors = new List<SerialProcessor>();
                }
                SerialProcessors.Add(proc);
                Debug.PrintLine("Added serial XML processor.");
            }
            finally
            {
                CMonitor.Exit(_crit);
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
            try
            {
                CMonitor.Enter(_crit);
                XDocument doc = null;
                try
                {
                    if (!Crestron.SimplSharp.CrestronIO.File.Exists(FilePath))
                    {
                        Debug.PrintLine("Trying to write empty XML Document since none was found.");
                        var newDoc = new XDocument();
                        newDoc.Add(new XElement(RootElement));
                        newDoc.Save(FilePath);
                    }
                    var content = Crestron.SimplSharp.CrestronIO.File.ReadToEnd(FilePath, Encoding.UTF8);
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
                    var settings = new XmlReaderSettings();
                    settings.IgnoreComments = false;
                    using (var reader = new XmlReader(content, settings))
                    {
                        reader.MoveToContent();
                        doc = XDocument.Load(reader);
                        Debug.PrintLine("There are " + doc.Nodes().Where((n) => n.NodeType == XmlNodeType.Comment).Count() + " comments in this file.");
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
                if (doc == null || doc.Root == null)
                {
                    CrestronConsole.PrintLine("Couldn't load XML document. No document exists, or the document is empty.");
                    CrestronConsole.PrintLine(DateTime.Now.ToShortDateString() + "|" + DateTime.Now.ToLongTimeString() + "| Failed to load xml file.");
                    LoadFailure("The XML document provided is empty, missing, or invalid.");
                    IsLoading(0);
                    return;
                }

                Debug.PrintLine("Starting to process XML elements.");

                var value = "";
                if (DigitalProcessors != null)
                {
                    Debug.PrintLine("Processing Digitals");
                    for (var i = 0; i < DigitalProcessors.Count; i++)
                    {
                        for (ushort j = 0; j < DigitalProcessors[i].Elements.Count; j++)
                        {
                            try
                            {
                                if (TryFindValue(DigitalProcessors[i].Elements[j].AttributePath, ref doc, out value))
                                {
                                    Debug.PrintLine("Setting value.");
                                    DigitalProcessors[i].UpdateValue(DigitalProcessors[i].Elements[j].ID, value);
                                }
                                else
                                {
                                    Debug.PrintLine("Setting value with default.");
                                    DigitalProcessors[i].UpdateValue(DigitalProcessors[i].Elements[j].ID, DigitalProcessors[i].Elements[j].DefaultValue);
                                }
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
                    Debug.PrintLine("Processing Analogs");
                    for (var i = 0; i < AnalogProcessors.Count; i++)
                    {
                        for (ushort j = 0; j < AnalogProcessors[i].Elements.Count; j++)
                        {
                            try
                            {
                                if (TryFindValue(AnalogProcessors[i].Elements[j].AttributePath, ref doc, out value))
                                {
                                    Debug.PrintLine("Setting value.");
                                    AnalogProcessors[i].UpdateValue(AnalogProcessors[i].Elements[j].ID, value);
                                }
                                else
                                {
                                    Debug.PrintLine("Setting value with default.");
                                    AnalogProcessors[i].UpdateValue(AnalogProcessors[i].Elements[j].ID, AnalogProcessors[i].Elements[j].DefaultValue);
                                }
                            }
                            catch
                            {
                                LoadFailure("Error processing analog value for path: " + AnalogProcessors[i].Elements[j].AttributePath);
                                Debug.PrintLine("Error processing analog value for path: " + AnalogProcessors[i].Elements[j].AttributePath);
                            }
                            current += step;
                            YieldProgress(current);
                        }
                    }
                }

                if (SignedAnalogProcessors != null)
                {
                    Debug.PrintLine("Processing SignedAnalogs");
                    for (var i = 0; i < SignedAnalogProcessors.Count; i++)
                    {
                        for (ushort j = 0; j < SignedAnalogProcessors[i].Elements.Count; j++)
                        {
                            try
                            {
                                if (TryFindValue(SignedAnalogProcessors[i].Elements[j].AttributePath, ref doc, out value))
                                {
                                    Debug.PrintLine("Setting value.");
                                    SignedAnalogProcessors[i].UpdateValue(SignedAnalogProcessors[i].Elements[j].ID, value);
                                }
                                else
                                {
                                    Debug.PrintLine("Setting value with default.");
                                    SignedAnalogProcessors[i].UpdateValue(SignedAnalogProcessors[i].Elements[j].ID, SignedAnalogProcessors[i].Elements[j].DefaultValue);
                                }
                            }
                            catch
                            {
                                LoadFailure("Error processing signed analog value for path: " + SignedAnalogProcessors[i].Elements[j].AttributePath);
                                Debug.PrintLine("Error processing signed analog value for path: " + SignedAnalogProcessors[i].Elements[j].AttributePath);
                            }
                            current += step;
                            YieldProgress(current);
                        }
                    }
                }
                if (SerialProcessors != null)
                {
                    Debug.PrintLine("Processing Serials");
                    Debug.PrintLine("There are " + SerialProcessors.Count + " serial processors.");
                    for (var i = 0; i < SerialProcessors.Count; i++)
                    {
                        for (ushort j = 0; j < SerialProcessors[i].Elements.Count; j++)
                        {
                            try
                            {
                                if (TryFindValue(SerialProcessors[i].Elements[j].AttributePath, ref doc, out value))
                                {
                                    Debug.PrintLine("Setting value.");
                                    SerialProcessors[i].UpdateValue(SerialProcessors[i].Elements[j].ID, value);
                                }
                                else
                                {
                                    Debug.PrintLine("Setting value with default.");
                                    SerialProcessors[i].UpdateValue(SerialProcessors[i].Elements[j].ID, SerialProcessors[i].Elements[j].DefaultValue);
                                }
                            }
                            catch
                            {
                                LoadFailure("Error processing serial value for path: " + SerialProcessors[i].Elements[j].AttributePath);
                                Debug.PrintLine("Error processing serial value for path: " + SerialProcessors[i].Elements[j].AttributePath);
                            }
                            current += step;
                            YieldProgress(current);
                        }
                    }
                }
                XmlDoc = doc;
            }
            finally
            {
                CMonitor.Exit(_crit);
            }
            CrestronConsole.PrintLine(DateTime.Now.ToShortDateString() + "|" + DateTime.Now.ToLongTimeString() + "|Finished loading xml file:" + FileName);
            LoadSuccess();
            IsLoading(0);
            IsSaveRequired(0);
        }

        /// <summary>
        /// Used publicly to find the value of a path element provided by a companion Simpl+ module.
        /// Includes a default value to pass to the element's output if no default is found.
        /// </summary>
        private bool TryFindValue(string path, ref XDocument doc, out string outValue)
        {
            Debug.PrintLine("Finding value in: " + path);
            XElement element = doc.Elements().First();
            var parts = path.Split(PathDelimiter);
            string[] slice = null;
            var name = "";
            var attribute = "";
            var value = "";
            outValue = "";

            var isElement = false;
            if (parts.Last().Contains("=")) { isElement = true; }
            if (parts.Last() == "" || parts.Last() == string.Empty) { isElement = true; }

            try
            {
                for (var i = 0; i < parts.Length - 1; i++)
                {
                    if (element == null)
                    {
                        Debug.PrintLine("Couldn't find value for the path: " + path);
                        return false;
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
                return false;
            }

            if (element == null)
            {
                Debug.PrintLine("Couldn't find value for the path: " + path);
                return false;
            }

            try
            {
                Debug.PrintLine("Finding a value. Element " + element.Name + " has " + (element.HasAttributes ? element.Attributes().Count() : 0) + " attributes.");
                Debug.PrintLine("Finding a value. Element " + element.Name + " has " + (element.HasElements ? element.Elements().Count() : 0) + " elements.");

                if (isElement)
                {
                    outValue = element.Value;
                    Debug.PrintLine("Set value to: " + outValue);
                }
                else
                {
                    Debug.PrintLine("Looking for part: " + parts[parts.Length - 1]);
                    if (element.HasAttributes && element.Attributes().Where((a) => a.Name.ToLower() == parts[parts.Length - 1].ToLower()).Count() > 0)
                    {
                        outValue = element.Attributes().Where((a) => a.Name.ToLower() == parts[parts.Length - 1].ToLower()).FirstOrDefault().Value;
                        Debug.PrintLine("Set value to: " + outValue);
                    }
                    else if (element.HasElements && element.Elements().Where((e) => e.Name.ToLower() == parts[parts.Length - 1].ToLower()).Count() > 0)
                    {
                        outValue = element.Elements().Where((e) => e.Name.ToLower() == parts[parts.Length - 1].ToLower()).FirstOrDefault().Value;
                        Debug.PrintLine("Set value to: " + outValue);
                    }
                    else
                    {
                        Debug.PrintLine("Couldn't find a value.");
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Error parsing attribute value: " + path + " while loading configuration file: ");
                Debug.PrintLine(ex.Message);
                return false;
            }

        }

        /// <summary>
        /// Used to save the file. If the construction of the XML file fails, it won't overwrite the existing file.
        /// </summary>
        public void SaveFile()
        {
            IsSaving(1);
            CrestronConsole.PrintLine(DateTime.Now.ToShortDateString() + "|" + DateTime.Now.ToLongTimeString() + "|Starting XML file saving for file: " + FilePath);
            try
            {
                CMonitor.Enter(_crit);
                XmlBuilder builder = null;
                if (XmlDoc != null)
                {
                    builder = new XmlBuilder(RootElement, XmlDoc, PathDelimiter);
                }
                else
                {
                    builder = new XmlBuilder(RootElement, PathDelimiter);
                }
                if (DigitalProcessors != null && !builder.WriteDigitals(DigitalProcessors))
                {
                    SaveFailure("Unable to write all Digital elements.");
                    Debug.PrintLine("Unable to write all Digital elements.");
                    IsSaving(0);
                    return;
                }
                if (AnalogProcessors != null && !builder.WriteAnalogs(AnalogProcessors))
                {
                    if (!builder.WriteAnalogs(AnalogProcessors))
                    {
                        SaveFailure("Unable to write all Analog elements.");
                        Debug.PrintLine("Unable to write all Analog elements.");
                        IsSaving(0);
                        return;
                    }
                }
                if (SignedAnalogProcessors != null && !builder.WriteSignedAnalogs(SignedAnalogProcessors))
                {
                    if (!builder.WriteSignedAnalogs(SignedAnalogProcessors))
                    {
                        SaveFailure("Unable to write all Signed Analog elements.");
                        Debug.PrintLine("Unable to write all Signed Analog elements.");
                        IsSaving(0);
                        return;
                    }
                }
                if (SerialProcessors != null && !builder.WriteSerials(SerialProcessors))
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
                    SaveFailure("Exception while saving.");
                    IsSaving(0);
                }
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while saving XML file to disk.");
                Debug.PrintLine(ex.Message);
                SaveFailure("Exception while saving.");
                IsSaving(0);
            }
            finally
            {
                CMonitor.Exit(_crit);
            }
        }

        private static object _crits = new Object();

        /// <summary>
        /// Adds a manager to the public static list of managers.
        /// </summary>
        /// <param name="processor"></param>
        public static void AddManager(Manager processor)
        {
            try
            {
                CMonitor.Enter(_crits);
                if (Managers == null)
                {
                    Managers = new List<Manager>();
                }
                Managers.Add(processor);
            }
            finally
            {
                CMonitor.Exit(_crits);
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
            try
            {
                CMonitor.Enter(_crits);
                var man = Managers.Where((p) => p.ID == ID).First();
                return man;
            }
            finally
            {
                CMonitor.Exit(_crits);
            }
        }

        /// <summary>
        /// Returns a total number of elements in all the manager's associated processors.
        /// Primarly used for calculating a progress percentage for save/load operaations.
        /// </summary>
        /// <returns></returns>
        public ushort GetTotalElements()
        {
            try
            {
                CMonitor.Enter(_crit);

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
            finally
            {
                CMonitor.Exit(_crit);
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
