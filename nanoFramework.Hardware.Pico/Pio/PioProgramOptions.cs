//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// Program-level configuration for <see cref="PioProgram.FromEncoded(ushort[], int, int, PioProgramOptions)"/>, 
    /// mirroring what <see cref="PioAssembler.Build"/> captures.
    /// </summary>
    public sealed class PioProgramOptions
    {
        /// <summary>
        /// Gets or sets the target PIO version. RP2350 unlocks v1-only instructions.
        /// </summary>
        public PioVersion Version { get; set; } = PioVersion.Rp2040;

        /// <summary>
        /// Gets or sets the load origin (absolute instruction-memory offset), or -1 to let the loader place the program.
        /// </summary>
        public sbyte Origin { get; set; } = -1;

        /// <summary>
        /// Gets or sets the side-set value-bit count (0..5). Reduces the per-instruction delay range.
        /// </summary>
        public int SideSetCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value indicating whether side-set is optional and an enable bit is reserved.
        /// </summary>
        public bool SideSetOptional { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether side-set drives PINDIRS instead of PINS.
        /// </summary>
        public bool SideSetPinDirs { get; set; } = false;

        /// <summary>
        /// Gets or sets the OUT/PULL shift direction.
        /// </summary>
        public ShiftDirection OutShiftDir { get; set; } = ShiftDirection.Right;

        /// <summary>
        /// Gets or sets a value indicating whether the output shift register auto-pulls when empty.
        /// </summary>
        public bool AutoPull { get; set; } = false;

        /// <summary>
        /// Gets or sets the auto-pull threshold in bits (1..32).
        /// </summary>
        public int PullThreshold { get; set; } = 32;

        /// <summary>
        /// Gets or sets the IN/PUSH shift direction.
        /// </summary>
        public ShiftDirection InShiftDir { get; set; } = ShiftDirection.Right;

        /// <summary>
        /// Gets or sets a value indicating whether the input shift register auto-pushes when full.
        /// </summary>
        public bool AutoPush { get; set; } = false;

        /// <summary>
        /// Gets or sets the auto-push threshold in bits (1..32).
        /// </summary>
        public int PushThreshold { get; set; } = 32;
    }
}
