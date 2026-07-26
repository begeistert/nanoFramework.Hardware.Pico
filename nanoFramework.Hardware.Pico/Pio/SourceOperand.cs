//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// Source operand. The legal subset depends on the instruction (validated at emit time).
    /// </summary>
    public enum SourceOperand
    {
        /// <summary>
        /// The pins mapped to the instruction's pin group.
        /// </summary>
        Pins = 0,
        /// <summary>
        /// The scratch register X.
        /// </summary>
        RegisterX = 1,
        /// <summary>
        /// The scratch register Y.
        /// </summary>
        RegisterY = 2,
        /// <summary>
        /// All zeroes.
        /// </summary>
        Null = 3,
        /// <summary>
        /// The status value (all-ones or all-zeroes per the configured status source).
        /// </summary>
        Status = 5,
        /// <summary>
        /// The input shift register (ISR).
        /// </summary>
        InputShiftRegister = 6,
        /// <summary>
        /// The output shift register (OSR).
        /// </summary>
        OutputShiftRegister = 7,
    }
}
