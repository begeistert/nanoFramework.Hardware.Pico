//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// One of a PIO block's four state machines. Initialize it from a
    /// <see cref="PioStateMachineConfig"/>, enable it, and exchange words through the
    /// TX/RX FIFOs. Disposing releases the SM claim.
    /// </summary>
    public sealed class PioStateMachine : IDisposable
    {
        private readonly PioBlock _block;
        private readonly int _sm;
        private readonly bool _owned;
        private bool _disposed;
#pragma warning disable 0414
        // this field is used in native so it must be kept here despite "not being used"
        private bool _enabled;
#pragma warning restore 0414

        /// <summary>
        /// Initializes a new instance of the <see cref="PioStateMachine"/> class.
        /// </summary>
        /// <param name="block">The owning PIO block.</param>
        /// <param name="sm">The state-machine index (0..3).</param>
        /// <param name="owned"><see langword="true"/> when this wrapper claimed the SM and must release it on dispose.</param>
        internal PioStateMachine(PioBlock block, int sm, bool owned)
        {
            _block = block;
            _sm = sm;
            _owned = owned;
        }

        /// <summary>
        /// State machine index (0..3).
        /// </summary>
        public int Index
        {
            get
            {
                return _sm;
            }
        }

        /// <summary>
        /// Configures and resets the state machine to start executing at
        /// <paramref name="offset"/> (maps to <c>pio_sm_init</c>). The configuration is
        /// flattened to a blob and rebuilt into a <c>pio_sm_config</c> natively.
        /// </summary>
        /// <param name="offset">The instruction-memory offset (0..31) to start at.</param>
        /// <param name="config">The state-machine configuration.</param>
        /// <exception cref="ArgumentNullException"><paramref name="config"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is greater than 31.</exception>
        /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
        public void Init(uint offset, PioStateMachineConfig config)
        {
            NativeInit((int)offset, config.ToBlob());
        }

        /// <summary>
        /// Enables or disables the state machine (maps to <c>pio_sm_set_enabled</c>). The getter
        /// reflects the last value set through this API.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
        public extern bool Enabled
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
            [MethodImpl(MethodImplOptions.InternalCall)]
            set;
        }

        /// <summary>
        /// Gets a value indicating whether the TX FIFO cannot accept another word.
        /// </summary>
        /// <value><see langword="true"/> if the TX FIFO is full; otherwise, <see langword="false"/>.</value>
        /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
        public extern bool IsTxFull
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /// <summary>
        /// Gets a value indicating whether the RX FIFO has no words to read.
        /// </summary>
        /// <value><see langword="true"/> if the RX FIFO is empty; otherwise, <see langword="false"/>.</value>
        /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
        public extern bool IsRxEmpty
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /// <summary>
        /// Reads the number of words currently queued in the TX FIFO (depth depends on FIFO join).
        /// </summary>
        /// <value>The number of words in the TX FIFO.</value>
        /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
        public extern uint TxLevel
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /// <summary>
        /// Reads the number of words currently queued in the RX FIFO (depth depends on FIFO join).
        /// </summary>
        /// <value>The number of words in the RX FIFO.</value>
        /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
        public extern uint RxLevel
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /// <summary>
        /// Reads the state machine's current program counter (instruction-memory offset 0..31).
        /// </summary>
        /// <value>The current program counter.</value>
        /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
        public extern uint ProgramCounter
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /// <summary>
        /// Changes the clock divider (1.0 .. 65536.0) live and restarts the divider phase.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="value"/> is outside the 1.0 .. 65536.0 range.</exception>
        /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
        public extern float ClockDivisor
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            set;
        }

        /// <summary>
        /// Pushes a word into the TX FIFO, yielding to other threads until there is room.
        /// </summary>
        /// <param name="value">The word to push into the TX FIFO.</param>
        /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
        public void Put(uint value)
        {
            while (IsTxFull)
            {
                System.Threading.Thread.Sleep(0);
                if (_disposed)
                {
                    throw new ObjectDisposedException(null);
                }
            }

            NativePutBlocking(value);
        }

        /// <summary>
        /// Pops a word from the RX FIFO, yielding to other threads until one is available.
        /// </summary>
        /// <returns>The word popped from the RX FIFO.</returns>
        /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
        public uint Get()
        {
            while (IsRxEmpty)
            {
                System.Threading.Thread.Sleep(0);
                if (_disposed)
                {
                    throw new ObjectDisposedException(null);
                }
            }

            return NativeGetBlocking();
        }

        /// <summary>
        /// Attempts to push a word into the TX FIFO without blocking. Returns <c>false</c> (and writes
        /// nothing) when the FIFO is full, so callers can poll or do other work instead of stalling the
        /// CLR thread the way <see cref="Put"/> does. Safe against the FIFO state changing under it: the
        /// check and the write are a single uninterrupted managed step on the cooperative CLR.
        /// </summary>
        /// <param name="value">The word to push into the TX FIFO.</param>
        /// <returns><see langword="true"/> if the word was pushed; <see langword="false"/> if the FIFO was full.</returns>
        /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
        public bool TryPut(uint value)
        {
            if (IsTxFull)
            {
                return false;
            }

            NativePutBlocking(value);
            return true;
        }

        /// <summary>
        /// Attempts to pop a word from the RX FIFO without blocking. Returns <c>false</c> (and sets
        /// <paramref name="value"/> to 0) when the FIFO is empty, instead of blocking like <see cref="Get"/>.
        /// </summary>
        /// <param name="value">The word popped from the RX FIFO.</param>
        /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
        public bool TryGet(out uint value)
        {
            if (IsRxEmpty)
            {
                value = 0;
                return false;
            }

            value = NativeGetBlocking();
            return true;
        }

        /// <summary>
        /// Clears this state machine's TX and RX FIFOs (maps to <c>pio_sm_clear_fifos</c>).
        /// </summary>
        /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
        [MethodImpl(MethodImplOptions.InternalCall)]
