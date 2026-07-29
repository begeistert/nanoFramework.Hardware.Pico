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
        /// Gets the system clock, in Hz, that the state machine dividers count down from.
        /// </summary>
        /// <remarks>
        /// A state machine's clock is this value divided by <see cref="PioStateMachine.ClockDivisor"/>,
        /// so anything with a real timing requirement -- a baud rate, a pulse width, a protocol period --
        /// has to be computed against it. It is not the same on every part, nor fixed for a given part,
        /// which is why it is read from the firmware instead of assumed:
        /// <see cref="PioStateMachineConfig.ClockFromFrequency(float, float)"/> defaults to the RP2040
        /// figure and will be 20 % out on an RP2350 unless this value is passed in.
        /// </remarks>
        public static extern int SystemClock
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
