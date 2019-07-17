using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace ElegantXml.Xml
{
    /// <summary>
    /// Base class for all element processors.
    /// </summary>
    public abstract class ProcessorBase
    {
        /// <summary>
        /// Gets/Sets the Default Value Delimiter for this processor.
        /// </summary>
        public char DefaultValueDelimiter { get; set; }

        private bool isInitialized = false;
        /// <summary>
        /// Returns true when the module has initialized correctly.
        /// </summary>
        public bool IsInitialized { get { return isInitialized; } set { isInitialized = value; } }

        public ushort IsProcessorInitialized { get { return IsInitialized ? (ushort)1 : (ushort)0; } }

        public delegate void ReportIsInitializedDelegate(ushort state);
        /// <summary>
        /// Reports that the class is initialized back to the Simpl+ module.
        /// </summary>
        public ReportIsInitializedDelegate ReportIsInitialized { get; set; }

        public ushort ManagerId { get; set; }

        /// <summary>
        /// Returns 1 if the manager associated with this processor is ready. This means this class can start initializing.
        /// </summary>
        /// <param name="id">The ID of the manager to check.</param>
        /// <returns>1 if true, 0 if false.</returns>
        public ushort IsManagerReady(ushort id)
        {
            if (Manager.GetManagerIsReady(id) == false) { return 0; }
            DefaultValueDelimiter = Manager.GetManagerDefaultValueDelimiter(id);
            return 1;
        }


    }
}