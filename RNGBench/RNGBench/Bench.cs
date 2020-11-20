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
//| Method |          Job |       Jit | Platform |      Mean |     Error |    StdDev |
//|------- |------------- |---------- |--------- |----------:|----------:|----------:|
//| Tangle | LegacyJitX64 | LegacyJit |      X64 |  1.525 ns | 0.0577 ns | 0.0750 ns |
//|    Sys | LegacyJitX64 | LegacyJit |      X64 | 22.630 ns | 0.1115 ns | 0.1043 ns |
//| Tangle | LegacyJitX86 | LegacyJit |      X86 |  6.099 ns | 0.0848 ns | 0.0752 ns |
//|    Sys | LegacyJitX86 | LegacyJit |      X86 | 24.828 ns | 0.1522 ns | 0.1424 ns |
//| Tangle |    RyuJitX64 |    RyuJit |      X64 |  1.433 ns | 0.0113 ns | 0.0105 ns |
//|    Sys |    RyuJitX64 |    RyuJit |      X64 | 25.056 ns | 0.2796 ns | 0.2615 ns |
//
//BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
//Intel Core i7-10750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
//.NET Core SDK=5.0.100
//  [Host]    : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
//  RyuJitX64 : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
//
//Job=RyuJitX64  Jit=RyuJit  Platform=X64
//
//| Method |       Mean |     Error |    StdDev |
//|------- |-----------:|----------:|----------:|
//| Tangle |  0.8173 ns | 0.0138 ns | 0.0108 ns |
//|    Sys | 23.5320 ns | 0.1362 ns | 0.1063 ns |


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
        private readonly Random sys = new Random(11111111);
        //private readonly AESRNG aes = new AESRNG();

        public RNGComparison()
        {
        }

        [Benchmark]
        public ulong Tangle() => tangle.NextULong();

        [Benchmark]
        public ulong Sys() => (ulong)sys.Next() ^ (ulong)sys.Next() << 31 ^ (ulong)sys.Next() << 62;

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