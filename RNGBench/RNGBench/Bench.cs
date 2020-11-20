using System;
//using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
//using System.Runtime.Intrinsics;

// | Method |       Mean |     Error |    StdDev |
// |------- |-----------:|----------:|----------:|
// | Tangle |  0.8584 ns | 0.0455 ns | 0.0524 ns |
// |    Sys | 23.1491 ns | 0.1607 ns | 0.1424 ns |
// | AESRNG |  1.8859 ns | 0.0673 ns | 0.0801 ns |


//BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
//Intel Core i7-10750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
//  [Host]       : .NET Framework 4.8 (4.8.4250.0), X64 RyuJIT
//  LegacyJitX64 : .NET Framework 4.8 (4.8.4250.0), X64 LegacyJIT
//  LegacyJitX86 : .NET Framework 4.8 (4.8.4250.0), X86 LegacyJIT
//  RyuJitX64    : .NET Framework 4.8 (4.8.4250.0), X64 RyuJIT
//
//
//|    Method |          Job |       Jit | Platform |      Mean |     Error |    StdDev |    Median |
//|---------- |------------- |---------- |--------- |----------:|----------:|----------:|----------:|
//|    Tangle | LegacyJitX64 | LegacyJit |      X64 |  1.538 ns | 0.0597 ns | 0.0710 ns |  1.556 ns |
//|  SplitMix | LegacyJitX64 | LegacyJit |      X64 |  1.770 ns | 0.0621 ns | 0.0910 ns |  1.817 ns |
//|       Sys | LegacyJitX64 | LegacyJit |      X64 | 21.948 ns | 0.4643 ns | 0.8133 ns | 22.499 ns |
//| SysSimple | LegacyJitX64 | LegacyJit |      X64 |  7.615 ns | 0.1790 ns | 0.2839 ns |  7.419 ns |
//|    Tangle | LegacyJitX86 | LegacyJit |      X86 |  5.803 ns | 0.3301 ns | 0.5605 ns |  5.956 ns |
//|  SplitMix | LegacyJitX86 | LegacyJit |      X86 |  7.813 ns | 0.3542 ns | 0.5719 ns |  8.091 ns |
//|       Sys | LegacyJitX86 | LegacyJit |      X86 | 24.531 ns | 0.6973 ns | 1.3266 ns | 24.345 ns |
//| SysSimple | LegacyJitX86 | LegacyJit |      X86 |  6.645 ns | 0.3329 ns | 0.5470 ns |  6.874 ns |
//|    Tangle |    RyuJitX64 |    RyuJit |      X64 |  1.324 ns | 0.0565 ns | 0.0828 ns |  1.367 ns |
//|  SplitMix |    RyuJitX64 |    RyuJit |      X64 |  1.572 ns | 0.0625 ns | 0.1027 ns |  1.631 ns |
//|       Sys |    RyuJitX64 |    RyuJit |      X64 | 23.915 ns | 0.5064 ns | 0.8178 ns | 24.455 ns |
//| SysSimple |    RyuJitX64 |    RyuJit |      X64 |  8.127 ns | 0.1912 ns | 0.3298 ns |  8.123 ns |
//
//BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
//Intel Core i7-10750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
//.NET Core SDK=5.0.100
//  [Host]    : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
//  RyuJitX64 : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
//
//Job=RyuJitX64  Jit=RyuJit  Platform=X64
//
//|    Method |      Mean |     Error |    StdDev |    Median |
//|---------- |----------:|----------:|----------:|----------:|
//|    Tangle |  1.178 ns | 0.0527 ns | 0.0821 ns |  1.210 ns |
//|  SplitMix |  1.464 ns | 0.0588 ns | 0.1187 ns |  1.542 ns |
//|       Sys | 24.806 ns | 0.5233 ns | 0.9569 ns | 25.358 ns |
//| SysSimple |  8.912 ns | 0.2059 ns | 0.3765 ns |  9.054 ns |

