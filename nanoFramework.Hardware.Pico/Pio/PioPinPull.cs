//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// The pad pull to leave on a GPIO when it is routed to a PIO block.
    /// </summary>
    /// <remarks>
    /// Routing a pin to the PIO rewrites its pad configuration, so the pull has to be stated at that
    /// point rather than set beforehand. An open-drain bus needs <see cref="Up"/>: every device on it
    /// only ever drives low, and the pull is what returns the line to idle. The internal pull is weak,
    /// in the tens of kilohms, so a bus with real capacitance or a demanding rise time still wants an
    /// external resistor.
    /// </remarks>
    public enum PioPinPull
    {
        /// <summary>
        /// No pull. The line is expected to be driven at all times.
        /// </summary>
        None = 0,
        /// <summary>
        /// Pull-up, which is what an open-drain bus needs to return to idle.
        /// </summary>
        Up = 1,
        /// <summary>
        /// Pull-down.
        /// </summary>
        Down = 2,
    }
}
