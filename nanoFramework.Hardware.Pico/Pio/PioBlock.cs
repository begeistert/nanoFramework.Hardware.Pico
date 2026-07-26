//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;
using nanoFramework.Runtime.Events;

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// Handles a PIO interrupt raised on a block. <paramref name="flags"/> is the bit mask of
    /// state-machine IRQ flags (0..3) that fired (bit <c>n</c> set means state-machine-relative <c>irq n</c>).
    /// </summary>
    public delegate void PioInterruptEventHandler(PioBlock sender, PioInterruptFlags flags);

    /// <summary>
    /// A single PIO block (instruction memory shared by four state machines). Wraps the
    /// native <c>hardware_pio</c> instance program-load and state-machine claiming.
    /// </summary>
    public sealed class PioBlock
    {
        private readonly int _index;

        private PioInterruptEventHandler _interruptCallbacks;
        private readonly object _irqLock = new object();
        private static readonly PioEventListener s_eventListener = new PioEventListener();

        /// <summary>
        /// Initializes a new instance of the <see cref="PioBlock"/> class.
        /// </summary>
        /// <param name="index">The index of the block.</param>
        internal PioBlock(int index)
        {
            _index = index;
            s_eventListener.AddBlock(this);
        }

        /// <summary>
        /// Block index (0..2 for Pico 1 and 0..3 for Pico 2).
        /// </summary>
        public int Index
        {
            get
            {
                return _index;
            }
        }

        /// <summary>
        /// Loads an assembled program into this block's instruction memory and returns
        /// the load offset (maps to <c>pio_add_program</c>).
        /// </summary>
        /// <param name="program">The assembled program to load.</param>
        /// <returns>The instruction-memory offset the program was loaded at.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="program"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">There is no room left in the block to load the program.</exception>
        public uint AddProgram(PioProgram program)
        {
            if (program == null)
            {
                throw new ArgumentNullException();
            }

            int offset = NativeAddProgram(_index, program.Instructions, program.Length, program.Origin);
            if (offset < 0)
            {
                throw new InvalidOperationException();
            }

            return (uint)offset;
        }

        /// <summary>
        /// Removes a previously added program (maps to <c>pio_remove_program</c>).
        /// </summary>
        /// <param name="program">The program previously returned by <see cref="AddProgram"/>.</param>
        /// <param name="offset">The load offset the program occupies.</param>
        /// <exception cref="ArgumentNullException"><paramref name="program"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> plus the program length exceeds the 32-word instruction memory.</exception>
        public void RemoveProgram(PioProgram program, uint offset)
        {
            if (program == null)
            {
                throw new ArgumentNullException();
            }

            // offset + length must fit the 32-word instruction memory; reject before the uint->int narrowing
            if (program.Length <= 0 || offset > (uint)(32 - program.Length))
            {
                throw new ArgumentOutOfRangeException();
            }

            NativeRemoveProgram(_index, program.Length, (int)offset);
        }

        /// <summary>
        /// Claims a free state machine on this block (maps to <c>pio_claim_unused_sm</c>).
        /// </summary>
        /// <returns>The newly claimed state machine.</returns>
        /// <exception cref="InvalidOperationException">All four state machines on the block are already claimed.</exception>
        public PioStateMachine ClaimStateMachine()
        {
            int stateMachine = NativeClaimUnusedSm(_index, true);
            if (stateMachine < 0)
            {
                throw new InvalidOperationException();
            }

            return new PioStateMachine(this, stateMachine, true);
        }

        /// <summary>
        /// Gets a specific state machine (0..3) on this block. Unlike <see cref="ClaimStateMachine"/>
        /// this does <em>not</em> mark the state machine as claimed, so the caller is responsible for avoiding
        /// collisions; prefer <see cref="ClaimStateMachine"/> unless a fixed state machine index is required.
        /// </summary>
        /// <param name="stateMachine">The state machine index (0..3).</param>
        /// <returns>The state machine instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="stateMachine"/> is less than 0 or greater than 3.</exception>
        public PioStateMachine StateMachine(int stateMachine)
        {
            if (stateMachine < 0 || stateMachine > 3)
            {
                throw new ArgumentOutOfRangeException();
            }

            return new PioStateMachine(this, stateMachine, false);
        }

        /// <summary>
        /// Routes a GPIO to this PIO block so a state machine can drive/read it (maps to
        /// <c>pio_gpio_init</c>: sets the pad's function select to PIO0/PIO1). Required before a
        /// pin mapped via OUT/SET/side-set/IN actually reaches the physical pad.
        /// </summary>
        /// <param name="pin">The GPIO to route.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pin"/> is less than 0 or greater than 47.</exception>
        public void InitGpio(int pin)
        {
            // native side enforces the per-chip GPIO ceiling
            if (pin < 0 || pin > 47)
            {
                throw new ArgumentOutOfRangeException();
            }

            NativeInitGpio(_index, pin);
        }

        /// <summary>
        /// Routes <paramref name="count"/> consecutive GPIOs from <paramref name="basePin"/> to this block.
        /// </summary>
        /// <param name="basePin">The first GPIO to route.</param>
        /// <param name="count">The number of consecutive GPIOs to route.</param>
        /// <exception cref="ArgumentOutOfRangeException">The span falls outside 0..47.</exception>
        public void InitGpioRange(int basePin, int count)
        {
            // validate the whole span first, so a bad range can't leave the block partly routed
            if (basePin < 0 || count < 0 || count > 48 || basePin > 48 - count)
            {
                throw new ArgumentOutOfRangeException();
            }

            for (int i = 0; i < count; i++)
            {
                InitGpio(basePin + i);
            }
        }

        /// <summary>
        /// Raises PIO IRQ flag <paramref name="irq"/> (0..7) from the CPU side (maps to IRQ_FORCE).
        /// </summary>
        /// <param name="irq">The IRQ flag to raise.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="irq"/> is less than 0 or greater than 7.</exception>
        public void ForceIrq(int irq)
        {
            if (irq < 0 || irq > 7)
            {
                throw new ArgumentOutOfRangeException();
            }

            NativeForceIrq(_index, irq);
        }

        /// <summary>
        /// Clears PIO IRQ flag <paramref name="irq"/> (0..7), e.g. one raised by a state machine's <c>irq</c>.
        /// </summary>
        /// <param name="irq">The IRQ flag to clear.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="irq"/> is less than 0 or greater than 7.</exception>
        public void ClearIrq(int irq)
        {
            if (irq < 0 || irq > 7)
            {
                throw new ArgumentOutOfRangeException();
            }

            NativeClearIrq(_index, irq);
        }

        /// <summary>
        /// Raises the <see cref="Interrupt"/> event on the event thread with the given IRQ flags. Called by the
        /// <see cref="PioEventListener"/> when a native PIO IRQ is delivered. Not intended for direct use by application code.
        /// </summary>
        /// <param name="flags">The IRQ flags.</param>
        internal void OnInterruptInternal(PioInterruptFlags flags)
        {
            PioInterruptEventHandler callbacks = _interruptCallbacks;
            callbacks?.Invoke(this, flags);
        }

        /// <summary>
        /// Raised when a state machine on this block asserts a PIO IRQ flag (0..3), or one is forced
        /// from the CPU with <see cref="ForceIrq"/>. The handler runs on the event thread with no CPU
        /// polling. Subscribing arms the interrupt; the last unsubscription disarms it. The flag is
        /// cleared natively before the event is delivered.
        /// </summary>
        public event PioInterruptEventHandler Interrupt
        {
            add
            {
                lock (_irqLock)
                {
                    bool isFirstSubscriber = _interruptCallbacks == null;
                    _interruptCallbacks += value;

                    if (isFirstSubscriber)
                    {
                        NativeSetIrqEnabled(_index, true);
                    }
                }
            }

            remove
            {
                lock (_irqLock)
                {
                    _interruptCallbacks -= value;

                    if (_interruptCallbacks == null)
                    {
                        NativeSetIrqEnabled(_index, false);
                    }
                }
            }
        }

        #region Native interop (implemented in nf-interpreter)

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int NativeAddProgram(int block, ushort[] instructions, int length, int origin);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void NativeRemoveProgram(int block, int length, int offset);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int NativeClaimUnusedSm(int block, bool required);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void NativeInitGpio(int block, int pin);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void NativeForceIrq(int block, int irq);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void NativeClearIrq(int block, int irq);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void NativeSetIrqEnabled(int block, bool enabled);

        #endregion
    }
}
