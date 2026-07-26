//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// PIO hardware version. Determines which instructions/features are legal.
    /// </summary>
    public enum PioVersion
    {
        /// <summary>
        /// RP2040 (PIO v0).
        /// </summary>
        Rp2040 = 0,
        /// <summary>
        /// RP2350 (PIO v1): adds extended MOV, FIFO PUTGET, GPIO base window.
        /// </summary>
        Rp2350 = 1,
    }
}