namespace RNGBench
{
    public class TangleRNG : Random
    {
        private ulong stateB;
        public ulong StateA { get; set; }
        public ulong StateB { get => stateB; set => stateB = (value | 1UL); }
        public TangleRNG()
        {
            StateA = 1111111111UL;
            StateB = 1UL;
        }
        public TangleRNG(ulong seedA, ulong seedB)
        {
            StateA = seedA;
            StateB = seedB;
        }
        public ulong NextULong()
        {
            ulong s = (StateA += 0xC6BC279692B5C323UL);
            ulong z = (s ^ s >> 31) * (StateB += 0x9E3779B97F4A7C16UL);
            return z ^ z >> 26 ^ z >> 6;
        }
    }
    public class GoatRNG : Random
    {
        public ulong StateA { get; set; }
        public ulong StateB { get; set; }
        public GoatRNG()
        {
            StateA = 1111111111UL;
            StateB = 1UL;
        }
        public GoatRNG(ulong seedA, ulong seedB)
        {
            StateA = seedA;
            StateB = seedB;
        }
        public ulong NextULong()
        {
            ulong s = (StateA += 0xD1342543DE82EF95L);
            ulong t = (s ^= s >> 31 ^ s >> 23) < 0x46BC279692B5C323UL ? StateB : (StateB += 0xB1E131D6149D9795L);
            s *= ((t ^ t << 9) | 1L);
            return s ^ s >> 25;
        }
    }
    public class SplitMixRNG : Random
    {
        public ulong State { get; set; }
        public SplitMixRNG()
        {
            State = 1111111111UL;
        }
        public SplitMixRNG(ulong seed)
        {
            State = seed;
        }
        public ulong NextULong()
        {
            ulong z = (State += 0x9E3779B97F4A7C15UL);
            z = (z ^ z >> 30) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ z >> 27) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
    //public class AESRNG : Random
    //{
    //    public Vector128<ulong> State { get; private set; }
    //    public Vector128<ulong> Increment { get; private set; }
    //    private Vector256<ulong> Buffer;
    //    private int Index;
    //    public AESRNG()
    //    {
    //        if (!(Aes.IsSupported && Avx.IsSupported)) throw new PlatformNotSupportedException("AES and AVX X86 Intrinsics are not supported by this machine.");
    //        State = Vector128.Create(1111111111UL, 1UL);
    //        Increment = Vector128.Create(0xF9F87D4DUL, 0x9E3779B9UL);
    //        Buffer = new Vector256<ulong>();
    //        Index = 0;
    //    }
    //    public AESRNG(ulong seedA, ulong seedB)
    //    {
    //        if (!(Aes.IsSupported && Avx.IsSupported)) throw new PlatformNotSupportedException("AES and AVX X86 Intrinsics are not supported by this machine.");
    //        State = Vector128.Create(seedA, seedB);
    //        Increment = Vector128.Create(0xF9F87D4DUL, 0x9E3779B9UL);
    //        Buffer = new Vector256<ulong>();
    //        Index = 0;
    //    }
    //    public ulong NextULong()
    //    {
    //        if (Index++ == 0)
    //        {
    //            State = Sse2.Add(State, Increment);
    //            Vector128<byte> early = Aes.Encrypt(State.AsByte(), Increment.AsByte());
    //            Vector128<byte> mid1 = Aes.Encrypt(early, Increment.AsByte());
    //            Vector128<byte> mid2 = Aes.Decrypt(early, Increment.AsByte());
    //            Buffer = Avx.InsertVector128(mid2.ToVector256Unsafe(), mid1, 1).AsUInt64();
    //        }
    //        return Buffer.GetElement(Index &= 3);
    //    }
    //}
    [LegacyJitX86Job, LegacyJitX64Job, RyuJitX64Job]
    public class RNGComparison
    {
        private readonly TangleRNG tangle = new TangleRNG();
        //private readonly GoatRNG goat = new GoatRNG();
        private readonly SplitMixRNG splitMix = new SplitMixRNG();
        private readonly Random sys = new Random(11111111);
        //private readonly AESRNG aes = new AESRNG();

        public RNGComparison()
        {
        }

        [Benchmark]
        public ulong Tangle() => tangle.NextULong();

        //[Benchmark]
        //public ulong Goat() => goat.NextULong();

        [Benchmark]
        public ulong SplitMix() => splitMix.NextULong();

        [Benchmark]
        public ulong Sys() => (ulong)sys.Next() ^ (ulong)sys.Next() << 31 ^ (ulong)sys.Next() << 62;

        [Benchmark]
        public ulong SysSimple() => (ulong)sys.Next();

        //[Benchmark]
        //public ulong AESRNG() => aes.NextULong();
    }

    public class Bench
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run(typeof(Bench).Assembly);
        }
    }
}