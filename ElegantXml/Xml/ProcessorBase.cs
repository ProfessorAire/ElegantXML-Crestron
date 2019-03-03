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

    }
}