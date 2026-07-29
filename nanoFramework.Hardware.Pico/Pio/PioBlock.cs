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
        [MethodImpl(MethodImplOptions.InternalCall)]
#pragma warning disable S4200 // OK to call native methods directly in nanoFramework
        public extern uint AddProgram(PioProgram program);

        /// <summary>
        /// Removes a previously added program (maps to <c>pio_remove_program</c>).
        /// </summary>
        /// <param name="program">The program previously returned by <see cref="AddProgram"/>.</param>
        /// <param name="offset">The load offset the program occupies.</param>
        /// <exception cref="ArgumentNullException"><paramref name="program"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> plus the program length exceeds the 32-word instruction memory.</exception>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern void RemoveProgram(PioProgram program, uint offset);
#pragma warning restore S4200

        /// <summary>
        /// Claims a state machine on this block and returns it. With no argument it takes whichever one
        /// is free (<c>pio_claim_unused_sm</c>); with an explicit index it takes exactly that one
        /// (<c>pio_sm_claim</c>) and fails if somebody else already holds it. Disposing the returned
        /// instance releases the claim.
        /// </summary>
        /// <param name="stateMachine">The state machine to claim, or <see cref="PioStateMachineIndex.Any"/> for the first free one.</param>
        /// <returns>The newly claimed state machine.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="stateMachine"/> is not one of the defined values.</exception>
        /// <exception cref="InvalidOperationException">The requested state machine is already claimed, or none is free.</exception>
        public PioStateMachine ClaimStateMachine(PioStateMachineIndex stateMachine = PioStateMachineIndex.Any)
        {
            return new PioStateMachine(this, NativeClaimSm((int)stateMachine), true);
        }

        /// <summary>
        /// Routes a GPIO to this PIO block so a state machine can drive/read it (maps to
        /// <c>pio_gpio_init</c>: sets the pad's function select to PIO0/PIO1). Required before a
        /// pin mapped via OUT/SET/side-set/IN actually reaches the physical pad.
        /// </summary>
        /// <param name="pin">The GPIO to route.</param>
        /// <param name="pull">The pad pull to leave on the pin; an open-drain bus needs <see cref="PioPinPull.Up"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pin"/> is less than 0 or greater than <see cref="Pio.MaxPin"/>, or <paramref name="pull"/> is not a defined value.</exception>
        [MethodImpl(MethodImplOptions.InternalCall)]
#pragma warning disable S4200 
        public extern void InitGpio(int pin, PioPinPull pull = PioPinPull.None);
#pragma warning restore S4200

        /// <summary>
        /// Routes <paramref name="count"/> consecutive GPIOs from <paramref name="basePin"/> to this block.
        /// </summary>
        /// <param name="basePin">The first GPIO to route.</param>
        /// <param name="count">The number of consecutive GPIOs to route.</param>
        /// <exception cref="ArgumentOutOfRangeException">The span falls outside 0..47.</exception>
        public void InitGpioRange(int basePin, int count)
        {
            // Validate the whole span first, so a bad range can't leave the block partly routed.
            // The bound comes from the firmware: a literal would be wrong on one device or the
            // other, and a span that passes here only to be rejected halfway through the loop is
            // exactly the partial routing this guard exists to prevent.
            int lastPin = Pio.MaxPin;

            if (basePin < 0 || count < 0 || count > lastPin + 1 || basePin > lastPin + 1 - count)
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
        [MethodImpl(MethodImplOptions.InternalCall)]
#pragma warning disable S4200 
        public extern void ForceIrq(int irq);

        /// <summary>
        /// Clears PIO IRQ flag <paramref name="irq"/> (0..7), e.g. one raised by a state machine's <c>irq</c>.
        /// </summary>
        /// <param name="irq">The IRQ flag to clear.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="irq"/> is less than 0 or greater than 7.</exception>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern void ClearIrq(int irq);
#pragma warning restore S4200

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
                        NativeSetIrqEnabled(true);
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
                        NativeSetIrqEnabled(false);
                    }
                }
            }
        }

        #region Private Native interop (implemented in nf-interpreter)

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int NativeClaimSm(int stateMachine);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeSetIrqEnabled(bool enabled);

        #endregion
    }
}
