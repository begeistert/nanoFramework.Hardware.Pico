//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// A handle to the just-emitted instruction, used to attach a delay and/or a
    /// side-set value fluently: <c>asm.Set(DestinationOperand.Pins, 1).Side(1).Delay(7);</c>.
    /// Validation of the values happens at <see cref="PioAssembler.Build"/>.
    /// </summary>
    /// <remarks>
    /// This is a value type carrying only the owning assembler and the instruction's
    /// <em>index</em> — no per-instruction object is allocated. <see cref="Delay"/> and
    /// <see cref="Side"/> call back into the assembler to mutate that slot.
    /// </remarks>
    public struct PioInstructionRef
    {
        private readonly PioAssembler _owner;
        private readonly int _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="PioInstructionRef"/> class.
        /// </summary>
        /// <param name="owner">The owning assembler.</param>
        /// <param name="index">The index of the instruction.</param>
        internal PioInstructionRef(PioAssembler owner, int index)
        {
            _owner = owner;
            _index = index;
        }

        /// <summary>
        /// Sets the post-instruction delay in cycles (range checked at Build).
        /// </summary>
        /// <param name="cycles">The number of cycles to delay.</param>
        /// <returns>The updated instruction reference.</returns>
        public PioInstructionRef Delay(int cycles)
        {
            _owner.ApplyDelay(_index, cycles);
            return this;
        }

        /// <summary>
        /// Asserts a side-set value alongside this instruction.
        /// </summary>
        /// <param name="value">The side-set value.</param>
        /// <returns>The updated instruction reference.</returns>
        public PioInstructionRef Side(int value)
        {
            _owner.ApplySide(_index, value);
            return this;
        }
    }
}
