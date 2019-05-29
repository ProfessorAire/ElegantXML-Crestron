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
    public class ProcessorBase
    {
        /// <summary>
        /// Sets or retrieves the delimiter used to separate the path from the default value in the Simpl+ parameter.
        /// </summary>
        public char DefaultValueDelimiter { get; set; }

        public void SetDefaultValueDelimiter(string DelimiterCharacter)
        {
            DefaultValueDelimiter = DelimiterCharacter[0];
        }

        private bool isInitialized = false;
        /// <summary>
        /// Returns true when the module has initialized correctly.
        /// </summary>
        public bool IsInitialized { get { return isInitialized; } set { isInitialized = value; } }

        public ushort IsProcessorInitialized { get { return IsInitialized ? (ushort)1 : (ushort)0; } }

        /// <summary>
        /// The Manager this processor is associated with.
        /// </summary>
        internal Manager manager = null;

        /// <summary>
        /// Returns 1 if the manager associated with this processor is ready. This means this class can start initializing.
        /// </summary>
        /// <param name="id">The ID of the manager to check.</param>
        /// <returns>1 if true, 0 if false.</returns>
        public ushort IsManagerReady(ushort id)
        {
            var man = Manager.GetManagerByID(id);
            if (man != null)
            {
                manager = man;
                return 1;
            }
            return 0;
        }

        internal ProcessorBase()
        {
            DefaultValueDelimiter = '|';
        }

    }
}