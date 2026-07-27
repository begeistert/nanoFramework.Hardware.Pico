//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//
using System;
using System.Runtime.CompilerServices;

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// Entry point to the RP2040/RP2350 PIO blocks. Use <see cref="Get"/> to obtain a
    /// <see cref="PioBlock"/>, load an assembled <see cref="PioProgram"/>, and claim a
    /// state machine.
    /// </summary>
    public static class Pio
    {
        // RP2040 and RP2350A expose 2 PIO blocks; RP2350 adds a third (PIO2).
        private static readonly PioBlock[] _blocks = new PioBlock[3];
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the first PIO block index this device makes available to application code.
        /// </summary>
        /// <remarks>
        /// On a board with wireless the firmware reserves PIO0 for the radio, so the first usable
        /// block is not the same everywhere. Starting from this property instead of a literal index
        /// keeps the same code working on both.
        /// </remarks>
        public static extern int MinIndex
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /// <summary>
        /// Gets the number of PIO blocks on this device, whether or not application code may use
        /// them. Valid indices for <see cref="Get"/> run from <see cref="MinIndex"/> up to, but not
        /// including, this value.
        /// </summary>
        public static extern int BlockCount
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /// <summary>
        /// Gets the highest GPIO a PIO block on this device can drive: 29 on the RP2040, 47 on the
        /// RP2350.
        /// </summary>
        public static extern int MaxPin
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /// <summary>
        /// Gets the PIO block at the specified index.
        /// </summary>
        /// <param name="index">The PIO block index, from <see cref="MinIndex"/> up to but not including <see cref="BlockCount"/>.</param>
        /// <returns>The <see cref="PioBlock"/> instance for the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="index"/> is not a block this device makes available, which includes a block the firmware reserves for itself, as PIO0 is on a wireless board.</exception>
        public static PioBlock Get(int index)
        {
            if (index < MinIndex || index >= BlockCount)
            {
                throw new ArgumentOutOfRangeException();
            }

            lock (_lock)
            {
                if (_blocks[index] == null)
                {
                    _blocks[index] = new PioBlock(index);
                }

                return _blocks[index];
            }
        }
    }
}
