//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// Jmp condition (3-bit field).
    /// </summary>
    public enum PioCondition
    {
        /// <summary>
        /// Always jump (unconditional).
        /// </summary>
        Always = 0,
        /// <summary>
        /// !X — X is zero.
        /// </summary>
        XZero = 1,
        /// <summary>
        /// X-- — X is non-zero prior to decrement, then decrement.
        /// </summary>
        XPostDec = 2,
        /// <summary>
        /// !Y — Y is zero.
        /// </summary>
        YZero = 3,
        /// <summary>
        /// Y-- — Y is non-zero prior to decrement, then decrement.
        /// </summary>
        YPostDec = 4,
        /// <summary>
        /// X != Y.
        /// </summary>
        XNotEqualY = 5,
        /// <summary>
        /// PIN — the JMP pin is high.
        /// </summary>
        Pin = 6,
        /// <summary>
        /// !OSRE — output shift register is not empty.
        /// </summary>
        OsrNotEmpty = 7,
    }
}
