using System;
using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal sealed class FftMultiplier : IMultiplier
{
    private const uint Mod = 998244353;
    private const uint PrimitiveRoot = 3;

    private const int BlockBits = 10;
    private const uint BlockMask = (1u << BlockBits) - 1;

    private const uint HalfBase = 1u << 16;
    private const uint MaskFirst16 = HalfBase - 1;

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null) throw new ArgumentNullException(nameof(a));
        if (b is null) throw new ArgumentNullException(nameof(b));

        var aDigits = a.GetDigits();
        var bDigits = b.GetDigits();

        if ((aDigits.Length == 1 && aDigits[0] == 0) || (bDigits.Length == 1 && bDigits[0] == 0))
            return new BetterBigInteger(new uint[] { 0 }, false);

        uint[] aBlocks = ToBlocks(aDigits);
        uint[] bBlocks = ToBlocks(bDigits);

        int resultLen = aBlocks.Length + bBlocks.Length - 1;
        int n = 1;
        while (n < resultLen) n <<= 1;

        Array.Resize(ref aBlocks, n);
        Array.Resize(ref bBlocks, n);

        Fft(aBlocks, invert: false);
        Fft(bBlocks, invert: false);

        for (int i = 0; i < n; i++)
        {
            aBlocks[i] = MultiplyMod(aBlocks[i], bBlocks[i]);
        }

        Fft(aBlocks, invert: true);

        uint[] resultDigits = ReconstructResult(aBlocks, resultLen);

        bool isNegative = a.IsNegative != b.IsNegative;
        return new BetterBigInteger(resultDigits, isNegative);
    }

    private static uint[] ToBlocks(ReadOnlySpan<uint> digits)
    {
        int totalBits = digits.Length * 32;
        int blockCount = (totalBits + BlockBits - 1) / BlockBits;
        uint[] blocks = new uint[blockCount];

        for (int i = 0; i < blockCount; i++)
        {
            int startBit = i * BlockBits;
            int wordIndex = startBit / 32;
            int bitOffset = startBit % 32;

            uint value = 0;
            if (wordIndex < digits.Length)
            {
                value = digits[wordIndex] >> bitOffset;
                if (bitOffset > 0 && wordIndex + 1 < digits.Length)
                {
                    value |= digits[wordIndex + 1] << (32 - bitOffset);
                }
            }
            blocks[i] = value & BlockMask;
        }
        return TrimTrailingZeros(blocks);
    }

    private static uint[] ReconstructResult(uint[] blocks, int expectedLen)
    {
        uint[] result = new uint[] { 0 };

        for (int i = 0; i < expectedLen; i++)
        {
            if (blocks[i] == 0) continue;

            uint[] blockAsNumber = CreateShiftedBlock(blocks[i], i * BlockBits);
            
            result = BetterBigInteger.AddMagnitudes(result, blockAsNumber);
        }

        return result;
    }

    private static uint[] CreateShiftedBlock(uint value, int shift)
    {
        if (value == 0) return new uint[] { 0 };

        int wordShift = shift / 32;
        int bitShift = shift % 32;

        int wordCount = wordShift + 1 + (bitShift > 0 ? 1 : 0);
        uint[] result = new uint[wordCount];

        if (bitShift == 0)
        {
            result[wordShift] = value;
        }
        else
        {
            result[wordShift] = value << bitShift;
            if (wordShift + 1 < result.Length)
            {
                result[wordShift + 1] = value >> (32 - bitShift);
            }
        }

        return BetterBigInteger.TrimMagnitude(result);
    }

    private static void Fft(uint[] a, bool invert)
    {
        int n = a.Length;

        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) > 0; bit >>= 1)
            {
                j ^= bit;
            }
            j ^= bit;

            if (i < j)
            {
                (a[i], a[j]) = (a[j], a[i]);
            }
        }

        for (int len = 2; len <= n; len <<= 1)
        {
            uint wLen = Power(PrimitiveRoot, (Mod - 1) / (uint)len);
            if (invert)
            {
                wLen = InverseMod(wLen);
            }

            for (int i = 0; i < n; i += len)
            {
                uint w = 1;
                for (int j = 0; j < len / 2; j++)
                {
                    uint u = a[i + j];
                    uint v = MultiplyMod(a[i + j + len / 2], w);

                    a[i + j] = AddMod(u, v);
                    a[i + j + len / 2] = SubtractMod(u, v);

                    w = MultiplyMod(w, wLen);
                }
            }
        }

        if (invert)
        {
            uint nInv = InverseMod((uint)n);
            for (int i = 0; i < n; i++)
            {
                a[i] = MultiplyMod(a[i], nInv);
            }
        }
    }

    private static uint AddMod(uint a, uint b)
    {
        uint res = a + b;
        return res >= Mod ? res - Mod : res;
    }

    private static uint SubtractMod(uint a, uint b)
    {
        return a >= b ? a - b : a + (Mod - b);
    }

    private static uint MultiplyMod(uint a, uint b)
    {
        MulUintHalves(a, b, out uint low, out uint high);
        
        ulong fullProduct = ((ulong)high << 32) | low;
        return (uint)(fullProduct % Mod);
    }

    private static void MulUintHalves(uint a, uint b, out uint low, out uint high)
    {
        uint aLow = a & MaskFirst16, aHigh = a >> 16;
        uint bLow = b & MaskFirst16, bHigh = b >> 16;

        uint mul1 = aLow * bLow;
        uint mul2 = aLow * bHigh;
        uint mul3 = aHigh * bLow;
        uint mul4 = aHigh * bHigh;

        uint mul1Low = mul1 & MaskFirst16, mul1High = mul1 >> 16;
        uint mul2Low = mul2 & MaskFirst16, mul2High = mul2 >> 16;
        uint mul3Low = mul3 & MaskFirst16, mul3High = mul3 >> 16;
        uint mul4Low = mul4 & MaskFirst16, mul4High = mul4 >> 16;

        uint middleSum = mul1High + mul2Low + mul3Low;
        uint middleLow = middleSum & MaskFirst16;
        uint middleAcc = middleSum >> 16;

        low = (middleLow << 16) | mul1Low;
        high = mul4 + mul2High + mul3High + middleAcc;  
    }

    private static uint Power(uint baseVal, uint exp)
    {
        uint res = 1;
        baseVal %= Mod;
        while (exp > 0)
        {
            if ((exp & 1) == 1) res = MultiplyMod(res, baseVal);
            baseVal = MultiplyMod(baseVal, baseVal);
            exp >>= 1;
        }
        return res;
    }

    private static uint InverseMod(uint a)
    {
        return Power(a, Mod - 2);
    }


    private static uint[] TrimTrailingZeros(uint[] arr)
    {
        int len = arr.Length;
        while (len > 1 && arr[len - 1] == 0) len--;
        if (len == arr.Length) return arr;
        uint[] res = new uint[len];
        Array.Copy(arr, res, len);
        return res;
    }
}
