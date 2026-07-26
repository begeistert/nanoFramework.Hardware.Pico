//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// FIFO selected by the EXECCTRL status source for the <c>mov ..., status</c> instruction.
    /// </summary>
    public enum PioMovStatusSel
    {
        /// <summary>
        /// Status is all-ones when the TX FIFO level is below N.
        /// </summary>
        TxLevel = 0,
        /// <summary>
        /// Status is all-ones when the RX FIFO level is below N.
        /// </summary>
        RxLevel = 1,
    }
}
