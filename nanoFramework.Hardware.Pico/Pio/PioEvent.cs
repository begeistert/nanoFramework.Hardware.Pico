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
        /// El índice del bloque PIO (0, 1 o 2).
        /// </summary>
        public int BlockIndex;

        /// <summary>
        /// Las banderas de interrupción del PIO.
        /// </summary>
        public uint Flags;
    }
}