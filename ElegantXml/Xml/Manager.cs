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
    public class Manager : IDisposable
    {
        /// <summary>
        /// A list of the Manager classes that have been registered in the program.
        /// </summary>
        private static Dictionary<ushort, Manager> Managers { get; set; }

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
        /// A list of the SerialPropertyInterlocks associated with this manager.
        /// </summary>
        private List<SerialPropertyInterlock> SerialInterlocks { get; set; }
        /// <summary>
        /// A list of the AnalogPropertyInterlocks associated with this manager.
        /// </summary>
        private List<AnalogPropertyInterlock> AnalogInterlocks { get; set; }
        /// <summary>
        /// A list of the SignedAnalogPropertyInterlocks associated with this manager.
        /// </summary>
        private List<SignedAnalogPropertyInterlock> SignedAnalogInterlocks { get; set; }

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

        private char defaultValueDelimiter = '|';
        /// <summary>
        /// Sets or retrieves the delimiter used to separate the path from the default value in the Simpl+ parameter.
        /// </summary>
        public char DefaultValueDelimiter { get { return defaultValueDelimiter; } set { defaultValueDelimiter = value; } }

        public void SetDefaultValueDelimiter(string DelimiterCharacter)
        {
            DefaultValueDelimiter = DelimiterCharacter[0];
            Debug.PrintLine("Setting default value delimiter to: " + DefaultValueDelimiter);
        }

        private double previousProgress = 0;
        /// <summary>
        /// Passes progress to the ReportProgress delegate, forcing the Crestron environment to allow other apps to process.
        /// </summary>
        /// <param name="progress"></param>
        private void YieldProgress(ushort progress)
        {
            var newProgress = Math.Floor((double)(progress * 100) / 65535);
            if (newProgress > previousProgress)
            {
                CrestronConsole.Print(".");
                ReportProgress(progress);
                CrestronEnvironment.Sleep(0);
            }
            previousProgress = newProgress;
            if (previousProgress >= 100)
            {
                previousProgress = 0;
                ReportProgress(0);
            }
        }

        private CCriticalSection _fileLock = new CCriticalSection();
        private CCriticalSection _analogLock = new CCriticalSection();
        private CCriticalSection _digitalLock = new CCriticalSection();
        private CCriticalSection _serialLock = new CCriticalSection();
        private CCriticalSection _signedAnalogLock = new CCriticalSection();
        private CCriticalSection _serialInterlockLock = new CCriticalSection();
        private CCriticalSection _analogInterlockLock = new CCriticalSection();
        private CCriticalSection _signedAnalogInterlockLock = new CCriticalSection();

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
        public bool AddAnalog(AnalogProcessor proc)
        {
            if (proc == null)
            {
                Debug.PrintLine("Null processor, couldn't add to manager.");
                return false;
            }
            try
            {
                if (AnalogProcessors == null)
                {
                    if (_analogLock.TryEnter())
                    {
                        try
                        {
                            if (AnalogProcessors == null)
                            {
                                AnalogProcessors = new List<AnalogProcessor>();
                            }
                        }
                        finally { _analogLock.Leave(); }
                    }
                }
                AnalogProcessors.Add(proc);
                return true;
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while adding Analog Processor to Manager.");
                Debug.PrintLine(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Adds a SignedAnalog processor to this manager instance.
        /// </summary>
        /// <param name="proc">The processor to add.</param>
        public bool AddSignedAnalog(SignedAnalogProcessor proc)
        {
            if (proc == null)
            {
                Debug.PrintLine("Null processor, couldn't add to manager.");
                return false;
            }
            try
            {
                if (SignedAnalogProcessors == null)
                {
                    if (_signedAnalogLock.TryEnter())
                    {
                        try
                        {
                            if (SignedAnalogProcessors == null)
                            {
                                SignedAnalogProcessors = new List<SignedAnalogProcessor>();
                            }
                        }
                        finally { _signedAnalogLock.Leave(); }
                    }
                }
                SignedAnalogProcessors.Add(proc);
                return true;
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while adding Signed Analog Processor to Manager.");
                Debug.PrintLine(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Adds a Digital processor to this manager instance.
        /// </summary>
        /// <param name="proc">The processor to add.</param>
        public bool AddDigital(DigitalProcessor proc)
        {
            if (proc == null)
            {
                Debug.PrintLine("Null processor, couldn't add to manager.");
                return false;
            }
            try
            {
                if (DigitalProcessors == null)
                {
                    if (_digitalLock.TryEnter())
                    {
                        try
                        {
                            if (DigitalProcessors == null)
                            {
                                DigitalProcessors = new List<DigitalProcessor>();
                            }
                        }
                        finally { _digitalLock.Leave(); }
                    }
                }
                DigitalProcessors.Add(proc);
                return true;
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while adding Digital Processor to Manager.");
                Debug.PrintLine(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Adds a Serial processor to this manager instance.
        /// </summary>
        /// <param name="proc">The processor to add.</param>
        public bool AddSerial(SerialProcessor proc)
        {
            if (proc == null)
            {
                Debug.PrintLine("Null processor, couldn't add to manager.");
                return false;
            }
            try
            {
                if (SerialProcessors == null)
                {
                    if (_serialLock.TryEnter())
                    {
                        try
                        {
                            if (SerialProcessors == null)
                            {
                                SerialProcessors = new List<SerialProcessor>();
                            }
                        }
                        finally { _serialLock.Leave(); }
                    }
                }
                SerialProcessors.Add(proc);
                return true;
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while adding Serial Processor to Manager.");
                Debug.PrintLine(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Adds a Serial Property Interlock to this manager instance.
        /// </summary>
        /// <param name="proc">The processor to add.</param>
        internal bool AddSerialPropertyInterlock(SerialPropertyInterlock proc)
        {
            if (proc == null)
            {
                Debug.PrintLine("Null processor, couldn't add to manager.");
                return false;
            }
            try
            {
                if (SerialInterlocks == null)
                {
                    if (_serialInterlockLock.TryEnter())
                    {
                        try
                        {
                            if (SerialInterlocks == null)
                            {
                                SerialInterlocks = new List<SerialPropertyInterlock>();
                            }
                        }
                        finally { _serialInterlockLock.Leave(); }
                    }
                }
                SerialInterlocks.Add(proc);
                return true;
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while adding Serial Property Interlock to Manager.");
                Debug.PrintLine(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Adds an Analog Property Interlock to this manager instance.
        /// </summary>
        /// <param name="proc">The processor to add.</param>
        internal bool AddAnalogPropertyInterlock(AnalogPropertyInterlock proc)
        {
            if (proc == null)
            {
                Debug.PrintLine("Null processor, couldn't add to manager.");
                return false;
            }
            try
            {
                if (AnalogInterlocks == null)
                {
                    if (_analogInterlockLock.TryEnter())
                    {
                        try
                        {
                            if (AnalogInterlocks == null)
                            {
                                AnalogInterlocks = new List<AnalogPropertyInterlock>();
                            }
                        }
                        finally { _analogInterlockLock.Leave(); }
                    }
                }
                AnalogInterlocks.Add(proc);
                return true;
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while adding Analog Property Interlock to Manager.");
                Debug.PrintLine(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Adds a Signed Analog Property Interlock to this manager instance.
        /// </summary>
        /// <param name="proc">The processor to add.</param>
        internal bool AddSignedAnalogPropertyInterlock(SignedAnalogPropertyInterlock proc)
        {
            if (proc == null)
            {
                Debug.PrintLine("Null processor, couldn't add to manager.");
                return false;
            }
            try
            {
                if (SignedAnalogInterlocks == null)
                {
                    if (_signedAnalogInterlockLock.TryEnter())
                    {
                        try
                        {
                            if (SignedAnalogInterlocks == null)
                            {
                                SignedAnalogInterlocks = new List<SignedAnalogPropertyInterlock>();
                            }
                        }
                        finally { _signedAnalogInterlockLock.Leave(); }
                    }
                }
                SignedAnalogInterlocks.Add(proc);
                return true;
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while adding Analog Property Interlock to Manager.");
                Debug.PrintLine(ex.Message);
                return false;
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
                _analogLock.Enter();
                _signedAnalogLock.Enter();
                _digitalLock.Enter();
                _serialLock.Enter();
                _serialInterlockLock.Enter();
                _analogInterlockLock.Enter();
                _signedAnalogInterlockLock.Enter();
                _fileLock.Enter();
                XDocument doc = null;
                try
                {
                    if (!Crestron.SimplSharp.CrestronIO.File.Exists(FilePath))
                    {
                        Debug.PrintLine("Using empty XML Document since none was found.");
                        doc = new XDocument();
                        doc.Add(new XElement(RootElement));
                    }
                    else
                    {

                        var content = Crestron.SimplSharp.CrestronIO.File.ReadToEnd(FilePath, Encoding.UTF8);
                        var start = content.IndexOf("<?xml", 0);
                        if (start < 0)
                        {
                            Debug.PrintLine("Couldn't find xml element to parse in the file.");
                            CrestronConsole.PrintLine(DateTime.Now.ToShortDateString() + "|" + DateTime.Now.ToLongTimeString() + "| Failed to load xml file.");
                            LoadFailure("Invalid XML. File couldn't be loaded.");
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
                            Debug.PrintLine("XML Document loaded into memory, ready to process.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.PrintLine("Exception occurred when loading XML document into XDocument variable.");
                    Debug.PrintLine(ex.Message);
                    CrestronConsole.PrintLine(DateTime.Now.ToShortDateString() + "|" + DateTime.Now.ToLongTimeString() + "| Failed to load xml file.");
                    LoadFailure("The document provided couldn't be parsed correctly. Check for invalid XML.");
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

                var element = doc.Elements().First();
                var isValidElement = element.HasAttributes || element.HasElements;
                
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
                                if (isValidElement && TryFindValue(DigitalProcessors[i].Elements[j].AttributePath, element, out value))
                                {
                                    DigitalProcessors[i].UpdateValue(DigitalProcessors[i].Elements[j].ID, value);
                                }
                                else
                                {
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
                                if (isValidElement && TryFindValue(AnalogProcessors[i].Elements[j].AttributePath, element, out value))
                                {
                                    AnalogProcessors[i].UpdateValue(AnalogProcessors[i].Elements[j].ID, value);
                                }
                                else
                                {
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
                                if (isValidElement && TryFindValue(SignedAnalogProcessors[i].Elements[j].AttributePath, element, out value))
                                {
                                    SignedAnalogProcessors[i].UpdateValue(SignedAnalogProcessors[i].Elements[j].ID, value);
                                }
                                else
                                {
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
                    for (var i = 0; i < SerialProcessors.Count; i++)
                    {
                        for (ushort j = 0; j < SerialProcessors[i].Elements.Count; j++)
                        {
                            try
                            {
                                if (isValidElement && TryFindValue(SerialProcessors[i].Elements[j].AttributePath, element, out value))
                                {
                                    SerialProcessors[i].UpdateValue(SerialProcessors[i].Elements[j].ID, value);
                                }
                                else
                                {
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
                if (SerialInterlocks != null)
                {
                    Debug.PrintLine("Processing Serial Interlocks");
                    for (var i = 0; i < SerialInterlocks.Count; i++)
                    {
                        if (isValidElement && TryFindValue(SerialInterlocks[i].Element.AttributePath, element, out value))
                        {
                            SerialInterlocks[i].SelectValue(value);
                        }
                        else
                        {
                            SerialInterlocks[i].SelectValue(SerialInterlocks[i].Element.DefaultValue);
                        }
                        current += step;
                        YieldProgress(current);
                    }
                }
                if (AnalogInterlocks != null)
                {
                    Debug.PrintLine("Processing Analog Interlocks");
                    for (var i = 0; i < AnalogInterlocks.Count; i++)
                    {
                        if (isValidElement && TryFindValue(AnalogInterlocks[i].Element.AttributePath, element, out value))
                        {
                            AnalogInterlocks[i].SelectValueByString(value);
                        }
                        else
                        {
                            AnalogInterlocks[i].SelectValue(AnalogInterlocks[i].Element.DefaultValue);
                        }
                        current += step;
                        YieldProgress(current);
                    }
                }
                if (SignedAnalogInterlocks != null)
                {
                    Debug.PrintLine("Processing SignedAnalog Interlocks");
                    for (var i = 0; i < SignedAnalogInterlocks.Count; i++)
                    {
                        if (isValidElement && TryFindValue(SignedAnalogInterlocks[i].Element.AttributePath, element, out value))
                        {
                            SignedAnalogInterlocks[i].SelectValueByString(value);
                        }
                        else
                        {
                            SignedAnalogInterlocks[i].SelectValue(SignedAnalogInterlocks[i].Element.DefaultValue);
                        }
                        current += step;
                        YieldProgress(current);
                    }
                }
                XmlDoc = doc;
            }
            finally
            {
                _analogLock.Leave();
                _signedAnalogLock.Leave();
                _digitalLock.Leave();
                _serialLock.Leave();
                _serialInterlockLock.Leave();
                _analogInterlockLock.Leave();
                _signedAnalogInterlockLock.Leave();
                _fileLock.Leave();
            }
            CrestronConsole.PrintLine("\n" + DateTime.Now.ToShortDateString() + "|" + DateTime.Now.ToLongTimeString() + "|Finished loading xml file:" + FileName);
            LoadSuccess();
            IsLoading(0);
            IsSaveRequired(0);
            YieldProgress(65535);
        }

        /// <summary>
        /// Used publicly to find the value of a path element provided by a companion Simpl+ module.
        /// Includes a default value to pass to the element's output if no default is found.
        /// </summary>
        private bool TryFindValue(string path, XElement element, out string outValue)
        {
            outValue = "";

            if (element == null)
            {
                Debug.PrintLine("Null value found while finding value for path: " + path);
                return false;
            }

            var parts = path.Split(PathDelimiter);
            
            var isElement = false;
            if (parts.Last().Contains("=") ||
                parts.Last() == "" ||
                parts.Last() == string.Empty)
            {
                isElement = true;
            }
            
            try
            {
                for (var i = 0; i < parts.Length - 1; i++)
                {
                    if (parts[i].Contains(" "))
                    {
                        var pieces = parts[i].Split(' ');
                        var att = pieces[1].Split('=');
                        att[1] = att[1].Replace("\"", "");
                        element =
                            (from elem in element.Elements(pieces[0])
                             where elem.Attribute(att[0]).Value == att[1]
                             select elem).FirstOrDefault();
                    }
                    else
                    {
                        element = element.Element(parts[i]);
                    }
                    if (element == null)
                    {
                        Debug.PrintLine("Unable to find element for path: " + path);
                        return false;
                    }
                }

                if (isElement)
                {
                    outValue = element.Value;
                }
                else if (element.HasAttributes && element.Attributes(parts[parts.Length - 1]).Count() > 0)
                {
                    outValue =
                        (from att in element.Attributes(parts[parts.Length - 1]) select att).FirstOrDefault().Value;
                }
                else if (element.HasElements && element.Elements(parts[parts.Length - 1]).Count() > 0)
                {
                    outValue =
                        (from elem in element.Elements(parts[parts.Length - 1]) select elem).FirstOrDefault().Value;
                }
                else
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Error finding element or attribute value when loading xml file: " + FileName);
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
                _analogLock.Enter();
                _signedAnalogLock.Enter();
                _digitalLock.Enter();
                _serialLock.Enter();
                _serialInterlockLock.Enter();
                _fileLock.Enter();
                XmlBuilder builder = null;
                if (XmlDoc != null)
                {
                    builder = new XmlBuilder(RootElement, XmlDoc, PathDelimiter);
                }
                else
                {
                    builder = new XmlBuilder(RootElement, PathDelimiter);
                }

                YieldProgress(0);
                ushort step = (ushort)(65535 / GetTotalElements());
                builder.ResetProgress(step);
                builder.ProgressUpdated += (o, a) =>
                    {
                        YieldProgress(a.Progress);
                    };

                var markerTime = CrestronEnvironment.GetLocalTime().AddSeconds(20);
                if (DigitalProcessors != null && !builder.WriteDigitals(DigitalProcessors))
                {
                    SaveFailure("Unable to write all Digital elements.");
                    Debug.PrintLine("Unable to write all Digital elements.");
                    IsSaving(0);
                    return;
                }
                if (CrestronEnvironment.GetLocalTime() > markerTime)
                {
                    CrestronEnvironment.Sleep(0);
                    markerTime = CrestronEnvironment.GetLocalTime().AddSeconds(20);
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
                if (CrestronEnvironment.GetLocalTime() > markerTime)
                {
                    CrestronEnvironment.Sleep(0);
                    markerTime = CrestronEnvironment.GetLocalTime().AddSeconds(20);
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
                if (CrestronEnvironment.GetLocalTime() > markerTime)
                {
                    CrestronEnvironment.Sleep(0);
                    markerTime = CrestronEnvironment.GetLocalTime().AddSeconds(20);
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
                if (SerialInterlocks != null && !builder.WriteSerialPropertyInterlocks(SerialInterlocks))
                {
                    if (!builder.WriteSerialPropertyInterlocks(SerialInterlocks))
                    {
                        SaveFailure("Unable to write all Serial Property Interlock elements.");
                        Debug.PrintLine("Unable to write all Serial Property Interlock elements.");
                        IsSaving(0);
                        return;
                    }
                }
                if (AnalogInterlocks != null && !builder.WriteAnalogPropertyInterlocks(AnalogInterlocks))
                {
                    if (!builder.WriteAnalogPropertyInterlocks(AnalogInterlocks))
                    {
                        SaveFailure("Unable to write all Analog Property Interlock elements.");
                        Debug.PrintLine("Unable to write all Analog Property Interlock elements.");
                        IsSaving(0);
                        return;
                    }
                }
                if (SignedAnalogInterlocks != null && !builder.WriteSignedAnalogPropertyInterlocks(SignedAnalogInterlocks))
                {
                    if (!builder.WriteSignedAnalogPropertyInterlocks(SignedAnalogInterlocks))
                    {
                        SaveFailure("Unable to write all SignedAnalog Property Interlock elements.");
                        Debug.PrintLine("Unable to write all SignedAnalog Property Interlock elements.");
                        IsSaving(0);
                        return;
                    }
                }
                try
                {
                    if (builder.Save(FilePath))
                    {
                        CrestronConsole.PrintLine("\n" + DateTime.Now.ToShortDateString() + "|" + DateTime.Now.ToLongTimeString() + "|XML file: " + FileName + " saved!");
                        SaveSuccess();
                        IsSaving(0);
                        IsSaveRequired(0);
                        XmlDoc = builder.Document;
                        YieldProgress(65535);
                        return;
                    }
                    else
                    {
                        CrestronConsole.PrintLine("\n" + DateTime.Now.ToShortDateString() + "|" + DateTime.Now.ToLongTimeString() + "|XML file: " + FileName + " not saved!");
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
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while saving XML file to disk.");
                Debug.PrintLine(ex.Message);
                SaveFailure("Exception while saving.");
                IsSaving(0);
                return;
            }
            finally
            {
                _analogLock.Leave();
                _signedAnalogLock.Leave();
                _digitalLock.Leave();
                _serialLock.Leave();
                _fileLock.Leave();
            }
        }

        private static CCriticalSection _crits = new CCriticalSection();

        /// <summary>
        /// Adds a manager to the public static list of managers.
        /// </summary>
        /// <param name="processor"></param>
        public static void AddManager(Manager processor)
        {
            try
            {
                _crits.Enter();
                if (Managers == null)
                {
                    Managers = new Dictionary<ushort, Manager>();
                }
                Managers.Add(processor.ID, processor);
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while adding manager via static AddManager method.");
                Debug.PrintLine(ex.Message);
            }
            finally
            {
                _crits.Leave();
            }
        }

        /// <summary>
        /// Returns a Manager object, if a manager with a matching ID exists, otherwise returns null.
        /// </summary>
        /// <param name="ID">The ID of the manager to return.</param>
        /// <returns></returns>
        public static Manager GetManagerByID(ushort id)
        {
            if (Managers == null || Managers.Keys.Contains(id) == false) { return null; }
            return Managers[id];
        }

        public static char GetManagerDefaultValueDelimiter(ushort id)
        {
            if (Managers == null || Managers.Keys.Contains(id) == false) { Debug.PrintLine("Returned default | value delimiter."); return '|'; }
            if (Managers.Keys.Contains(id) && Managers[id] != null)
            {
                return Managers[id].DefaultValueDelimiter;
            }
            return '|';
        }

        public static void SetManagerUpdateRequired(ushort id, bool updateRequired)
        {
            if (Managers == null || Managers.Keys.Contains(id) == false) { return; }
            if (Managers[id] != null)
            {
                Managers[id].IsSaveRequired(updateRequired ? SimplBool.True : SimplBool.False);
            }
        }

        public static bool GetManagerIsReady(ushort id)
        {
            if (Managers == null || Managers.Keys.Contains(id) == false) { return false; }
            try
            {
                return Managers.Keys.Contains(id);
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered when checking for manager preparedness!");
                Debug.PrintLine(ex.Message);
                return false;
            }
        }

        public static bool AddProcessorToManager(ushort managerId, AnalogProcessor processor)
        {
            try
            {
                if (Managers == null || Managers.Keys.Contains(managerId) == false) { return false; }
                var man = Managers[managerId];
                if (man == null) { return false; }
                return man.AddAnalog(processor);
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while adding Analog processor to manager.");
                Debug.PrintLine(ex.Message);
                return false;
            }
        }

        public static bool AddProcessorToManager(ushort managerId, SignedAnalogProcessor processor)
        {
            try
            {
                if (Managers == null || Managers.Keys.Contains(managerId) == false) { return false; }
                var man = Managers[managerId];
                if (man == null) { return false; }
                return man.AddSignedAnalog(processor);
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while adding Signed Analog processor to manager.");
                Debug.PrintLine(ex.Message);
                return false;
            }
        }

        public static bool AddProcessorToManager(ushort managerId, DigitalProcessor processor)
        {
            try
            {
                if (Managers == null || Managers.Keys.Contains(managerId) == false) { return false; }
                var man = Managers[managerId];
                if (man == null) { return false; }
                return man.AddDigital(processor);
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while adding Digital processor to manager.");
                Debug.PrintLine(ex.Message);
                return false;
            }
        }

        public static bool AddProcessorToManager(ushort managerId, SerialProcessor processor)
        {
            try
            {
                if (Managers == null || Managers.Keys.Contains(managerId) == false) { return false; }
                var man = Managers[managerId];
                if (man == null) { return false; }
                return man.AddSerial(processor);
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while adding Serial processor to manager.");
                Debug.PrintLine(ex.Message);
                return false;
            }
        }

        public static bool AddProcessorToManager(ushort managerId, SerialPropertyInterlock interlock)
        {
            try
            {
                if(Managers == null || Managers.Keys.Contains(managerId) == false) { return false; }
                var man = Managers[managerId];
                if(man == null) { return false; }
                return man.AddSerialPropertyInterlock(interlock);
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while adding Serial Property Interlock to manager.");
                Debug.PrintLine(ex.Message);
                return false;
            }
        }

        public static bool AddProcessorToManager(ushort managerId, AnalogPropertyInterlock interlock)
        {
            try
            {
                if (Managers == null || Managers.Keys.Contains(managerId) == false) { return false; }
                var man = Managers[managerId];
                if (man == null) { return false; }
                return man.AddAnalogPropertyInterlock(interlock);
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while adding Analog Property Interlock to manager.");
                Debug.PrintLine(ex.Message);
                return false;
            }
        }

        public static bool AddProcessorToManager(ushort managerId, SignedAnalogPropertyInterlock interlock)
        {
            try
            {
                if (Managers == null || Managers.Keys.Contains(managerId) == false) { return false; }
                var man = Managers[managerId];
                if (man == null) { return false; }
                return man.AddSignedAnalogPropertyInterlock(interlock);
            }
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while adding Signed Analog Property Interlock to manager.");
                Debug.PrintLine(ex.Message);
                return false;
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
            catch (Exception ex)
            {
                Debug.PrintLine("Exception encountered while counting total elements.");
                Debug.PrintLine(ex.Message);
                return 0;
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


        #region IDisposable Members

        public void Dispose()
        {
            _analogLock.Dispose();
            _signedAnalogLock.Dispose();
            _digitalLock.Dispose();
            _serialLock.Dispose();
            _fileLock.Dispose();
        }

        #endregion
    }
}
