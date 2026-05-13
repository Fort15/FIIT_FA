using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class KaratsubaMultiplier : IMultiplier
{
    private static readonly uint halfBase = 1u << 16;
    private static readonly uint maskFirst16 = halfBase - 1;
    private static readonly int halfDigitBits = 16;
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null) throw new ArgumentNullException("a can't be null");
        if (b is null) throw new ArgumentNullException("b can't be null");

        uint[] resultDigits = KaratsubaMultiply(a.GetDigits(), b.GetDigits());

        bool isNegative = a.IsNegative != b.IsNegative;

        return new BetterBigInteger(resultDigits, isNegative);
    }

    private static uint[] KaratsubaMultiply(ReadOnlySpan<uint> A, ReadOnlySpan<uint> B)
    {
        A = TrimHighZeros(A);
        B = TrimHighZeros(B);

        if (A.Length == 0 || B.Length == 0) return new uint[] { 0 };

        if (A.Length == 1 && B.Length == 1)
        {
            MulUint(A[0], B[0], out uint low, out uint high);
            return high == 0 ? new uint[] { low } : new uint[] { low, high };
        }

        int n = Math.Max(A.Length, B.Length);
        int half = n / 2;

        ReadOnlySpan<uint> A0 = A[..Math.Min(half, A.Length)];
        ReadOnlySpan<uint> A1 = A[Math.Min(half, A.Length)..];

        ReadOnlySpan<uint> B0 = B[..Math.Min(half, B.Length)];
        ReadOnlySpan<uint> B1 = B[Math.Min(half, B.Length)..];

        uint[] A0B0 = KaratsubaMultiply(A0, B0);
        uint[] A1B1 = KaratsubaMultiply(A1, B1);

        uint[] A0PlusA1 = BetterBigInteger.AddMagnitudes(A0, A1);
        uint[] B0PlusB1 = BetterBigInteger.AddMagnitudes(B0, B1);
        uint[] A0PlusA1_B0PlusB1 = KaratsubaMultiply(A0PlusA1, B0PlusB1);

        uint[] middle = BetterBigInteger.SubtractMagnitudes(A0PlusA1_B0PlusB1, A0B0);
        middle = BetterBigInteger.SubtractMagnitudes(middle, A1B1);

        uint[] shiftedMiddle = ShiftWords(middle, half);
        uint[] shiftedA1B1 = ShiftWords(A1B1, 2 * half);
        
        uint[] result = BetterBigInteger.AddMagnitudes(A0B0, shiftedMiddle);
        result = BetterBigInteger.AddMagnitudes(result, shiftedA1B1);

        return BetterBigInteger.TrimMagnitude(result);
    }

    private static void MulUint(uint a, uint b, out uint low, out uint high)
    {
        uint aLow = a & maskFirst16, aHigh = a >> halfDigitBits;
        uint bLow = b & maskFirst16, bHigh = b >> halfDigitBits;

        uint mul1 = aLow * bLow;
        uint mul2 = aLow * bHigh;
        uint mul3 = aHigh * bLow;
        uint mul4 = aHigh * bHigh;

        uint mul1Low = mul1 & maskFirst16, mul1High = mul1 >> halfDigitBits;
        uint mul2Low = mul2 & maskFirst16, mul2High = mul2 >> halfDigitBits;
        uint mul3Low = mul3 & maskFirst16, mul3High = mul3 >> halfDigitBits;
        uint mul4Low = mul4 & maskFirst16, mul4High = mul4 >> halfDigitBits;

        uint middleSum = mul1High + mul2Low + mul3Low;
        uint middleLow = middleSum & maskFirst16;
        uint middleAcc = middleSum >> halfDigitBits;

        low = (middleLow << halfDigitBits) | mul1Low;

        uint highLowPart = mul2High + mul3High + mul4Low + middleAcc;

        uint highLowResult = highLowPart & maskFirst16;
        uint highLowCarry = highLowPart >> halfDigitBits;

        uint highHighPart = mul4High + highLowCarry;
        
        high = (highHighPart << halfDigitBits) | highLowResult;
    }

    private static ReadOnlySpan<uint> TrimHighZeros(ReadOnlySpan<uint> digits)
    {
        int len = digits.Length;

        while (len > 0 && digits[len - 1] == 0) len--;

        return digits[..len];
    }

    private static uint[] ShiftWords(ReadOnlySpan<uint> digits, int wordShift)
    {
        digits = TrimHighZeros(digits);

        if (digits.Length == 0) return new uint[] { 0 };

        uint[] result = new uint[digits.Length + wordShift];

        for (int i = 0; i < digits.Length; i++) result[i + wordShift] = digits[i];

        return result;
    }
}