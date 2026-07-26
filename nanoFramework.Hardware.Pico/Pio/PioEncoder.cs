//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System;

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// Stateless helpers that build the 16-bit PIO opcodes. The base-opcode methods
    /// emit the instruction without the shared delay/side-set field, which is layered
    /// on afterwards by <see cref="PackDelaySideSet"/> once the program's side-set
    /// configuration is known.
    /// </summary>
    public static class PioEncoder
    {
        /// <summary>
        /// Base opcode for the JMP instruction.
        /// </summary>
        internal const int OpJmp = 0x0000;
        /// <summary>
        /// Base opcode for the WAIT instruction.
        /// </summary>
        internal const int OpWait = 0x2000;
        /// <summary>
        /// Base opcode for the IN instruction.
        /// </summary>
        internal const int OpIn = 0x4000;
        /// <summary>
        /// Base opcode for the OUT instruction.
        /// </summary>
        internal const int OpOut = 0x6000;
        /// <summary>
        /// Base opcode for the PUSH instruction.
        /// </summary>
        internal const int OpPush = 0x8000;
        /// <summary>
        /// Base opcode for the PULL instruction.
        /// </summary>
        internal const int OpPull = 0x8080;
        /// <summary>
        /// Base opcode for the MOV instruction.
        /// </summary>
        internal const int OpMov = 0xA000;
        /// <summary>
        /// Base opcode for the IRQ instruction.
        /// </summary>
        internal const int OpIrq = 0xC000;
        /// <summary>
        /// Base opcode for the SET instruction.
        /// </summary>
        internal const int OpSet = 0xE000;

        /// <summary>
        /// Jmp &lt;cond&gt; &lt;addr&gt;. Address is an absolute program offset 0..31.
        /// </summary>
        /// <param name="condition">The jump condition.</param>
        /// <param name="address">The absolute program offset to jump to (0..31).</param>
        /// <returns>The encoded instruction.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="address"/> is outside 0..31.</exception>
        public static ushort Jmp(PioCondition condition, int address)
        {
            if (address < 0 || address > 31)
            {
                throw new ArgumentOutOfRangeException();
            }

            return (ushort)(OpJmp | (((int)condition & 0x7) << 5) | address);
        }

        /// <summary>
        /// Wait &lt;polarity&gt; &lt;source&gt; &lt;index&gt;.
        /// </summary>
        /// <param name="polarity">The polarity of the wait signal.</param>
        /// <param name="source">The source of the wait signal.</param>
        /// <param name="index">The index of the wait signal (0..31).</param>
        /// <returns>The encoded instruction.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside 0..31.</exception>
        public static ushort Wait(bool polarity, PioWaitSource source, int index)
        {
            if (index < 0 || index > 31)
            {
                throw new ArgumentOutOfRangeException();
            }

            return (ushort)(OpWait | (polarity ? 0x80 : 0) | (((int)source & 0x3) << 5) | index);
        }

        /// <summary>
        /// IN &lt;source&gt;, &lt;bitCount&gt; (1..32; 32 encodes as 0).
        /// </summary>
        /// <param name="source">The source of the input signal.</param>
        /// <param name="bitCount">The number of bits to input (1..32).</param>
        /// <returns>The encoded instruction.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bitCount"/> is outside 1..32.</exception>
        public static ushort In(SourceOperand source, int bitCount)
        {
            if (bitCount < 1 || bitCount > 32)
            {
                throw new ArgumentOutOfRangeException();
            }

            return (ushort)(OpIn | (((int)source & 0x7) << 5) | (bitCount & 0x1F));
        }

        /// <summary>
        /// Out &lt;dest&gt;, &lt;bitCount&gt; (1..32; 32 encodes as 0).
        /// </summary>
        /// <param name="dest">The destination of the output signal.</param>
        /// <param name="bitCount">The number of bits to output (1..32).</param>
        /// <returns>The encoded instruction.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bitCount"/> is outside 1..32.</exception>
        public static ushort Out(DestinationOperand dest, int bitCount)
        {
            if (bitCount < 1 || bitCount > 32)
            {
                throw new ArgumentOutOfRangeException();
            }

            return (ushort)(OpOut | (((int)dest & 0x7) << 5) | (bitCount & 0x1F));
        }

        /// <summary>
        /// Push [iffull] [block].
        /// </summary>
        /// <param name="ifFull">Indicates if the operation should be performed when the FIFO is full.</param>
        /// <param name="block">Indicates if the operation should block.</param>
        /// <returns>The encoded instruction.</returns>
        public static ushort Push(bool ifFull, bool block)
        {
            return (ushort)(OpPush | (ifFull ? 0x40 : 0) | (block ? 0x20 : 0));
        }

        /// <summary>
        /// Pull [ifempty] [block].
        /// </summary>
        /// <param name="ifEmpty">Indicates if the operation should be performed when the FIFO is empty.</param>
        /// <param name="block">Indicates if the operation should block.</param>
        /// <returns>The encoded instruction.</returns>
        public static ushort Pull(bool ifEmpty, bool block)
        {
            return (ushort)(OpPull | (ifEmpty ? 0x40 : 0) | (block ? 0x20 : 0));
        }

        /// <summary>
        /// Mov &lt;dest&gt;, [op] &lt;src&gt;.
        /// </summary>
        /// <param name="dest">The destination of the move operation.</param>
        /// <param name="op">The operation to perform.</param>
        /// <param name="src">The source of the move operation.</param>
        /// <returns>The encoded instruction.</returns>
        public static ushort Mov(DestinationOperand dest, PioMovOp op, SourceOperand src)
        {
            return (ushort)(OpMov | (((int)dest & 0x7) << 5) | (((int)op & 0x3) << 3) | ((int)src & 0x7));
        }

        #region RP2350 (PIO v1) indexed RX-FIFO MOV
        // PUSH/PULL opcode space, arg2 != 0: bit4 v1 form, bit3 fixed index, arg1 bit2 direction

        /// <summary>
        /// (v1) MOV RXFIFO[index], ISR — write ISR to RX FIFO slot 0..3.
        /// </summary>
        /// <param name="index">The RX FIFO slot index (0..3).</param>
        /// <returns>The encoded instruction.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside 0..3.</exception>
        public static ushort MovToRxFifo(int index)
        {
            if (index < 0 || index > 3)
            {
                throw new ArgumentOutOfRangeException();
            }

            return (ushort)(OpPush | 0x18 | index);
        }

        /// <summary>
        /// (v1) MOV RXFIFO[Y], ISR — write ISR to the RX FIFO slot selected by Y.
        /// </summary>
        /// <returns>The encoded instruction.</returns>
        public static ushort MovToRxFifoIndexedY()
        {
            return (ushort)(OpPush | 0x10);
        }

        /// <summary>
        /// (v1) MOV OSR, RXFIFO[index] — read RX FIFO slot 0..3 into OSR.
        /// </summary>
        /// <param name="index">The RX FIFO slot index (0..3).</param>
        /// <returns>The encoded instruction.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside 0..3.</exception>
        public static ushort MovFromRxFifo(int index)
        {
            if (index < 0 || index > 3)
            {
                throw new ArgumentOutOfRangeException();
            }

            return (ushort)(OpPush | 0x80 | 0x18 | index);
        }

        /// <summary>
        /// (v1) MOV OSR, RXFIFO[Y] — read the RX FIFO slot selected by Y into OSR.
        /// </summary>
        /// <returns>The encoded instruction.</returns>
        public static ushort MovFromRxFifoIndexedY()
        {
            return (ushort)(OpPush | 0x80 | 0x10);
        }

        #endregion

        /// <summary>
        /// IRQ [clear] [wait] &lt;index&gt; (0..7).
        /// </summary>
        /// <param name="clear">Indicates if the IRQ should be cleared.</param>
        /// <param name="wait">Indicates if the operation should wait for the IRQ.</param>
        /// <param name="index">The IRQ index (0..7).</param>
        /// <returns>The encoded instruction.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside 0..7.</exception>
        public static ushort Irq(bool clear, bool wait, int index)
        {
            if (index < 0 || index > 7)
            {
                throw new ArgumentOutOfRangeException();
            }

            return (ushort)(OpIrq | (clear ? 0x40 : 0) | (wait ? 0x20 : 0) | index);
        }

        /// <summary>
        /// Set &lt;dest&gt;, &lt;value&gt; (0..31).
        /// </summary>
        /// <param name="dest">The destination of the set operation.</param>
        /// <param name="value">The value to set (0..31).</param>
        /// <returns>The encoded instruction.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is outside 0..31.</exception>
        public static ushort Set(DestinationOperand dest, int value)
        {
            if (value < 0 || value > 31)
            {
                throw new ArgumentOutOfRangeException();
            }

            return (ushort)(OpSet | (((int)dest & 0x7) << 5) | value);
        }

        /// <summary>
        /// Nop is an alias for MOV Y, Y.
        /// </summary>
        /// <returns>The encoded instruction.</returns>
        public static ushort Nop()
        {
            return Mov(DestinationOperand.RegisterY, PioMovOp.None, SourceOperand.RegisterY);
        }

        /// <summary>
        /// Number of delay bits available given the side-set configuration.
        /// The 5-bit [12:8] field is split: side-set value bits (plus one enable bit
        /// when optional) take the top, the remaining low bits are delay.
        /// </summary>
        /// <param name="sideSetCount">The number of side-set value bits.</param>
        /// <param name="sideSetOpt">Indicates if side-set is optional.</param>
        /// <returns>The number of delay bits available.</returns>
        public static int DelayBits(int sideSetCount, bool sideSetOpt)
        {
            return 5 - sideSetCount - (sideSetOpt ? 1 : 0);
        }

        /// <summary>
        /// Layers the delay and optional side-set value onto a base opcode.
        /// Mirrors pico-sdk pio_encode_delay / pio_encode_sideset[_opt].
        /// </summary>
        /// <param name="baseBits">Opcode from one of the encode methods (no delay/side field).</param>
        /// <param name="delay">Delay cycles (0..2^delayBits-1).</param>
        /// <param name="sideValue">Side-set value to assert.</param>
        /// <param name="sideUsed">Whether this instruction asserts a side-set value.</param>
        /// <param name="sideSetCount">Side-set value-bit count (excludes the opt enable bit).</param>
        /// <param name="sideSetOpt">Whether side-set is optional for this program.</param>
        /// <returns>The opcode with the delay/side field layered on.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="sideSetCount"/> (plus the optional enable bit) exceeds the 5-bit delay/side field.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="delay"/> or <paramref name="sideValue"/> does not fit its field.</exception>
        public static ushort PackDelaySideSet(
            ushort baseBits,
            int delay,
            int sideValue,
            bool sideUsed,
            int sideSetCount,
            bool sideSetOpt)
        {
            // guard the layout so the packed field can't spill past bits [12:8] into the opcode
            if (sideSetCount < 0 || sideSetCount > 5 || sideSetCount + (sideSetOpt ? 1 : 0) > 5)
            {
                throw new ArgumentOutOfRangeException();
            }

            int delayBits = DelayBits(sideSetCount, sideSetOpt);

            if (delay < 0 || delay > (1 << delayBits) - 1)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (sideUsed && sideSetCount > 0 && (sideValue < 0 || sideValue > (1 << sideSetCount) - 1))
            {
                throw new ArgumentOutOfRangeException();
            }

            int field = delay;

            if (sideUsed && sideSetCount > 0)
            {
                if (sideSetOpt)
                {
                    // enable bit at top of the 5-bit field (instr bit 12)
                    field |= 0x10;
                }

                field |= sideValue << delayBits;
            }

            return (ushort)(baseBits | (field << 8));
        }
    }
}
