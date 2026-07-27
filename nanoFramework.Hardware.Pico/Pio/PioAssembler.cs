//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System;

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// Builds a <see cref="PioProgram"/> from fluent instruction calls. Modeled on
    /// <c>System.Reflection.Emit.ILGenerator</c>: <see cref="DefineLabel"/> then
    /// <see cref="MarkLabel"/>, with forward references resolved in <see cref="Build"/>.
    /// </summary>
    public sealed class PioAssembler
    {
        // PIO instruction memory holds at most 32 entries
        private const int MaxInstructions = 32;

        // per-instruction parallel arrays, allocated once
        private readonly ushort[] _baseBits;
        private readonly int[] _delay;
        private readonly int[] _sideValue;
        private readonly bool[] _sideUsed;
        // non-null slot => resolve a JMP target at Build
        private readonly PioLabel[] _jmpLabel;

        // instructions emitted (may exceed 32 so Build can report it)
        private int _count;
        // ids handed out by DefineLabel
        private int _labelCount;

        private readonly PioVersion _version;
        private readonly int _sideSetCount;
        private readonly bool _sideSetOpt;
        private readonly bool _sideSetPinDirs;

        private int _wrapTarget = -1;
        private int _wrap = -1;
        private sbyte _origin = -1;

        private ShiftDirection _outShiftDir = ShiftDirection.Right;
        private bool _autoPull = false;
        private int _pullThreshold = 32;
        private ShiftDirection _inShiftDir = ShiftDirection.Right;
        private bool _autoPush = false;
        private int _pushThreshold = 32;

        #region Construction

        /// <summary>
        /// Creates an assembler with default options (no side-set, RP2040).
        /// </summary>
        public PioAssembler() : this(null)
        {
        }

        /// <summary>
        /// Creates an assembler with the given options.
        /// </summary>
        /// <param name="options">The assembler options.</param>
        /// <exception cref="ArgumentException">SideSetCount must be 0..5.</exception>
        /// <exception cref="ArgumentException">Side-set (count + optional enable bit) cannot exceed 5 bits.</exception>
        public PioAssembler(PioAssemblerOptions options)
        {
            if (options == null)
            {
                options = new PioAssemblerOptions();
            }

            if (options.SideSetCount < 0 || options.SideSetCount > 5)
            {
                throw new ArgumentOutOfRangeException();
            }

            int total = options.SideSetCount + (options.SideSetOption ? 1 : 0);
            if (total > 5)
            {
                throw new ArgumentException();
            }

            _version = options.Version;
            _sideSetCount = options.SideSetCount;
            _sideSetOpt = options.SideSetOption;
            _sideSetPinDirs = options.SideSetPinDirs;

            // allocated once for the assembler's lifetime
            _baseBits = new ushort[MaxInstructions];
            _delay = new int[MaxInstructions];
            _sideValue = new int[MaxInstructions];
            _sideUsed = new bool[MaxInstructions];
            _jmpLabel = new PioLabel[MaxInstructions];
        }

        /// <summary>
        /// Maximum delay encodable per instruction given the side-set configuration.
        /// </summary>
        public int MaxDelay
        {
            get
            {
                return (1 << PioEncoder.DelayBits(_sideSetCount, _sideSetOpt)) - 1;
            }
        }

        /// <summary>
        /// Number of instructions emitted so far (the current program offset).
        /// </summary>
        public int Count
        {
            get
            {
                return _count;
            }
        }

        #endregion

        #region Labels and directives

        /// <summary>
        /// Allocates an unbound label. Place it later with <see cref="MarkLabel"/>.
        /// </summary>
        /// <returns>The created <see cref="PioLabel"/>.</returns>
        public PioLabel DefineLabel()
        {
            // reference type so MarkLabel can bind the address in place
            return new PioLabel(_labelCount++, this);
        }

        /// <summary>
        /// Binds a label to the current program offset.
        /// </summary>
        /// <param name="label">The label to bind, created by this assembler's <see cref="DefineLabel"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="label"/> belongs to another assembler.</exception>
        /// <exception cref="InvalidOperationException">Label already marked.</exception>
        public void MarkLabel(PioLabel label)
        {
            if (label == null)
            {
                throw new ArgumentNullException();
            }

            if (!object.ReferenceEquals(label.Owner, this))
            {
                throw new ArgumentException();
            }

            if (label.Address >= 0)
            {
                throw new InvalidOperationException();
            }

            label.Address = _count;
        }

        /// <summary>
        /// Marks the wrap-target (PC wraps back here). Defaults to offset 0.
        /// </summary>
        /// <exception cref="InvalidOperationException">.wrap_target already set.</exception>
        public void WrapTarget()
        {
            if (_wrapTarget >= 0)
            {
                throw new InvalidOperationException();
            }

            _wrapTarget = _count;
        }

        /// <summary>
        /// Marks the wrap point (the last instruction that wraps). Defaults to the final instruction.
        /// </summary>
        /// <exception cref="InvalidOperationException">.wrap requires at least one instruction.</exception>
        /// <exception cref="InvalidOperationException">.wrap already set.</exception>
        public void Wrap()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException();
            }

            if (_wrap >= 0)
            {
                throw new InvalidOperationException();
            }

            _wrap = _count - 1;
        }

        /// <summary>
        /// Pins the program to a fixed load offset 0..31 (default: relocatable, -1).
        /// </summary>
        /// <param name="origin">The load offset.</param>
        /// <exception cref="ArgumentException">Origin must be 0..31.</exception>
        public void SetOrigin(int origin)
        {
            if (origin < 0 || origin > 31)
            {
                throw new ArgumentOutOfRangeException();
            }

            _origin = (sbyte)origin;
        }

        /// <summary>
        /// Default OUT/pull shift behavior (equivalent to pioasm <c>.out</c>).
        /// </summary>
        /// <param name="direction">The shift direction.</param>
        /// <param name="autoPull">Indicates whether to automatically pull data.</param>
        /// <param name="threshold">The threshold for automatic pulling.</param>
        public void OutShift(ShiftDirection direction, bool autoPull, int threshold)
        {
            ValidateThreshold(threshold);
            _outShiftDir = direction;
            _autoPull = autoPull;
            _pullThreshold = threshold;
        }

        /// <summary>
        /// Default IN/push shift behavior (equivalent to pioasm <c>.in</c>).
        /// </summary>
        /// <param name="direction">The shift direction.</param>
        /// <param name="autoPush">Indicates whether to automatically push data.</param>
        /// <param name="threshold">The threshold for automatic pushing.</param>
        public void InShift(ShiftDirection direction, bool autoPush, int threshold)
        {
            ValidateThreshold(threshold);
            _inShiftDir = direction;
            _autoPush = autoPush;
            _pushThreshold = threshold;
        }

        #endregion

        #region Instructions

        /// <summary>
        /// Jmp &lt;label&gt; (unconditional).
        /// </summary>
        /// <param name="target">The label to jump to.</param>
        /// <returns>The instruction reference.</returns>
        public PioInstructionRef Jmp(PioLabel target)
        {
            return Jmp(PioCondition.Always, target);
        }

        /// <summary>
        /// Jmp &lt;cond&gt; &lt;label&gt;.
        /// </summary>
        /// <param name="condition">The condition for the jump.</param>
        /// <param name="target">The label to jump to.</param>
        /// <returns>The instruction reference.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="target"/> belongs to another assembler.</exception>
        public PioInstructionRef Jmp(PioCondition condition, PioLabel target)
        {
            if (target == null)
            {
                throw new ArgumentNullException();
            }

            if (!object.ReferenceEquals(target.Owner, this))
            {
                throw new ArgumentException();
            }

            int index = Add(PioEncoder.Jmp(condition, 0));
            if (index < MaxInstructions)
            {
                _jmpLabel[index] = target;
            }

            return new PioInstructionRef(this, index);
        }

        /// <summary>
        /// Jmp to an absolute program offset.
        /// </summary>
        /// <param name="condition">The condition for the jump.</param>
        /// <param name="address">The absolute address to jump to.</param>
        /// <returns>The instruction reference.</returns>
        /// <exception cref="ArgumentException">Jump address must be 0..31.</exception>
        public PioInstructionRef Jmp(PioCondition condition, int address)
        {
            if (address < 0 || address > 31)
            {
                throw new ArgumentOutOfRangeException();
            }

            return new PioInstructionRef(this, Add(PioEncoder.Jmp(condition, address)));
        }

        /// <summary>
        /// Wait &lt;polarity&gt; &lt;source&gt; &lt;index&gt;.
        /// </summary>
        /// <param name="polarity">The polarity of the wait condition.</param>
        /// <param name="source">The source of the wait condition.</param>
        /// <param name="index">The index for the wait condition.</param>
        /// <returns>The instruction reference.</returns>
        /// <exception cref="ArgumentException">Wait index must be 0..31.</exception>
        public PioInstructionRef Wait(bool polarity, PioWaitSource source, int index)
        {
            if (index < 0 || index > 31)
            {
                throw new ArgumentOutOfRangeException();
            }

            return new PioInstructionRef(this, Add(PioEncoder.Wait(polarity, source, index)));
        }

        /// <summary>Wait for an absolute GPIO to reach <paramref name="level"/>.</summary>
        /// <param name="level">The level to wait for.</param>
        /// <param name="gpio">The GPIO to wait for.</param>
        /// <returns>The instruction reference.</returns>
        public PioInstructionRef WaitGpio(bool level, int gpio) => Wait(level, PioWaitSource.Gpio, gpio);

        /// <summary>Wait for an IN-base-relative pin to reach <paramref name="level"/>.</summary>
        /// <param name="level">The level to wait for.</param>
        /// <param name="pin">The pin to wait for.</param>
        /// <returns>The instruction reference.</returns>
        public PioInstructionRef WaitPin(bool level, int pin) => Wait(level, PioWaitSource.Pin, pin);

        /// <summary>Wait for IRQ flag <paramref name="irq"/> (0..7) to reach <paramref name="level"/>.</summary>
        /// <param name="level">The level to wait for.</param>
        /// <param name="irq">The IRQ flag to wait for.</param>
        /// <returns>The instruction reference.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="irq"/> is not in the range 0..7.</exception>
        public PioInstructionRef WaitIrq(bool level, int irq)
        {
            if (irq < 0 || irq > 7)
            {
                throw new ArgumentOutOfRangeException();
            }

            return Wait(level, PioWaitSource.Irq, irq);
        }

        /// <summary>IN &lt;source&gt;, &lt;bitCount&gt; (1..32).</summary>
        /// <param name="source">The source for the IN instruction.</param>
        /// <param name="bitCount">The number of bits to read.</param>
        /// <returns>The instruction reference.</returns>
        public PioInstructionRef In(SourceOperand source, int bitCount)
        {
            ValidateBitCount(bitCount);
            RequireInSource(source);
            return new PioInstructionRef(this, Add(PioEncoder.In(source, bitCount)));
        }

        /// <summary>Out &lt;dest&gt;, &lt;bitCount&gt; (1..32).</summary>
        /// <param name="dest">The destination for the Out instruction.</param>
        /// <param name="bitCount">The number of bits to write.</param>
        /// <returns>The instruction reference.</returns>
        public PioInstructionRef Out(DestinationOperand dest, int bitCount)
        {
            ValidateBitCount(bitCount);
            RequireOutDest(dest);
            return new PioInstructionRef(this, Add(PioEncoder.Out(dest, bitCount)));
        }

        /// <summary>Push [iffull] [block].</summary>
        /// <param name="ifFull">Indicates whether to push if full.</param>
        /// <param name="block">Indicates whether to block.</param>
        /// <returns>The instruction reference.</returns>
        public PioInstructionRef Push(bool ifFull = false, bool block = true)
        {
            return new PioInstructionRef(this, Add(PioEncoder.Push(ifFull, block)));
        }

        /// <summary>Pull [ifempty] [block].</summary>
        /// <param name="ifEmpty">Indicates whether to pull if empty.</param>
        /// <param name="block">Indicates whether to block.</param>
        /// <returns>The instruction reference.</returns>
        public PioInstructionRef Pull(bool ifEmpty = false, bool block = true)
        {
            return new PioInstructionRef(this, Add(PioEncoder.Pull(ifEmpty, block)));
        }

        /// <summary>Mov &lt;dest&gt;, &lt;src&gt;.</summary>
        /// <param name="dest">The destination for the MOV instruction.</param>
        /// <param name="src">The source for the MOV instruction.</param>
        /// <returns>The instruction reference.</returns>
        public PioInstructionRef Mov(DestinationOperand dest, SourceOperand src)
        {
            return Mov(dest, PioMovOp.None, src);
        }

        /// <summary>Mov &lt;dest&gt;, &lt;op&gt; &lt;src&gt;.</summary>
        /// <param name="dest">The destination for the MOV instruction.</param>
        /// <param name="op">The operation for the MOV instruction.</param>
        /// <param name="src">The source for the MOV instruction.</param>
        /// <returns>The instruction reference.</returns>
        public PioInstructionRef Mov(DestinationOperand dest, PioMovOp op, SourceOperand src)
        {
            RequireMovDest(dest);
            RequireMovSrc(src);
            return new PioInstructionRef(this, Add(PioEncoder.Mov(dest, op, src)));
        }

        /// <summary>
        /// (RP2350/PIO v1) MOV RXFIFO[index], ISR — write ISR to RX FIFO slot 0..3.
        /// </summary>
        /// <param name="index">The RX FIFO index (0..3).</param>
        /// <returns>The instruction reference.</returns>
        /// <exception cref="ArgumentException">RX FIFO index must be 0..3.</exception>
        public PioInstructionRef MovToRxFifo(int index)
        {
            RequireV1("MOV RXFIFO[]");
            if (index < 0 || index > 3)
            {
                throw new ArgumentOutOfRangeException();
            }

            return new PioInstructionRef(this, Add(PioEncoder.MovToRxFifo(index)));
        }

        /// <summary>
        /// (RP2350/PIO v1) MOV RXFIFO[Y], ISR — write ISR to the slot selected by Y.
        /// </summary>
        /// <returns>The instruction reference.</returns>
        public PioInstructionRef MovToRxFifoIndexedY()
        {
            RequireV1("MOV RXFIFO[Y]");
            return new PioInstructionRef(this, Add(PioEncoder.MovToRxFifoIndexedY()));
        }

        /// <summary>
        /// (RP2350/PIO v1) MOV OSR, RXFIFO[index] — read RX FIFO slot 0..3 into OSR.
        /// </summary>
        /// <param name="index">The RX FIFO index (0..3).</param>
        /// <exception cref="ArgumentException">RX FIFO index must be 0..3.</exception>
        public PioInstructionRef MovFromRxFifo(int index)
        {
            RequireV1("MOV OSR, RXFIFO[]");
            if (index < 0 || index > 3)
            {
                throw new ArgumentOutOfRangeException();
            }

            return new PioInstructionRef(this, Add(PioEncoder.MovFromRxFifo(index)));
        }

        /// <summary>
        /// (RP2350/PIO v1) MOV OSR, RXFIFO[Y] — read the slot selected by Y into OSR.
        /// </summary>
        /// <returns>The instruction reference.</returns>
        public PioInstructionRef MovFromRxFifoIndexedY()
        {
            RequireV1("MOV OSR, RXFIFO[Y]");
            return new PioInstructionRef(this, Add(PioEncoder.MovFromRxFifoIndexedY()));
        }

        private void RequireV1(string what)
        {
            if (_version < PioVersion.Rp2350)
            {
                throw new InvalidOperationException(what + " requires PioVersion.Rp2350 (PIO v1).");
            }
        }

        /// <summary>
        /// IRQ &lt;index&gt; [clear] [wait].
        /// </summary>
        /// <param name="index">The IRQ index (0..7).</param>
        /// <param name="clear">Indicates whether to clear the IRQ.</param>
        /// <param name="wait">Indicates whether to wait for the IRQ.</param>
        /// <returns>The instruction reference.</returns>
        /// <exception cref="ArgumentException">IRQ index must be 0..7.</exception>
        public PioInstructionRef Irq(int index, bool clear = false, bool wait = false)
        {
            if (index < 0 || index > 7)
            {
                throw new ArgumentOutOfRangeException();
            }

            return new PioInstructionRef(this, Add(PioEncoder.Irq(clear, wait, index)));
        }

        /// <summary>
        /// Set &lt;dest&gt;, &lt;value&gt; (0..31).
        /// </summary>
        /// <param name="dest">The destination for the Set instruction.</param>
        /// <param name="value">The value to set (0..31).</param>
        /// <returns>The instruction reference.</returns>
        /// <exception cref="ArgumentException">Set value must be 0..31.</exception>
        public PioInstructionRef Set(DestinationOperand dest, int value)
        {
            if (value < 0 || value > 31)
            {
                throw new ArgumentOutOfRangeException();
            }

            RequireSetDest(dest);
            return new PioInstructionRef(this, Add(PioEncoder.Set(dest, value)));
        }

        /// <summary>
        /// Nop (alias of MOV Y, Y).
        /// </summary>
        /// <returns>The instruction reference.</returns>
        public PioInstructionRef Nop()
        {
            return new PioInstructionRef(this, Add(PioEncoder.Nop()));
        }

        #endregion

        #region Build

        /// <summary>
        /// Resolves labels, packs delay/side-set, validates ranges, and returns the
        /// finished <see cref="PioProgram"/>. Throws with a clear message on any
        /// out-of-range delay/side value, unbound label, or oversized program.
        /// </summary>
        /// <exception cref="InvalidOperationException">Program is empty.</exception>
        /// <exception cref="InvalidOperationException">PIO program exceeds 32 instructions (</exception>
        /// <exception cref="InvalidOperationException">Unbound label referenced by JMP at offset </exception>
        public PioProgram Build()
        {
            int count = _count;
            if (count == 0)
            {
                throw new InvalidOperationException();
            }

            if (count > MaxInstructions)
            {
                throw new InvalidOperationException();
            }

            int maxDelay = MaxDelay;
            int maxSide = _sideSetCount > 0 ? (1 << _sideSetCount) - 1 : 0;
            bool sideMandatory = _sideSetCount > 0 && !_sideSetOpt;

            ushort[] words = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                ushort baseBits = _baseBits[i];

                PioLabel jmpLabel = _jmpLabel[i];
                if (jmpLabel != null)
                {
                    if (jmpLabel.Address < 0)
                    {
                        throw new InvalidOperationException();
                    }

                    // a resolved target must be a real PC in this program, never masked past the end
                    if (jmpLabel.Address >= count)
                    {
                        throw new InvalidOperationException();
                    }

                    baseBits = (ushort)(baseBits | jmpLabel.Address);
                }

                int delay = _delay[i];
                if (delay < 0 || delay > maxDelay)
                {
                    throw new ArgumentOutOfRangeException(
                        "Delay " + delay + " at offset " + i + " out of range 0.." + maxDelay +
                        " (side-set of " + _sideSetCount + " bit(s) leaves " +
                        PioEncoder.DelayBits(_sideSetCount, _sideSetOpt) + " delay bits).");
                }

                bool sideUsed = _sideUsed[i];
                int sideValue = _sideValue[i];

                if (sideUsed)
                {
                    if (_sideSetCount == 0)
                    {
                        throw new InvalidOperationException(
                            "Side-set used at offset " + i + " but the program declares no side-set bits.");
                    }

                    if (sideValue < 0 || sideValue > maxSide)
                    {
                        throw new ArgumentOutOfRangeException(
                            "Side-set value " + sideValue + " at offset " + i + " out of range 0.." + maxSide + ".");
                    }
                }
                else if (sideMandatory)
                {
                    // Mandatory side-set: an unspecified value encodes as 0 (matches `side 0`).
                    sideUsed = true;
                    sideValue = 0;
                }

                words[i] = PioEncoder.PackDelaySideSet(
                    baseBits, delay, sideValue, sideUsed, _sideSetCount, _sideSetOpt);
            }

            int wrapTarget = _wrapTarget >= 0 ? _wrapTarget : 0;
            int wrap = _wrap >= 0 ? _wrap : count - 1;

            // wrap and wrap-target must be valid PCs in this program, wrapping back not forward
            if (wrapTarget >= count || wrap >= count || wrapTarget > wrap)
            {
                throw new InvalidOperationException();
            }

            return new PioProgram(
                words,
                wrap,
                wrapTarget,
                _origin,
                _sideSetCount,
                _sideSetOpt,
                _sideSetPinDirs,
                _outShiftDir,
                _autoPull,
                _pullThreshold,
                _inShiftDir,
                _autoPush,
                _pushThreshold,
                _version);
        }

        // emit base bits to the next slot; counter may exceed MaxInstructions so Build can report it
        private int Add(ushort baseBits)
        {
            int index = _count;
            _count++;
            if (index < MaxInstructions)
            {
                _baseBits[index] = baseBits;
            }

            return index;
        }

        /// <summary>
        /// Attaches a post-instruction delay to a slot. Called back from <see cref="PioInstructionRef"/> by index.
        /// </summary>
        /// <param name="index">The instruction slot index.</param>
        /// <param name="cycles">The number of delay cycles to attach.</param>
        internal void ApplyDelay(int index, int cycles)
        {
            if (index < MaxInstructions)
            {
                _delay[index] = cycles;
            }
        }

        /// <summary>
        /// Attaches a side-set value to a slot. Called back from <see cref="PioInstructionRef"/> by index.
        /// </summary>
        /// <param name="index">The instruction slot index.</param>
        /// <param name="value">The side-set value to drive.</param>
        internal void ApplySide(int index, int value)
        {
            if (index < MaxInstructions)
            {
                _sideUsed[index] = true;
                _sideValue[index] = value;
            }
        }

        #endregion

        #region Validation helpers

        private static void ValidateBitCount(int bitCount)
        {
            if (bitCount < 1 || bitCount > 32)
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        private static void ValidateThreshold(int threshold)
        {
            if (threshold < 1 || threshold > 32)
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        private static void RequireSetDest(DestinationOperand dest)
        {
            if (dest != DestinationOperand.Pins && dest != DestinationOperand.RegisterX && dest != DestinationOperand.RegisterY && dest != DestinationOperand.PinDirs)
            {
                throw new ArgumentException();
            }
        }

        private static void RequireOutDest(DestinationOperand dest)
        {
            // Pins, X, Y, Null, PinDirs, Pc, Isr, Exec are all valid (every 3-bit code).
            if ((int)dest < 0 || (int)dest > 7)
            {
                throw new ArgumentException();
            }
        }

        private static void RequireInSource(SourceOperand source)
        {
            if (source != SourceOperand.Pins && source != SourceOperand.RegisterX && source != SourceOperand.RegisterY &&
                source != SourceOperand.Null && source != SourceOperand.InputShiftRegister && source != SourceOperand.OutputShiftRegister)
            {
                throw new ArgumentException();
            }
        }

        private static void RequireMovDest(DestinationOperand dest)
        {
            if (dest != DestinationOperand.Pins && dest != DestinationOperand.RegisterX && dest != DestinationOperand.RegisterY &&
                dest != DestinationOperand.Executes && dest != DestinationOperand.Pc && dest != DestinationOperand.InputShiftRegister && dest != DestinationOperand.OutputShiftRegister)
            {
                throw new ArgumentException();
            }
        }

        private static void RequireMovSrc(SourceOperand src)
        {
            // Pins, X, Y, Null, Status, Isr, Osr are valid MOV sources.
            if ((int)src < 0 || (int)src > 7)
            {
                throw new ArgumentException();
            }
        }
        #endregion

    }
}
