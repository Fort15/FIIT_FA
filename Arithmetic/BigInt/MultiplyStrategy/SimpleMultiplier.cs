using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
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
        int resultLength = a.Length + b.Length;
        uint[] result = new uint[resultLength];

        for (int i = 0; i < a.Length; i++) 
        {
            ulong carry = 0;
            for (int j = 0; j < b.Length; j++) 
            {
                ulong current = (ulong)a[i] * b[j] + result[i + j] + carry;
                result[i + j] = (uint)current;
                carry = current >> 32;
            }

            if (carry != 0) 
            {
                result[i + b.Length] += (uint)carry;
            }
        }

        return result;
    }
}