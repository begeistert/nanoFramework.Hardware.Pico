//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// Identifies one of the four state machines a PIO block owns. Every RP2040 and RP2350
    /// variant has exactly four per block, so unlike the block index this set never changes
    /// with the device.
    /// </summary>
    public enum PioStateMachineIndex
    {
        /// <summary>
        /// State machine 0.
        /// </summary>
        Sm0 = 0,
        /// <summary>
        /// State machine 1.
        /// </summary>
        Sm1 = 1,
        /// <summary>
        /// State machine 2.
        /// </summary>
        Sm2 = 2,
        /// <summary>
        /// State machine 3.
        /// </summary>
        Sm3 = 3,
        /// <summary>
        /// Whichever state machine is free. Valid only when asking for one; never returned as an identity.
        /// </summary>
        Any = 4,
    }
}
