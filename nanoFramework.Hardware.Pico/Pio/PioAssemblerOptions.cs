//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>Construction-time options for <see cref="PioAssembler"/>.</summary>
    public sealed class PioAssemblerOptions
    {
        /// <summary>
        /// Gets or sets the target PIO version. RP2350 unlocks v1-only instructions.
        /// </summary>
        public PioVersion Version { get; set; } = PioVersion.Rp2040;

        /// <summary>
        /// Gets or sets the side-set value-bit count (0..5). Reduces the per-instruction delay range.
        /// </summary>
        public int SideSetCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value indicating whether side-set is optional and an enable bit is reserved.
        /// </summary>
        public bool SideSetOption { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether side-set drives PINDIRS instead of PINS.
        /// </summary>
        public bool SideSetPinDirs { get; set; } = false;
    }
}
