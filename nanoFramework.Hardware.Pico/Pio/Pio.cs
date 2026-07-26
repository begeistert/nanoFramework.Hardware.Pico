//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

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
        /// Gets the PIO block at the specified index.
        /// </summary>
        /// <param name="index">The PIO block index (0 or 1; 2 is RP2350-only).</param>
        /// <returns>The <see cref="PioBlock"/> instance for the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="index"/> is less than 0 or exceeds the number of available PIO blocks.</exception>
        public static PioBlock Get(int index)
        {
            if (index < 0 || index >= _blocks.Length)
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
