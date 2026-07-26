//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// Wait polarity source.
    /// </summary>
    public enum PioWaitSource
    {
        /// <summary>
        /// Absolute GPIO index.
        /// </summary>
        Gpio = 0,
        /// <summary>
        /// Pin relative to the state machine's IN base.
        /// </summary>
        Pin = 1,
        /// <summary>
        /// IRQ flag.
        /// </summary>
        Irq = 2,
    }
}
