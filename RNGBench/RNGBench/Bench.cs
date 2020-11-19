using System;
using System.Runtime.Intrinsics.X86;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Runtime.Intrinsics;
/// <summary>
/// | Method |      Mean |     Error |    StdDev |
/// |------- |----------:|----------:|----------:|
/// | Tangle | 0.9996 ns | 0.0362 ns | 0.0320 ns |
/// | AESRNG | 1.9211 ns | 0.0689 ns | 0.0896 ns |
/// </summary>
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
    public class AESRNG : Random
    {
        public Vector128<ulong> State { get; private set; }
        public Vector128<ulong> Increment { get; private set; }
        private Vector256<ulong> Buffer;
        private int Index;
        public AESRNG()
        {
            if (!(Aes.IsSupported && Avx.IsSupported)) throw new PlatformNotSupportedException("AES and AVX X86 Intrinsics are not supported by this machine.");
            State = Vector128.Create(1111111111UL, 1UL);
            Increment = Vector128.Create(0xF9F87D4DUL, 0x9E3779B9UL);
            Buffer = new Vector256<ulong>();
            Index = 0;
        }
        public AESRNG(ulong seedA, ulong seedB)
        {
            if (!(Aes.IsSupported && Avx.IsSupported)) throw new PlatformNotSupportedException("AES and AVX X86 Intrinsics are not supported by this machine.");
            State = Vector128.Create(seedA, seedB);
            Increment = Vector128.Create(0xF9F87D4DUL, 0x9E3779B9UL);
            Buffer = new Vector256<ulong>();
            Index = 0;
        }
        public ulong NextULong()
        {
            if (Index++ == 0)
            {
                State = Sse2.Add(State, Increment);
                Vector128<byte> early = Aes.Encrypt(State.AsByte(), Increment.AsByte());
                Vector128<byte> mid1 = Aes.Encrypt(early, Increment.AsByte());
                Vector128<byte> mid2 = Aes.Decrypt(early, Increment.AsByte());
                Buffer = Avx.InsertVector128(mid2.ToVector256Unsafe(), mid1, 1).AsUInt64();
            }
            return Buffer.GetElement(Index &= 3);
        }
    }
    public class RNGComparison
    {
        private readonly TangleRNG tangle = new TangleRNG();
        private readonly AESRNG aes = new AESRNG();

        public RNGComparison()
        {
        }

        [Benchmark]
        public ulong Tangle() => tangle.NextULong();

        [Benchmark]
        public ulong AESRNG() => aes.NextULong();
    }

    public class Bench
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run(typeof(Bench).Assembly);
        }
    }
}