#pragma warning disable S4200 
        public extern void ClearFifos();

        /// <summary>
        /// Drains any words left in the TX FIFO (maps to <c>pio_sm_drain_tx_fifo</c>): useful before
        /// reconfiguring or restarting so a stale half-streamed frame is not emitted.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern void DrainTxFifo();

        /// <summary>
        /// Restarts the state machine's internal state — ISR/OSR, shift counters, delay/clock phase
        /// (maps to <c>pio_sm_restart</c>). Does not touch the FIFOs or the program counter.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern void Restart();

        /// <summary>
        /// Restarts this state machine's clock divider so its phase realigns with other SMs started at
        /// the same time (maps to <c>pio_sm_clkdiv_restart</c>).
        /// </summary>
        /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern void ClockDivRestart();

        /// <summary>
        /// Immediately executes a single instruction on the state machine, out of band, without
        /// advancing the program counter (maps to <c>pio_sm_exec</c>). Encode the 16-bit instruction
        /// with <see cref="PioEncoder"/> (or take a word from an assembled <see cref="PioProgram"/>).
        /// </summary>
        /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern void Exec(ushort instruction);

        /// <summary>
        /// Sets the direction (output/input) of <paramref name="count"/> consecutive pins starting at
        /// <paramref name="basePin"/> for this state machine (maps to
        /// <c>pio_sm_set_consecutive_pindirs</c>). Pins an SM drives with OUT/SET/side-set must be set
        /// as outputs; combine with <see cref="PioBlock.InitGpio"/>, which routes them to the block.
        /// </summary>
        /// <param name="basePin">The first GPIO in the range.</param>
        /// <param name="count">The number of consecutive pins.</param>
        /// <param name="output"><see langword="true"/> for output, <see langword="false"/> for input.</param>
        /// <exception cref="ArgumentOutOfRangeException">The pin range is outside the chip's GPIOs.</exception>
        /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern void SetConsecutivePinDirs(int basePin, int count, bool output);
#pragma warning restore S4200

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the SM claim if the instance is collected without an explicit Dispose.
        /// </summary>
        ~PioStateMachine()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _enabled = false;

            // only a wrapper that claimed the SM may stop or release it; a fixed-index view must not touch someone else's
            if (_owned)
            {
                Enabled = false;
                NativeUnclaim();
            }
        }

        /// <summary>
        /// Reads <paramref name="count"/> words from the RX FIFO into <paramref name="buffer"/> using a
        /// DMA channel paced by this state machine's RX request. Blocks the calling thread but yields the
        /// CLR while the transfer runs, so other threads keep running (no busy-wait, no FIFO overflow).
        /// </summary>
        /// <param name="buffer">Destination array.</param>
        /// <param name="offset">Index in <paramref name="buffer"/> at which to start writing.</param>
        /// <param name="count">Number of 32-bit words to read.</param>
        /// <param name="timeoutMs">Maximum time to wait, in milliseconds.</param>
        /// <returns>The number of words actually transferred (less than <paramref name="count"/> on timeout).</returns>
        /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The range falls outside <paramref name="buffer"/>, or an argument is negative.</exception>
        /// <exception cref="InvalidOperationException">No DMA channel was free, or a transfer is already running on this state machine.</exception>
        [MethodImpl(MethodImplOptions.InternalCall)]
#pragma warning disable S4200 
        public extern int Read(uint[] buffer, int offset, int count, int timeoutMs);

        /// <summary>
        /// Writes <paramref name="count"/> words from <paramref name="buffer"/> into the TX FIFO using a
        /// DMA channel paced by this state machine's TX request. Blocks the calling thread but yields the
        /// CLR while the transfer runs, so other threads keep running (no busy-wait, no FIFO stall).
        /// </summary>
        /// <param name="buffer">Source array.</param>
        /// <param name="offset">Index in <paramref name="buffer"/> at which to start reading.</param>
        /// <param name="count">Number of 32-bit words to write.</param>
        /// <param name="timeoutMs">Maximum time to wait, in milliseconds.</param>
        /// <returns>The number of words actually transferred (less than <paramref name="count"/> on timeout).</returns>
        /// <exception cref="ObjectDisposedException">The state machine has been disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The range falls outside <paramref name="buffer"/>, or an argument is negative.</exception>
        /// <exception cref="InvalidOperationException">No DMA channel was free, or a transfer is already running on this state machine.</exception>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern int Write(uint[] buffer, int offset, int count, int timeoutMs);
#pragma warning restore S4200

        #region Native interop (implemented in nf-interpreter)

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeInit(int offset, uint[] configBlob);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativePutBlocking(uint value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern uint NativeGetBlocking();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void NativeUnclaim();

        #endregion
    }
}
