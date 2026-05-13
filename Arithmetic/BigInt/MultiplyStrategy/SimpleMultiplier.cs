using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    private static readonly uint halfBase = 1u << 16;
    private static readonly uint maskFirst16 = halfBase - 1;
    private static readonly int halfDigitBits = 16;
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b) 
    {
        if (a is null) throw new ArgumentNullException("a can't be null");
        if (b is null) throw new ArgumentNullException("b can't be null");

        uint[] resultDigits = MultiplyDigits(a.GetDigits().ToArray(), b.GetDigits().ToArray());

        bool isNegative = a.IsNegative != b.IsNegative;

        return new BetterBigInteger(resultDigits, isNegative);
    }

    private uint[] MultiplyDigits(uint[] a, uint[] b) 
    {
        if (a.Length == 0 || b.Length == 0) return new uint[] { 0 };

        uint[] result = new uint[a.Length + b.Length];

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] == 0) continue;

            for (int j = 0; j < b.Length; j++)
            {
                if (b[j] == 0) continue;

                MulUint(a[i], b[j], out uint low, out uint high);

                uint carry = 0;
                result[i + j] = AddWithCarry(result[i + j], low, ref carry);

                int pos = i + j + 1;
                if (pos < result.Length)
                {
                    result[pos] = AddWithCarry(result[pos], high, ref carry);
                    pos++;
                }

                while (carry != 0 && pos < result.Length)
                {
                    result[pos] = AddWithCarry(result[pos], 0, ref carry);
                    pos++;
                }
            }
        }

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
        high = mul4 + mul2High + mul3High + middleAcc;  
    }

    private static uint AddWithCarry(uint x, uint y, ref uint carry)
    {
        uint xLow = x & maskFirst16, xHigh = x >> halfDigitBits;
        uint yLow = y & maskFirst16, yHigh = y >> halfDigitBits;

        uint sumL = xLow + yLow + carry;
        uint resL = sumL & maskFirst16;
        uint carryFromLow = sumL >> halfDigitBits;

        uint sumH = xHigh + yHigh + carryFromLow;
        uint resH = sumH & maskFirst16;
        carry = sumH >> halfDigitBits;

        return (resH << halfDigitBits) | resL;
    }
}