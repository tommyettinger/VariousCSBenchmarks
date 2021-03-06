﻿using System;
using System.Collections.Generic;
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
//|     Goto | 30.178 ns | 0.1946 ns | 0.1725 ns |
//| ThreeTwo | 44.060 ns | 0.3705 ns | 0.3284 ns |
//|  SixFour | 16.203 ns | 0.3377 ns | 0.3614 ns |
//|        F |  4.970 ns | 0.0913 ns | 0.0712 ns |
//|      Sys | 33.440 ns | 0.3438 ns | 0.2870 ns |

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
        public static float CbrtF(float fx)
        {
            fu_32 fu32 = new fu_32
            {
                f = fx
            };
            uint sign = fu32.u & 0x80000000u;
            fu32.u &= 0x7FFFFFFFu;
            uint uy = (fu32.u >> 2);
            uy += uy >> 2;
            uy += uy >> 4;
            fu32.u = uy + (uy >> 8) + 0x2A5137A0u | sign;
            float fy = fu32.f;
            fy =   0.3333f * (fx / (fy * fy) + 2.0f * fy);
            return 0.3333333f * (fx / (fy * fy) + 2.0f * fy);
        }
        public static uint Cbrt64(ulong x)
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

    public static uint Cbrt32(uint x)
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

    public static uint CbrtGoto(uint x)
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

    public class Optimizer
    {
        private static float CbrtF(float fx, uint edit)
        {
            fu_32 fu32 = new fu_32
            {
                f = fx
            };
            uint sign = fu32.u & 0x80000000u;
            fu32.u &= 0x7FFFFFFFu;
            uint uy = (fu32.u >> 2);
            uy += uy >> 2;
            uy += uy >> 4;
            fu32.u = uy + (uy >> 8) + (0x2A5137A0u ^ edit) | sign; //0x2A517D3Cu //0x2A5137A0u
            float fy = fu32.f;
            fy = 0.33333330f * (fx / (fy * fy) + 2.0f * fy);
            return 0.33333333f * (fx / (fy * fy) + 2.0f * fy);
            //fy = 0.33333330f * (fx / (fy * fy) + 2.0f * fy);

            //fu32.f = (fx / (fu32.f * fu32.f) + 2.0f * fu32.f) * 0.33333333f;
            //fu32.f = (fx / (fu32.f * fu32.f) + 2.0f * fu32.f) * 0.33333333f;
            //fu32.u -= fu32.u >> 29;
            //return fu32.f;
            //fy = 0.33333333f * (fx / (fy * fy) + 2.0f * fy);
            //return (fx / (fy * fy) + 2.0f * fy) * 0.333333333f;
        }

        public static void Main(string[] args)
        {
            for (int i = 0, c = 0; i <= 256; i++, c = i * i * i)
            {
                Console.WriteLine($"CbrtF({c}) == {CbrtF(c, 0u):F9} (hex {BitConverter.SingleToInt32Bits(CbrtF(c, 0u)):X8})");
            }
            SortedList<int, uint> sort = new SortedList<int, uint>(65536);
            Dictionary<uint, int> toAbs = new Dictionary<uint, int>(65536);
            Dictionary<uint, int> toRel = new Dictionary<uint, int>(65536);
            DateTime start = DateTime.Now;
            for (uint e = 0; e < 65536u; e += 4697u)
            {
                int absoluteF = 0, relativeF = 0;
                for (uint u = 0u; u <= 1u << 19; u++)//8388607u
                {
                    uint accurate = (uint)(Math.Cbrt(u));
                    uint approx = (uint)CbrtF(u, e); // 14091
                    int error = (int)approx - (int)accurate;
                    relativeF += error;
                    absoluteF += Math.Abs(error);
                }
                if(absoluteF == 0)
                {
                    Console.WriteLine($"{e} !!!");
                    return;
                }
                sort[absoluteF] = e;
                toRel[e] = relativeF;
                toAbs[e] = absoluteF;
                if ((e & 63u) == 0u) Console.WriteLine($"{e >> 6}/1024 in {DateTime.Now - start}");
            }
            Console.WriteLine($"First: {sort.Keys[0]} to {sort.Values[0]} (relative {toRel[sort.Values[0]]})");
            Console.WriteLine($"Last : {sort.Keys[sort.Count - 1]} to {sort.Values[sort.Count - 1]} (relative {toRel[sort.Values[sort.Count - 1]]})");
            Console.WriteLine($"0: absolute {toAbs[0]}, relative {toRel[0]}");
        }
    }

    public class AccuracyTest
    {
        public static void Main(string[] args)
        {
            Random random = new Random(123456789);
            double sumError = 0.0, relativeError = 0.0;
            for (int i = 0; i < 1000000; i++)
            {
                float r = (float)(random.NextDouble() * random.Next(-512, 512));
                float accurate = MathF.Cbrt(r);
                float approx = CbrtComparison.CbrtF(r);
                float error = approx - accurate;
                relativeError += error;
                sumError += Math.Abs(error);
            }
            Console.WriteLine($"CbrtF: Sum Error: {sumError} (averaged, {sumError / 0x100000}), Rel Error: {relativeError} (averaged, {relativeError / 0x100000})");

            int absolute64 = 0, relative64 = 0;
            int absoluteGT = 0, relativeGT = 0;
            int absoluteF  = 0, relativeF  = 0;
            for (uint u = 0u; u <= 8388607u; u++)
            {
                uint accurate = (uint)(Math.Cbrt(u));
                uint approx = CbrtComparison.Cbrt64(u);
                int error = (int)approx - (int)accurate;
                relative64 += error;
                absolute64 += Math.Abs(error);
                approx = CbrtComparison.CbrtGoto(u);
                error = (int)approx - (int)accurate;
                relativeGT += error;
                absoluteGT += Math.Abs(error);
                approx = (uint)CbrtComparison.CbrtF(u);
                error = (int)approx - (int)accurate;
                relativeF += error;
                absoluteF += Math.Abs(error);

            }
            Console.WriteLine($"Cbrt64: Sum Error: {absolute64} (averaged, {absolute64 / 8388607.0}), Rel Error: {relative64} (averaged, {relative64 / 8388607.0})");
            Console.WriteLine($"CbrtGT: Sum Error: {absoluteGT} (averaged, {absoluteGT / 8388607.0}), Rel Error: {relativeGT} (averaged, {relativeGT / 8388607.0})");
            Console.WriteLine($"CbrtF : Sum Error: {absoluteF } (averaged, {absoluteF  / 8388607.0}), Rel Error: {relativeF } (averaged, {relativeF  / 8388607.0})");
        }
    }
}