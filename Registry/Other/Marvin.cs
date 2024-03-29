﻿using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Registry.Other;

internal static class Marvin
{
    public static ulong DefaultSeed { get; } = 0x82EF4D887A4E55C5;

    /// <summary>
    ///     Convenience method to compute a Marvin hash and collapse it into a 32-bit hash.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ComputeHash32(ref byte data, int count, ulong seed)
    {
        var hash64 = ComputeHash(ref data, count, seed);
        return (int) (hash64 >> 32) ^ (int) hash64;
    }

    /// <summary>
    ///     Computes a 64-hash using the Marvin algorithm.
    /// </summary>
    public static long ComputeHash(ref byte data, int count, ulong seed)
    {
        var ucount = (uint) count;
        var p0 = (uint) seed;
        var p1 = (uint) (seed >> 32);

        var
            byteOffset =
                0; // declared as signed int so we don't have to cast everywhere (it's passed to Unsafe.Add() and used for nothing else.)

        while (ucount >= 8)
        {
            p0 += Unsafe.As<byte, uint>(ref Unsafe.Add(ref data, byteOffset));
            Block(ref p0, ref p1);

            p0 += Unsafe.As<byte, uint>(ref Unsafe.Add(ref data, byteOffset + 4));
            Block(ref p0, ref p1);

            byteOffset += 8;
            ucount -= 8;
        }

        switch (ucount)
        {
            case 4:
                p0 += Unsafe.As<byte, uint>(ref Unsafe.Add(ref data, byteOffset));
                Block(ref p0, ref p1);
                goto case 0;

            case 0:
                p0 += 0x80u;
                break;

            case 5:
                p0 += Unsafe.As<byte, uint>(ref Unsafe.Add(ref data, byteOffset));
                byteOffset += 4;
                Block(ref p0, ref p1);
                goto case 1;

            case 1:
                p0 += 0x8000u | Unsafe.Add(ref data, byteOffset);
                break;

            case 6:
                p0 += Unsafe.As<byte, uint>(ref Unsafe.Add(ref data, byteOffset));
                byteOffset += 4;
                Block(ref p0, ref p1);
                goto case 2;

            case 2:
                p0 += 0x800000u | Unsafe.As<byte, ushort>(ref Unsafe.Add(ref data, byteOffset));
                break;

            case 7:
                p0 += Unsafe.As<byte, uint>(ref Unsafe.Add(ref data, byteOffset));
                byteOffset += 4;
                Block(ref p0, ref p1);
                goto case 3;

            case 3:
                p0 += 0x80000000u | ((uint) Unsafe.Add(ref data, byteOffset + 2) << 16) |
                      Unsafe.As<byte, ushort>(ref Unsafe.Add(ref data, byteOffset));
                break;

            default:
                Debug.Fail("Should not get here.");
                break;
        }

        Block(ref p0, ref p1);
        Block(ref p0, ref p1);

        return ((long) p1 << 32) | p0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Block(ref uint rp0, ref uint rp1)
    {
        var p0 = rp0;
        var p1 = rp1;

        p1 ^= p0;
        p0 = _rotl(p0, 20);

        p0 += p1;
        p1 = _rotl(p1, 9);

        p1 ^= p0;
        p0 = _rotl(p0, 27);

        p0 += p1;
        p1 = _rotl(p1, 19);

        rp0 = p0;
        rp1 = p1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint _rotl(uint value, int shift)
    {
        // This is expected to be optimized into a single rol (or ror with negated shift value) instruction
        return (value << shift) | (value >> (32 - shift));
    }
}