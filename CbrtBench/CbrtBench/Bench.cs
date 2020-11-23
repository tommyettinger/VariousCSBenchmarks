﻿using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

//BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
//Intel Core i7-10750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
//.NET Core SDK=5.0.100
//  [Host]    : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
//  RyuJitX64 : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
//
//Job=RyuJitX64  Jit=RyuJit  Platform=X64
//
//|   Method |      Mean |     Error |    StdDev |
//|--------- |----------:|----------:|----------:|
//|     Goto | 29.902 ns | 0.1806 ns | 0.1690 ns |
//| ThreeTwo | 41.859 ns | 0.1386 ns | 0.1229 ns |
//|  SixFour | 15.263 ns | 0.0400 ns | 0.0334 ns |
//|        F |  4.200 ns | 0.0889 ns | 0.0832 ns |
//|      Sys | 32.854 ns | 0.1881 ns | 0.1469 ns |

namespace CbrtBench
{
    [StructLayout(LayoutKind.Explicit)]
    public struct fu_32   // float <==> uint
    {
        [FieldOffset(0)]
        public float f;
        [FieldOffset(0)]
        public uint u;
    }

    [RyuJitX64Job]
    public class CbrtComparison
    {
    private static float CbrtF(float fx)
    {
        fu_32 fu32 = new fu_32();
        fu32.f = fx;
        uint uy = fu32.u >> 2;
        uy += uy >> 2;
        uy += uy >> 4;
        uy += uy >> 8;
        uy += 0x2a5137a0;
        fu32.u = uy;
        float fy = fu32.f;
        fy = 0.33333333f * (fx / (fy * fy) + 2.0f * fy);
        return 0.33333333f * (fx / (fy * fy) + 2.0f * fy);
    }
    private static uint Cbrt64(ulong x)
    {
        if (x >= 18446724184312856125) return 2642245;
        float fx = x;
        fu_32 fu32 = new fu_32();
        fu32.f = fx;
        uint uy = fu32.u >> 2;
        uy += uy >> 2;
        uy += uy >> 4;
        uy += uy >> 8;
        uy += 0x2a5137a0;
        fu32.u = uy;
        float fy = fu32.f;
        fy = 0.33333333f * (fx / (fy * fy) + 2.0f * fy);

        // uint y1 = (uint)
        //    (0.33333333f * (fx / (fy * fy) + 2.0f * fy));

        int y0 = (int)                                      // 2013-09-22
            (0.33333333f * (fx / (fy * fy) + 2.0f * fy));   // 5 ns 
        uint y1 = (uint)y0;                                 // faster

        ulong y2, y3;
        if (y1 >= 2642245)
        {
            y1 = 2642245;
            y2 = 6981458640025;
            y3 = 18446724184312856125;
        }
        else
        {
            y2 = (ulong)y1 * y1;
            y3 = y2 * y1;
        }
        if (y3 > x)
        {
            y1 -= 1;
            y2 -= 2 * y1 + 1;
            y3 -= 3 * y2 + 3 * y1 + 1;
            while (y3 > x)
            {
                y1 -= 1;
                y2 -= 2 * y1 + 1;
                y3 -= 3 * y2 + 3 * y1 + 1;
            }
            return y1;
        }
        do
        {
            y3 += 3 * y2 + 3 * y1 + 1;
            y2 += 2 * y1 + 1;
            y1 += 1;
        }
        while (y3 <= x);
        return y1 - 1;
    }

    private static uint Cbrt32(uint x)
    {
            uint y = 0, z = 0, b;
        int s = x < 1u << 24 ? x < 1u << 12 ? x < 1u << 06 ? x < 1u << 03 ? 00 : 03 :
                                                             x < 1u << 09 ? 06 : 09 :
                                              x < 1u << 18 ? x < 1u << 15 ? 12 : 15 :
                                                             x < 1u << 21 ? 18 : 21 :
                               x >= 1u << 30 ? 30 : x < 1u << 27 ? 24 : 27;
        do
        {
            y *= 2;
            z *= 4;
            b = 3 * y + 3 * z + 1 << s;
            if (x >= b)
            {
                x -= b;
                z += 2 * y + 1;
                y += 1;
            }
            s -= 3;
        }
        while (s >= 0);
        return y;
    }

