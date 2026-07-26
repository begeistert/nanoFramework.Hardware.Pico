//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System;

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// Fluent configuration for a PIO state machine. Seed it from an assembled program
    /// with <see cref="FromProgram"/> so wrap, side-set and shift defaults are applied
    /// automatically, then set the pin mappings and clock divider.
    /// </summary>
    public sealed class PioStateMachineConfig
    {
        /// <summary>
        /// Number of entries in the flattened configuration blob.
        /// </summary>
        public const int BlobLength = 27;
        /// <summary>
        /// Blob index of the OUT pin base.
        /// </summary>
        public const int IdxOutBase = 0;
        /// <summary>
        /// Blob index of the OUT pin count.
        /// </summary>
        public const int IdxOutCount = 1;
        /// <summary>
        /// Blob index of the SET pin base.
        /// </summary>
        public const int IdxSetBase = 2;
        /// <summary>
        /// Blob index of the SET pin count.
        /// </summary>
        public const int IdxSetCount = 3;
        /// <summary>
        /// Blob index of the side-set pin base.
        /// </summary>
        public const int IdxSideSetBase = 4;
        /// <summary>
        /// Blob index of the side-set value-bit count.
        /// </summary>
        public const int IdxSideSetCount = 5;
        /// <summary>
        /// Blob index of the side-set optional flag.
        /// </summary>
        public const int IdxSideSetOpt = 6;
        /// <summary>
        /// Blob index of the side-set PINDIRS flag.
        /// </summary>
        public const int IdxSideSetPinDirs = 7;
        /// <summary>
        /// Blob index of the IN pin base.
        /// </summary>
        public const int IdxInBase = 8;
        /// <summary>
        /// Blob index of the JMP PIN GPIO.
        /// </summary>
        public const int IdxJmpPin = 9;
        /// <summary>
        /// Blob index of the OUT/pull shift-right flag.
        /// </summary>
        public const int IdxOutShiftRight = 10;
        /// <summary>
        /// Blob index of the autopull flag.
        /// </summary>
        public const int IdxAutoPull = 11;
        /// <summary>
        /// Blob index of the autopull threshold.
        /// </summary>
        public const int IdxPullThreshold = 12;
        /// <summary>
        /// Blob index of the IN/push shift-right flag.
        /// </summary>
        public const int IdxInShiftRight = 13;
        /// <summary>
        /// Blob index of the autopush flag.
        /// </summary>
        public const int IdxAutoPush = 14;
        /// <summary>
        /// Blob index of the autopush threshold.
        /// </summary>
        public const int IdxPushThreshold = 15;
        /// <summary>
        /// Blob index of the wrap-target offset.
        /// </summary>
        public const int IdxWrapTarget = 16;
        /// <summary>
        /// Blob index of the wrap offset.
        /// </summary>
        public const int IdxWrap = 17;
        /// <summary>
        /// Blob index of the clock-divider integer part.
        /// </summary>
        public const int IdxClkDivInt = 18;
        /// <summary>
        /// Blob index of the clock-divider fractional part.
        /// </summary>
        public const int IdxClkDivFrac = 19;
        /// <summary>
        /// Blob index of the FIFO join mode.
        /// </summary>
        public const int IdxFifoJoin = 20;
        /// <summary>
        /// Blob index of the GPIO base.
        /// </summary>
        public const int IdxGpioBase = 21;
        /// <summary>
        /// Blob index of the MOV status source selector.
        /// </summary>
        public const int IdxMovStatusSel = 22;
        /// <summary>
        /// Blob index of the MOV status N threshold.
        /// </summary>
        public const int IdxMovStatusN = 23;
        /// <summary>
        /// Blob index of the OUT sticky flag.
        /// </summary>
        public const int IdxOutSticky = 24;
        /// <summary>
        /// Blob index of the inline-OUT-enable flag.
        /// </summary>
        public const int IdxInlineOutEn = 25;
        /// <summary>
        /// Blob index of the inline OUT enable bit selector.
        /// </summary>
        public const int IdxOutEnSel = 26;

        // the shift threshold spans the full 32-bit shift register; the clock divider integer defaults to 1 (no division)
        private const int MaxShiftThreshold = 32;
        private const int DefaultClockDivInt = 1;

        private int _outBase, _outCount;
        private int _setBase, _setCount;
        private int _sideSetBase;
        private int _inBase;
        private bool _inBaseSet;
        private int _jmpPin;
        private bool _jmpPinSet;

        private int _sideSetCount;
        private bool _sideSetOpt;
        private bool _sideSetPinDirs;

        private ShiftDirection _outShiftDir = ShiftDirection.Right;
        private bool _autoPull;
        private int _pullThreshold = MaxShiftThreshold;
        private ShiftDirection _inShiftDir = ShiftDirection.Right;
        private bool _autoPush;
        private int _pushThreshold = MaxShiftThreshold;

        private int _wrapTarget;
        private int _wrap;

        private int _clkDivInt = DefaultClockDivInt;
        private int _clkDivFrac;

        private PioFifoJoin _fifoJoin = PioFifoJoin.None;

        private int _gpioBase;

        private PioMovStatusSel _movStatusSel = PioMovStatusSel.TxLevel;
        private int _movStatusN;

        private bool _outSticky;
        private bool _inlineOutEn;
        private int _outEnSel;

        /// <summary>
        /// Creates an empty configuration (clkdiv = 1.0, no pins mapped).
        /// </summary>
        public PioStateMachineConfig()
        {
        }

        /// <summary>
        /// Builds a configuration pre-populated from an assembled program loaded at
        /// <paramref name="offset"/>: wrap/side-set/shift defaults are taken from the
        /// program (matching the SDK's <c>*_program_get_default_config</c>).
        /// </summary>
        /// <param name="program">The assembled program.</param>
        /// <param name="offset">The absolute instruction-memory offset where the program will be loaded (0..31).</param>
        /// <exception cref="ArgumentNullException"><paramref name="program"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Offset must be 0..31.</exception>
        public static PioStateMachineConfig FromProgram(PioProgram program, int offset)
        {
            if (program == null)
            {
                throw new ArgumentNullException();
            }

            if (offset < 0 || offset > 31)
            {
                throw new ArgumentException();
            }

            PioStateMachineConfig cfg = new PioStateMachineConfig();
            cfg._wrapTarget = offset + program.WrapTarget;
            cfg._wrap = offset + program.Wrap;
            cfg._sideSetCount = program.SideSetCount;
            cfg._sideSetOpt = program.SideSetOptional;
            cfg._sideSetPinDirs = program.SideSetPinDirs;
            cfg._outShiftDir = program.OutShiftDir;
            cfg._autoPull = program.AutoPull;
            cfg._pullThreshold = program.PullThreshold;
            cfg._inShiftDir = program.InShiftDir;
            cfg._autoPush = program.AutoPush;
            cfg._pushThreshold = program.PushThreshold;
            return cfg;
        }

        /// <summary>
        /// Maps the OUT pin group (base GPIO and consecutive pin count).
        /// </summary>
        /// <param name="basePin">The base GPIO pin.</param>
        /// <param name="count">The number of consecutive pins.</param>
        public PioStateMachineConfig OutPins(int basePin, int count)
        {
            ValidatePinGroup(basePin, count);
            _outBase = basePin;
            _outCount = count;
            return this;
        }

        /// <summary>
        /// Maps the SET pin group (base GPIO and consecutive pin count, 0..5).
        /// </summary>
        /// <param name="basePin">The base GPIO pin.</param>
        /// <param name="count">The number of consecutive pins.</param>
        /// <exception cref="ArgumentException">SET pin count must be 0..5.</exception>
        public PioStateMachineConfig SetPins(int basePin, int count)
        {
            if (count < 0 || count > 5)
            {
                throw new ArgumentException();
            }

            ValidatePinBase(basePin);
            _setBase = basePin;
            _setCount = count;
            return this;
        }

        /// <summary>
        /// Maps the side-set pin base. The count comes from the program/assembler.
        /// </summary>
        /// <param name="basePin">The base GPIO pin.</param>
        public PioStateMachineConfig SideSetPins(int basePin)
        {
            ValidatePinBase(basePin);
            _sideSetBase = basePin;
            return this;
        }

        /// <summary>
        /// Maps the IN pin base.
        /// </summary>
        /// <param name="basePin">The base GPIO pin.</param>
        public PioStateMachineConfig InPins(int basePin)
        {
            ValidatePinBase(basePin);
            _inBase = basePin;
            _inBaseSet = true;
            return this;
        }

        /// <summary>
        /// Selects the GPIO used by the JMP PIN condition.
        /// </summary>
        /// <param name="pin">The GPIO pin.</param>
        public PioStateMachineConfig JmpPin(int pin)
        {
            ValidatePinBase(pin);
            _jmpPin = pin;
            _jmpPinSet = true;
            return this;
        }

        /// <summary>
        /// Sets the clock divider (1.0 .. 65536.0). The SM advances at sysclk/div.
        /// Stored as the integer and 1/256 fractional parts of the CLKDIV register.
        /// </summary>
        /// <param name="div">The clock divider, 1.0 to 65536.0.</param>
        /// <exception cref="ArgumentException">Clock divisor must be 1.0..65536.0.</exception>
        public PioStateMachineConfig ClockDivisor(float div)
        {
            // closed-range test so NaN (which fails every ordered comparison) is rejected too
            if (!(div >= 1.0f && div <= 65536.0f))
            {
                throw new ArgumentException();
            }

            int intPart = (int)div;
            int frac = (int)((div - intPart) * 256.0f + 0.5f);
            if (frac > 255)
            {
                frac = 0;
                intPart += 1;
            }

            // 65536 is represented by integer field 0 with zero fraction.
            _clkDivInt = intPart >= 65536 ? 0 : intPart;
            _clkDivFrac = frac;
            return this;
        }

        /// <summary>
        /// Default system clock used by <see cref="ClockFromFrequency"/> — the RP2040 power-on
        /// default (125 MHz). Pass the real sysclk to that method if you changed it or run on RP2350.
        /// </summary>
        public const float DefaultSystemClockHz = 125000000f;

        /// <summary>
        /// Sets the clock divider from a target state-machine execution frequency rather than a raw
        /// divisor: <c>div = sysClockHz / frequencyHz</c> (mirrors the SDK pattern around
        /// <c>clock_get_hz(clk_sys)</c>). The divisor is clamped to the legal 1.0..65536.0 range, so
        /// frequencies above sysclk peg at div 1.0 and very low ones at div 65536.0.
        /// </summary>
        /// <param name="frequencyHz">Desired SM tick frequency in Hz (must be positive).</param>
        /// <param name="sysClockHz">System clock in Hz (defaults to <see cref="DefaultSystemClockHz"/>).</param>
        /// <exception cref="ArgumentException">Frequency must be positive.</exception>
        /// <exception cref="ArgumentException">System clock must be positive.</exception>
        public PioStateMachineConfig ClockFromFrequency(float frequencyHz, float sysClockHz = DefaultSystemClockHz)
        {
            // ordered test so NaN (which fails every ordered comparison) is rejected too
            if (!(frequencyHz > 0f))
            {
                throw new ArgumentException();
            }

            if (!(sysClockHz > 0f))
            {
                throw new ArgumentException();
            }

            float div = sysClockHz / frequencyHz;
            if (div < 1.0f)
            {
                div = 1.0f;
            }
            else if (div > 65536.0f)
            {
                div = 65536.0f;
            }

            return ClockDivisor(div);
        }

        /// <summary>
        /// Sets the FIFO join mode. The PUT/GET modes (RP2350/PIO v1) make the RX FIFO a
        /// random-access register file for the indexed RX-FIFO MOV instructions.
        /// </summary>
        /// <param name="mode">The FIFO join mode.</param>
        public PioStateMachineConfig FifoJoin(PioFifoJoin mode)
        {
            _fifoJoin = mode;
            return this;
        }

        /// <summary>
        /// (RP2350) Selects the block's GPIO base — <c>0</c> (pins 0..31) or <c>16</c> (pins 16..47)
        /// — so a state machine can reach the upper GPIOs on 48-pin packages (RP2350B). Pin mappings
        /// stay <em>absolute</em> GPIO numbers; the base is subtracted to form the 5-bit pin fields.
        /// </summary>
        /// <param name="gpioBase">The GPIO base.</param>
        /// <exception cref="ArgumentException">GPIO base must be 0 or 16.</exception>
        public PioStateMachineConfig GpioBase(int gpioBase)
        {
            if (gpioBase != 0 && gpioBase != 16)
            {
                throw new ArgumentException();
            }

            _gpioBase = gpioBase;
            return this;
        }

        /// <summary>
        /// Overrides the wrap region (absolute program offsets). Normally seeded by FromProgram.
        /// </summary>
        /// <param name="wrapTarget">The PC to wrap back to (0..31).</param>
        /// <param name="wrap">The last PC before wrapping (0..31), at or after <paramref name="wrapTarget"/>.</param>
        /// <returns>This configuration, for chaining.</returns>
        /// <exception cref="ArgumentOutOfRangeException">A value is outside 0..31, or <paramref name="wrapTarget"/> is past <paramref name="wrap"/>.</exception>
        public PioStateMachineConfig Wrap(int wrapTarget, int wrap)
        {
            if (wrapTarget < 0 || wrapTarget > 31 || wrap < 0 || wrap > 31 || wrapTarget > wrap)
            {
                throw new ArgumentOutOfRangeException();
            }

            _wrapTarget = wrapTarget;
            _wrap = wrap;
            return this;
        }

        /// <summary>
        /// Source for the <c>mov ..., status</c> instruction: all-ones when the selected FIFO's level is
        /// less than <paramref name="n"/>, else all-zeroes (EXECCTRL STATUS_SEL/STATUS_N).
        /// </summary>
        /// <param name="sel">The FIFO to monitor.</param>
        /// <param name="n">The threshold (0..15).</param>
        /// <returns>This configuration, for chaining.</returns>
        /// <exception cref="ArgumentException">Status N must be 0..15.</exception>
        public PioStateMachineConfig MovStatus(PioMovStatusSel sel, int n)
        {
            if (n < 0 || n > 15)
            {
                throw new ArgumentException();
            }

            _movStatusSel = sel;
            _movStatusN = n;
            return this;
        }

        /// <summary>
        /// OUT special behaviour: <paramref name="sticky"/> keeps the last OUT/SET value driven each
        /// cycle; <paramref name="enableInlineOut"/> uses OUT bit <paramref name="enableBitIndex"/> as a
        /// per-cycle output enable (EXECCTRL OUT_STICKY / INLINE_OUT_EN / OUT_EN_SEL).
        /// </summary>
        /// <param name="sticky">Indicates whether to keep the last OUT/SET value driven each cycle.</param>
        /// <param name="enableInlineOut">Indicates whether to use the inline output enable.</param>
        /// <param name="enableBitIndex">The index of the bit used for output enable (0..31).</param>
        /// <returns>This configuration, for chaining.</returns>
        /// <exception cref="ArgumentException">Enable bit index must be 0..31.</exception>
        public PioStateMachineConfig OutSpecial(bool sticky, bool enableInlineOut, int enableBitIndex)
        {
            if (enableBitIndex < 0 || enableBitIndex > 31)
            {
                throw new ArgumentException();
            }

            _outSticky = sticky;
            _inlineOutEn = enableInlineOut;
            _outEnSel = enableBitIndex;
            return this;
        }

        /// <summary>
        /// Flattens the configuration into the fixed-layout blob handed to native interop.
        /// </summary>
        /// <returns>The configuration blob.</returns>
        public uint[] ToBlob()
        {
            // PINCTRL fields are relative to the GPIO base, so every mapped pin must sit in the window
            CheckWindow(_outBase, _outCount);
            CheckWindow(_setBase, _setCount);
            CheckWindow(_sideSetBase, _sideSetCount);
            CheckWindow(_inBase, _inBaseSet ? 1 : 0);
            CheckWindow(_jmpPin, _jmpPinSet ? 1 : 0);

            // wrap/wrap-target are absolute PCs; reject out-of-range or forward-wrapping values
            if (_wrapTarget < 0 || _wrapTarget > 31 || _wrap < 0 || _wrap > 31 || _wrapTarget > _wrap)
            {
                throw new ArgumentException();
            }

            uint[] b = new uint[BlobLength];
            b[IdxOutBase] = (uint)Rel(_outBase);
            b[IdxOutCount] = (uint)_outCount;
            b[IdxSetBase] = (uint)Rel(_setBase);
            b[IdxSetCount] = (uint)_setCount;
            b[IdxSideSetBase] = (uint)Rel(_sideSetBase);
            b[IdxSideSetCount] = (uint)_sideSetCount;
            b[IdxSideSetOpt] = _sideSetOpt ? 1u : 0u;
            b[IdxSideSetPinDirs] = _sideSetPinDirs ? 1u : 0u;
            b[IdxInBase] = (uint)Rel(_inBase);
            b[IdxJmpPin] = (uint)Rel(_jmpPin);
            b[IdxOutShiftRight] = _outShiftDir == ShiftDirection.Right ? 1u : 0u;
            b[IdxAutoPull] = _autoPull ? 1u : 0u;
            b[IdxPullThreshold] = (uint)_pullThreshold;
            b[IdxInShiftRight] = _inShiftDir == ShiftDirection.Right ? 1u : 0u;
            b[IdxAutoPush] = _autoPush ? 1u : 0u;
            b[IdxPushThreshold] = (uint)_pushThreshold;
            b[IdxWrapTarget] = (uint)_wrapTarget;
            b[IdxWrap] = (uint)_wrap;
            b[IdxClkDivInt] = (uint)_clkDivInt;
            b[IdxClkDivFrac] = (uint)_clkDivFrac;
            b[IdxFifoJoin] = (uint)_fifoJoin;
            b[IdxGpioBase] = (uint)_gpioBase;
            b[IdxMovStatusSel] = (uint)_movStatusSel;
            b[IdxMovStatusN] = (uint)_movStatusN;
            b[IdxOutSticky] = _outSticky ? 1u : 0u;
            b[IdxInlineOutEn] = _inlineOutEn ? 1u : 0u;
            b[IdxOutEnSel] = (uint)_outEnSel;
            return b;
        }

        // Pin relative to the GPIO base (clamped: unused groups keep base 0 harmlessly).
        private int Rel(int absPin)
        {
            int rel = absPin - _gpioBase;
            return rel < 0 ? 0 : rel;
        }

        // A mapped pin group must fall inside the 32-pin window above the GPIO base.
        private void CheckWindow(int basePin, int count)
        {
            if (count <= 0)
            {
                return;
            }

            if (basePin < _gpioBase || basePin + count - 1 > _gpioBase + 31)
            {
                throw new ArgumentException();
            }
        }

        private static void ValidatePinBase(int basePin)
        {
            if (basePin < 0 || basePin > 47)
            {
                throw new ArgumentException();
            }
        }

        private static void ValidatePinGroup(int basePin, int count)
        {
            ValidatePinBase(basePin);
            if (count < 0 || count > 32)
            {
                throw new ArgumentException();
            }
        }
    }
}
