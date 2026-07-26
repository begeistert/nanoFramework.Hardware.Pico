//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
//

namespace nanoFramework.Hardware.Pico.Pio
{
    /// <summary>
    /// Mov operation applied to the source before writing the destination.
    /// </summary>
    public enum PioMovOp
    {
        /// <summary>
        /// No operation — the source is copied unchanged.
        /// </summary>
        None = 0,
        /// <summary>
        /// Bitwise NOT ("!" / "~").
        /// </summary>
        Invert = 1,
        /// <summary>
        /// Bit reverse ("::").
        /// </summary>
        BitReverse = 2,
    }
}
