// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET5_0 && (!NETSTANDARD2_0 || NETSTANDARD2_0_OR_GREATER)

using System;
using System.Runtime.CompilerServices;

// Some routines inspired by the Stanford Bit Twiddling Hacks by Sean Eron Anderson:
// http://graphics.stanford.edu/~seander/bithacks.html

namespace System.Numerics
{
    /// <summary>
    ///     Utility methods for intrinsic bit-twiddling operations.
    ///     The methods use hardware intrinsics when available on the underlying platform,
    ///     otherwise they use optimized software fallbacks.
    /// </summary>
    internal static class BitOperations
    {
        /// <summary>
        ///     Rotates the specified value left by the specified number of bits.
        ///     Similar in behavior to the x86 instruction ROL.
        /// </summary>
        /// <param name="value">The value to rotate.</param>
        /// <param name="offset">
        ///     The number of bits to rotate by.
        ///     Any value outside the range [0..31] is treated as congruent mod 32.
        /// </param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining), CLSCompliant(false)]
        public static uint RotateLeft(uint value, int offset)
        {
            return (value << offset) | (value >> (32 - offset));
        }

        /// <summary>
        ///     Rotates the specified value left by the specified number of bits.
        ///     Similar in behavior to the x86 instruction ROL.
        /// </summary>
        /// <param name="value">The value to rotate.</param>
        /// <param name="offset">
        ///     The number of bits to rotate by.
        ///     Any value outside the range [0..63] is treated as congruent mod 64.
        /// </param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining), CLSCompliant(false)]
        public static ulong RotateLeft(ulong value, int offset)
        {
            return (value << offset) | (value >> (64 - offset));
        }

        /// <summary>
        ///     Rotates the specified value right by the specified number of bits.
        ///     Similar in behavior to the x86 instruction ROR.
        /// </summary>
        /// <param name="value">The value to rotate.</param>
        /// <param name="offset">
        ///     The number of bits to rotate by.
        ///     Any value outside the range [0..31] is treated as congruent mod 32.
        /// </param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining), CLSCompliant(false)]
        public static uint RotateRight(uint value, int offset)
        {
            return (value >> offset) | (value << (32 - offset));
        }

        /// <summary>
        ///     Rotates the specified value right by the specified number of bits.
        ///     Similar in behavior to the x86 instruction ROR.
        /// </summary>
        /// <param name="value">The value to rotate.</param>
        /// <param name="offset">
        ///     The number of bits to rotate by.
        ///     Any value outside the range [0..63] is treated as congruent mod 64.
        /// </param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining), CLSCompliant(false)]
        public static ulong RotateRight(ulong value, int offset)
        {
            return (value >> offset) | (value << (64 - offset));
        }
    }
}
#endif