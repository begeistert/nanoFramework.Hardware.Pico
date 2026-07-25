//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using nanoFramework.Runtime.Events;

namespace nanoFramework.Hardware.Pico.Pio
{
    internal class PioEvent : BaseEvent
    {
        /// <summary>
        /// The index of the PIO block (0, 1, or 2).
        /// </summary>
        public int BlockIndex;

        /// <summary>
        /// The state machine interrupt flags raised by the PIO block.
        /// </summary>
        public PioInterruptFlags Flags;
    }
}