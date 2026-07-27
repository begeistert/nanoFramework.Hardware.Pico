//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System;

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// A fully assembled PIO program: the encoded instructions plus the metadata the
    /// SDK carries in <c>pio_program_t</c> and applies through <c>pio_sm_config</c>
    /// (wrap, side-set, shift defaults, origin). Produced by <see cref="PioAssembler.Build"/>.
    /// </summary>
    public sealed class PioProgram
    {
        /// <summary>
        /// Encoded 16-bit instruction words (length &lt;= 32).
        /// </summary>
        public ushort[] Instructions { get; private set; }

        /// <summary>
        /// Number of instructions.
        /// </summary>
        public int Length
        {
            get
            {
                return Instructions.Length;
            }
        }

        /// <summary>
        /// PC value that wraps back to <see cref="WrapTarget"/> (last instruction by default).
        /// </summary>
        public int Wrap { get; private set; }

        /// <summary>
        /// PC value wrapped to (0 by default).
        /// </summary>
        public int WrapTarget { get; private set; }

        /// <summary>
        /// Fixed load offset 0..31, or -1 when relocatable.
        /// </summary>
        public sbyte Origin { get; private set; }

        /// <summary>
        /// Side-set value-bit count (excludes the optional enable bit).
        /// </summary>
        public int SideSetCount { get; private set; }

        /// <summary>
        /// Whether side-set is optional (an extra enable bit is reserved).
        /// </summary>
        public bool SideSetOptional { get; private set; }

        /// <summary>
        /// Whether side-set targets PINDIRS instead of PINS.
        /// </summary>
        public bool SideSetPinDirs { get; private set; }

        /// <summary>
        /// Out/pull shift direction default.
        /// </summary>
        public ShiftDirection OutShiftDir { get; private set; }

        /// <summary>
        /// Whether autopull is enabled by default.
        /// </summary>
        public bool AutoPull { get; private set; }

        /// <summary>
        /// Autopull threshold in bits (1..32).
        /// </summary>
        public int PullThreshold { get; private set; }

        /// <summary>
        /// IN/push shift direction default.
        /// </summary>
        public ShiftDirection InShiftDir { get; private set; }

        /// <summary>
        /// Whether autopush is enabled by default.
        /// </summary>
        public bool AutoPush { get; private set; }

        /// <summary>
        /// Autopush threshold in bits (1..32).
        /// </summary>
        public int PushThreshold { get; private set; }

        /// <summary>
        /// Minimum PIO version required to run this program.
        /// </summary>
        public PioVersion Version { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PioProgram"/> class.
        /// </summary>
        /// <param name="instructions">The encoded 16-bit PIO instructions (1..32).</param>
        /// <param name="wrap">The wrap instruction index.</param>
        /// <param name="wrapTarget">The wrap-target instruction index.</param>
        /// <param name="origin">The fixed load offset 0..31, or -1 when relocatable.</param>
        /// <param name="sideSetCount">The side-set value-bit count (excludes the optional enable bit).</param>
        /// <param name="sideSetOpt">Whether side-set is optional (an extra enable bit is reserved).</param>
        /// <param name="sideSetPinDirs">Whether side-set targets PINDIRS instead of PINS.</param>
        /// <param name="outShiftDir">The out/pull shift direction default.</param>
        /// <param name="autoPull">Whether autopull is enabled by default.</param>
        /// <param name="pullThreshold">The autopull threshold in bits (1..32).</param>
        /// <param name="inShiftDir">The IN/push shift direction default.</param>
        /// <param name="autoPush">Whether autopush is enabled by default.</param>
        /// <param name="pushThreshold">The autopush threshold in bits (1..32).</param>
        /// <param name="version">The minimum PIO version required to run this program.</param>
        internal PioProgram(
            ushort[] instructions,
            int wrap,
            int wrapTarget,
            sbyte origin,
            int sideSetCount,
            bool sideSetOpt,
            bool sideSetPinDirs,
            ShiftDirection outShiftDir,
            bool autoPull,
            int pullThreshold,
            ShiftDirection inShiftDir,
            bool autoPush,
            int pushThreshold,
            PioVersion version)
        {
            Instructions = instructions;
            Wrap = wrap;
            WrapTarget = wrapTarget;
            Origin = origin;
            SideSetCount = sideSetCount;
            SideSetOptional = sideSetOpt;
            SideSetPinDirs = sideSetPinDirs;
            OutShiftDir = outShiftDir;
            AutoPull = autoPull;
            PullThreshold = pullThreshold;
            InShiftDir = inShiftDir;
            AutoPush = autoPush;
            PushThreshold = pushThreshold;
            Version = version;
        }

        /// <summary>
        /// Wraps an externally produced opcode array (for example, the output of the
        /// stand-alone <c>pioasm</c> tool) with default program configuration. Use the
        /// <see cref="FromEncoded(ushort[], int, int, PioProgramOptions)"/> overload to carry
        /// side-set, shift and origin metadata.
        /// </summary>
        /// <param name="instructions">The encoded 16-bit PIO instructions (1..32).</param>
        /// <param name="wrapTarget">The wrap-target instruction index.</param>
        /// <param name="wrap">The wrap instruction index.</param>
        /// <returns>A program wrapping the supplied instructions.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="instructions"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="instructions"/> is empty or longer than 32 instructions.</exception>
        public static PioProgram FromEncoded(ushort[] instructions, int wrapTarget, int wrap)
        {
            return FromEncoded(instructions, wrapTarget, wrap, new PioProgramOptions());
        }

        /// <summary>
        /// Wraps an externally produced opcode array together with explicit program metadata
        /// (side-set, shift and origin), so a precompiled program seeds a state machine the same
        /// way an inline-assembled one does through <see cref="PioStateMachineConfig.FromProgram"/>.
        /// </summary>
        /// <param name="instructions">The encoded 16-bit PIO instructions (1..32).</param>
        /// <param name="wrapTarget">The wrap-target instruction index.</param>
        /// <param name="wrap">The wrap instruction index.</param>
        /// <param name="options">The program metadata to carry.</param>
        /// <returns>A program wrapping the supplied instructions and metadata.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="instructions"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="instructions"/> is empty or longer than 32 instructions.</exception>
        /// <exception cref="ArgumentException"><paramref name="wrapTarget"/>/<paramref name="wrap"/> are outside the program, an <paramref name="options"/> field is out of range, or a fixed <see cref="PioProgramOptions.Origin"/> cannot fit the program.</exception>
        public static PioProgram FromEncoded(ushort[] instructions, int wrapTarget, int wrap, PioProgramOptions options)
        {
            if (instructions == null)
            {
                throw new ArgumentNullException();
            }

            if (options == null)
            {
                throw new ArgumentNullException();
            }

            int length = instructions.Length;
            if (length == 0 || length > 32)
            {
                throw new ArgumentOutOfRangeException();
            }

            // wrap/wrap-target must be PCs inside the program, wrapping back not forward (matches Build)
            if (wrapTarget < 0 || wrapTarget >= length || wrap < 0 || wrap >= length || wrapTarget > wrap)
            {
                throw new ArgumentException();
            }

            // metadata must satisfy the same ranges PioAssembler.Build enforces
            if (options.Origin < -1 || options.Origin > 31 ||
                (options.Origin >= 0 && options.Origin + length > 32) ||
                options.SideSetCount < 0 || options.SideSetCount > 5 ||
                options.SideSetCount + (options.SideSetOptional ? 1 : 0) > 5 ||
                options.PullThreshold < 1 || options.PullThreshold > 32 ||
                options.PushThreshold < 1 || options.PushThreshold > 32)
            {
                throw new ArgumentException();
            }

            return new PioProgram(
                instructions,
                wrap,
                wrapTarget,
                options.Origin,
                options.SideSetCount,
                options.SideSetOptional,
                options.SideSetPinDirs,
                options.OutShiftDir,
                options.AutoPull,
                options.PullThreshold,
                options.InShiftDir,
                options.AutoPush,
                options.PushThreshold,
                options.Version);
        }
    }
}
