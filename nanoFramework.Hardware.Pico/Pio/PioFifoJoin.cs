//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// FIFO join mode (SHIFTCTRL FJOIN). Values match the SDK's <c>pio_fifo_join</c>. The
    /// PUT/GET modes are RP2350 (PIO v1) only and turn the RX FIFO into a random-access
    /// register file addressed by the indexed RX-FIFO MOV instructions.
    /// </summary>
    public enum PioFifoJoin
    {
        /// <summary>
        /// Separate TX (depth 4) and RX (depth 4) FIFOs.
        /// </summary>
        None = 0,
        /// <summary>
        /// TX FIFO depth 8, RX disabled.
        /// </summary>
        Tx = 1,
        /// <summary>
        /// RX FIFO depth 8, TX disabled.
        /// </summary>
        Rx = 2,
        /// <summary>
        /// (v1) TX depth 4; RX storage is the processor-writable "get" register file.
        /// </summary>
        TxGet = 4,
        /// <summary>
        /// (v1) TX depth 4; RX storage is the processor-readable "put" register file.
        /// </summary>
        TxPut = 8,
        /// <summary>
        /// (v1) TX depth 4; RX storage is the SM-only "put"/"get" register file.
        /// </summary>
        PutGet = 12,
    }
}