    private static uint CbrtGoto(uint x)
        {
            uint y = 4u, z = 16u, b = 61u << 21;
            if (x < 1u << 24)
                if (x < 1u << 12)
                    if (x < 1u << 06)
                        if (x < 1u << 03)
                            return x == 0u ? 0u : 1u;
                        else
                            return x < 27u ? 2u : 3u;
                    else
                        if (x < 1u << 09) goto L8; else goto L7;
                else
                    if (x < 1u << 18)
                    if (x < 1u << 15) goto L6; else goto L5;
                else
                        if (x < 1u << 21) goto L4; else goto L3;
            else
                if (x < 1u << 30)
                if (x < 1u << 27) goto L2;
                else
                {
                    if (x >= 27u << 24) { x -= 27u << 24; z = 36u; y = 6u; b = 127u << 21; }
                    else { x -= 1u << 27; }
                }
            else
            {
                if (x >= 27u << 27) { x -= 27u << 27; z = 144u; y = 12u; b = 469u << 21; }
                else
                {
                    if (x >= 125u << 24) { x -= 125u << 24; z = 100u; y = 10u; b = 331u << 21; }
                    else { x -= 1u << 30; z = 64u; y = 8u; b = 217u << 21; }
                }
            }
            goto M1;

        L2: if (x >= 27u << 21) { x -= 27u << 21; z = 36u; y = 6u; } else { x -= 1u << 24; }
            goto M2;
        L3: if (x >= 27u << 18) { x -= 27u << 18; z = 36u; y = 6u; } else { x -= 1u << 21; }
            goto M3;
        L4: if (x >= 27u << 15) { x -= 27u << 15; z = 36u; y = 6u; } else { x -= 1u << 18; }
            goto M4;
        L5: if (x >= 27u << 12) { x -= 27u << 12; z = 36u; y = 6u; } else { x -= 1u << 15; }
            goto M5;
        L6: if (x >= 27u << 09) { x -= 27u << 09; z = 36u; y = 6u; } else { x -= 1u << 12; }
            goto M6;
        L7: if (x >= 27u << 06) { x -= 27u << 06; z = 36u; y = 6u; } else { x -= 1u << 09; }
            goto M7;
        L8: if (x >= 27u << 03) { x -= 27u << 03; z = 36u; y = 6u; } else { x -= 1u << 06; }
            goto M8;

        M1: if (x >= b) { x -= b; z += y * 2 + 1u; y += 1u; }
            y *= 2; z *= 4;
        M2: b = (y + z) * 3 + 1u << 18; if (x >= b) { x -= b; z += y * 2 + 1u; y += 1u; }
            y *= 2; z *= 4;
        M3: b = (y + z) * 3 + 1u << 15; if (x >= b) { x -= b; z += y * 2 + 1u; y += 1u; }
            y *= 2; z *= 4;
        M4: b = (y + z) * 3 + 1u << 12; if (x >= b) { x -= b; z += y * 2 + 1u; y += 1u; }
            y *= 2; z *= 4;
        M5: b = (y + z) * 3 + 1u << 09; if (x >= b) { x -= b; z += y * 2 + 1u; y += 1u; }
            y *= 2; z *= 4;
        M6: b = (y + z) * 3 + 1u << 06; if (x >= b) { x -= b; z += y * 2 + 1u; y += 1u; }
            y *= 2; z *= 4;
        M7: b = (y + z) * 3 + 1u << 03; if (x >= b) { x -= b; z += y * 2 + 1u; y += 1u; }
            y *= 2; z *= 4;
        M8: return x <= (y + z) * 3 ? y : y + 1u;
        }
        public CbrtComparison()
        {
        }

        private uint gState = 0x1000000u;
        private uint sState = 0x1000000u;
        private uint aState = 0x1000000u;
        private uint fState = 0x1000000u;
        private ulong bState = 0x1000000u;
        [Benchmark]
        public uint Goto() => CbrtGoto(gState += 0x9E3779B9u);

        [Benchmark]
        public uint ThreeTwo() => Cbrt32(aState += 0x9E3779B9u);

        [Benchmark]
        public uint SixFour() => Cbrt64(bState += 0x9E3779B97F4A7C15UL);

        [Benchmark]
        public float F() => CbrtF((fState += 0x9E3779B9u) & 0xFFFFFFu);

        [Benchmark]
        public uint Sys() => (uint)MathF.Cbrt(sState += 0x9E3779B9u);
    }

    public class Bench
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run(typeof(Bench).Assembly);
        }
    }
}