//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// Shift direction for the ISR/OSR (and autopush/autopull defaults).
    /// </summary>
    public enum ShiftDirection
    {
        /// <summary>
        /// Shift towards the most-significant bit (left).
        /// </summary>
        Left = 0,
        /// <summary>
        /// Shift towards the least-significant bit (right).
        /// </summary>
        Right = 1,
    }
}
