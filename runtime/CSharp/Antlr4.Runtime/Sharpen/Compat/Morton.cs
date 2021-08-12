using System;
using System.Runtime.CompilerServices;

namespace Antlr4.Runtime.Sharpen.Compat
{
    public static class Morton
    {
        private static ReadOnlySpan<nuint> InterleaveMask => new nuint[] { 0x55555555, 0x33333333, 0x0F0F0F0F, 0x00FF00FF };
        private static ReadOnlySpan<int> InterleaveShift => new[] { 1, 2, 4, 8 };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static ushort Interleave16(byte x, byte y)
        {
            return (ushort)(((x * 0x0101010101010101ul & 0x8040201008040201ul) * 0x0102040810204081ul >> 49) & 0x5555
                            | ((y * 0x0101010101010101ul & 0x8040201008040201ul) * 0x0102040810204081ul >> 48) & 0xAAAA);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static uint Interleave32(ushort x, ushort y)
        {
            nuint x1 = x;
            x1 = (x1 | (x1 << InterleaveShift[3])) & InterleaveMask[3];
            x1 = (x1 | (x1 << InterleaveShift[2])) & InterleaveMask[2];
            x1 = (x1 | (x1 << InterleaveShift[1])) & InterleaveMask[1];
            x1 = (x1 | (x1 << InterleaveShift[0])) & InterleaveMask[0];
            
            nuint y1 = y;
            y1 = (y1 | (y1 << InterleaveShift[3])) & InterleaveMask[3];
            y1 = (y1 | (y1 << InterleaveShift[2])) & InterleaveMask[2];
            y1 = (y1 | (y1 << InterleaveShift[1])) & InterleaveMask[1];
            y1 = (y1 | (y1 << InterleaveShift[0])) & InterleaveMask[0];

            return (uint)(x1 | (y1 << 1));
        }
    }
}