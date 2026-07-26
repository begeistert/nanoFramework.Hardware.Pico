//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System;

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// Specifies the state machine interrupt flags raised by a PIO block.
    /// </summary>
    [Flags]
    public enum PioInterruptFlags : uint
    {
        /// <summary>
        /// No interrupt flags are set.
        /// </summary>
        None = 0,
        /// <summary>
        /// Interrupt flag for State Machine 0.
        /// </summary>
        Sm0 = 1U << 0,
        /// <summary>
        /// Interrupt flag for State Machine 1.
        /// </summary>
        Sm1 = 1U << 1,
        /// <summary>
        /// Interrupt flag for State Machine 2.
        /// </summary>
        Sm2 = 1U << 2,
        /// <summary>
        /// Interrupt flag for State Machine 3.
        /// </summary>
        Sm3 = 1U << 3
    }
}