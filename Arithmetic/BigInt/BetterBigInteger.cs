using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private int _signBit;
    
    private uint _smallValue; // Если число маленькое, храним его прямо в этом поле, а _data == null.
    private uint[]? _data;
    
    public bool IsNegative => _signBit == 1;
    
    /// От массива цифр (little endian)
    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        if (digits == null) 
        {
            throw new ArgumentNullException("digits can't be null");
        }
        InitFromDigits(digits, isNegative);
    }
    
    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false) 
            : this(digits?.ToArray() ?? throw new ArgumentNullException("digits can't be null"), isNegative)
    {
    }
    
    public BetterBigInteger(string value, int radix)
    {
        if (radix < 2 || radix > 36)
        {
            throw new ArgumentOutOfRangeException("radix must be in [2..36]");
        }

        if (value == null) 
        {
            throw new ArgumentNullException("value can't be null");
        }
        
        if (value.Length == 0) 
        {
            throw new ArgumentException("value can't be empty");
        }
        
        int index = 0;
        bool isNegative = false;
        if (value[0] == '-' || value[0] == '+')
        {
            isNegative = value[index] == '-';
            index++;
        }

        if (index == value.Length) 
        {
            throw new ArgumentException("value can't be only a sign");
        }

        List<uint> words = new List<uint>();
        for (; index < value.Length; index++)
        {
            int digit = GetDigitValue(value[index]);

            if (digit < 0 || digit >= radix)
            {
                throw new ArgumentException($"Invalid symbol '{value[index]}' for radix {radix}");
            }

            ulong carry = (uint)digit;
            for (int j = 0; j < words.Count; j++)
            {
                ulong current = (ulong)words[j] * (uint)radix + carry;
                words[j] = (uint)current;
                carry = current >> 32;
            }
            if (carry != 0)
            {
                words.Add((uint)carry);
            }
        }

        uint[] digitsArray = words.ToArray();
        InitFromDigits(digitsArray, isNegative);
    }    
    
    public ReadOnlySpan<uint> GetDigits()
    {
        return _data ?? [_smallValue];
    }
    
    public int CompareTo(IBigInteger? other) 
    {
        if (other == null) return 1;

        if (IsNegative != other.IsNegative)
        {
            return IsNegative ? -1 : 1;
        }

        var digits1 = GetDigits();
        var digits2 = other.GetDigits();

        int result = 0;

        if (digits1.Length != digits2.Length)
        {
            result = digits1.Length < digits2.Length ? -1 : 1;
        }
        else 
        {
            for (int i = digits1.Length - 1; i >= 0; i--)
            {
                if (digits1[i] != digits2[i])
                {
                    result = digits1[i] < digits2[i] ? -1 : 1;
                    break;
                }
            }
        }

        return IsNegative ? -result : result;
    }

    public bool Equals(IBigInteger? other) 
    {
        return CompareTo(other) == 0;
    }

    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);
    public override int GetHashCode() 
    {
        HashCode hash = new HashCode();

        hash.Add(_signBit);

        if (_data == null) 
        {
            hash.Add(_smallValue);
        }
        else
        {
            for (int i = 0; i < _data.Length; i++)
            {
                hash.Add(_data[i]);
            }
        }
        
        return hash.ToHashCode();
    }
    
    
    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b) 
    {
        if (a is null) throw new ArgumentNullException("a can't be null");
        if (b is null) throw new ArgumentNullException("b can't be null");
        
        var aDigits = a.GetDigits();
        var bDigits = b.GetDigits();

        if (a.IsNegative == b.IsNegative)
        {
            uint[] sum = AddAbs(aDigits, bDigits);
            return new BetterBigInteger(sum, a.IsNegative);
        }

        int absCompare = CompareAbs(aDigits, bDigits);

        if (absCompare == 0)
        {
            return new BetterBigInteger(new uint[] { 0 });
        }

        if (absCompare > 0)
        {
            uint[] diff = SubtractAbs(aDigits, bDigits);
            return new BetterBigInteger(diff, a.IsNegative);
        }
        else
        {
            uint[] diff = SubtractAbs(bDigits, aDigits);
            return new BetterBigInteger(diff, b.IsNegative);
        }
    }

    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b) 
    {
        if (a is null) throw new ArgumentNullException("a can't be null");
        if (b is null) throw new ArgumentNullException("b can't be null");
        
        return a + (-b);
    }
    public static BetterBigInteger operator -(BetterBigInteger a) 
    {
        if (a is null) throw new ArgumentNullException("a can't be null");

        var digits = a.GetDigits().ToArray();
        bool isZero = digits.Length == 1 && digits[0] == 0;
        return new BetterBigInteger(digits, isZero ? false : !a.IsNegative);
    }

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null) throw new ArgumentNullException("a can't be null");
        if (b is null) throw new ArgumentNullException("b can't be null");

        if (IsZero(b.GetDigits())) throw new DivideByZeroException("Division by zero");

        var (quotient, remainder) = DivRemAbs(a.GetDigits(), b.GetDigits());
        bool isNegative = a.IsNegative != b.IsNegative;

        return new BetterBigInteger(quotient, isNegative);
    }
    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null) throw new ArgumentNullException("a can't be null");
        if (b is null) throw new ArgumentNullException("b can't be null");

        if (IsZero(b.GetDigits())) throw new DivideByZeroException("Division by zero");

        var (quotient, remainder) = DivRemAbs(a.GetDigits(), b.GetDigits());

        return new BetterBigInteger(remainder, a.IsNegative);
    }
    
    
    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b) 
    {
        if (a is null) throw new ArgumentNullException("a can't be null");
        if (b is null) throw new ArgumentNullException("b can't be null");

        return new SimpleMultiplier().Multiply(a, b);
    }
    
    public static BetterBigInteger operator ~(BetterBigInteger a)
    {
        if (a is null) throw new ArgumentNullException("a can't be null");

        int length = a.GetDigits().Length + 1;
        uint[] value = ToTwosComplement(a.GetDigits(), a.IsNegative, length);

        for (int i = 0; i < value.Length; i++)
        {
            value[i] = ~value[i];
        }

        return FromTwosComplement(value);
    }

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null) throw new ArgumentNullException("a can't be null");
        if (b is null) throw new ArgumentNullException("b can't be null");

        int length = Math.Max(a.GetDigits().Length, b.GetDigits().Length) + 1;

        uint[] left = ToTwosComplement(a.GetDigits(), a.IsNegative, length);
        uint[] right = ToTwosComplement(b.GetDigits(), b.IsNegative, length);

        uint[] result = new uint[length];

        for (int i = 0; i < length; i++)
        {
            result[i] = left[i] & right[i];
        }
        return FromTwosComplement(result);
    }
    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null) throw new ArgumentNullException("a can't be null");
        if (b is null) throw new ArgumentNullException("b can't be null");

        int length = Math.Max(a.GetDigits().Length, b.GetDigits().Length) + 1;

        uint[] left = ToTwosComplement(a.GetDigits(), a.IsNegative, length);
        uint[] right = ToTwosComplement(b.GetDigits(), b.IsNegative, length);

        uint[] result = new uint[length];

        for (int i = 0; i < length; i++)
        {
            result[i] = left[i] | right[i];
        }
        return FromTwosComplement(result);
    }
    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b)
    {
        if (a is null) throw new ArgumentNullException("a can't be null");
        if (b is null) throw new ArgumentNullException("b can't be null");

        int length = Math.Max(a.GetDigits().Length, b.GetDigits().Length) + 1;

        uint[] left = ToTwosComplement(a.GetDigits(), a.IsNegative, length);
        uint[] right = ToTwosComplement(b.GetDigits(), b.IsNegative, length);

        uint[] result = new uint[length];

        for (int i = 0; i < length; i++)
        {
            result[i] = left[i] ^ right[i];
        }
        return FromTwosComplement(result);
    }

    public static BetterBigInteger operator <<(BetterBigInteger a, int shift)
    {
        if (a is null) throw new ArgumentNullException("a can't be null");

        if (shift < 0) return a >> (-shift);

        uint[] result = ShiftLeftAbs(a.GetDigits(), shift);
        return new BetterBigInteger(result, a.IsNegative);
    }

    public static BetterBigInteger operator >> (BetterBigInteger a, int shift)
    {
        if (a is null) throw new ArgumentNullException("a can't be null");
        
        if (shift < 0) return a << (-shift);

        var digits = a.GetDigits();

        if (!a.IsNegative)
        {
            uint[] result = ShiftRightAbs(digits, shift);
            return new BetterBigInteger(result, false);
        }

        uint[] shifted = ShiftRightAbs(digits, shift);

        if (HasLostBitsOnRightShift(digits, shift))
        {
            shifted = AddOneAbs(shifted);
        }

        return new BetterBigInteger(shifted, true);

    }
    
    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;
    
    public override string ToString() => ToString(10);
    public string ToString(int radix)
    {
        if (radix < 2 || radix > 36)
        {
            throw new ArgumentOutOfRangeException("radix must be in [2..36]");
        }

        var digits = GetDigits().ToArray();

        if (digits.Length == 1 && digits[0] == 0)
        {
            return "0";
        }

        List<char> chars = new List<char>();
        
        while (!(digits.Length == 1 && digits[0] == 0)) 
        {
            uint remainder = GetRemainder(ref digits, (uint)radix);
            chars.Add(GetCharFromDigit(remainder));
        }

        if (IsNegative)
        {
            chars.Add('-');
        }

        chars.Reverse();

        return new string(chars.ToArray());
    }



    // helper methods

    private void InitFromDigits(uint[] digits, bool isNegative)
    {
        int len = digits.Length;

        while (len > 0 && digits[len - 1] == 0) len--;

        if (len == 0)
        {
            _signBit = 0;
            _smallValue = 0;
            _data = null;
            return;
        }

        _signBit = isNegative ? 1 : 0;

        if (len == 1)
        {
            _smallValue = digits[0];
            _data = null;
        }
        else
        {
            _smallValue = 0;
            _data = new uint[len];
            Array.Copy(digits, _data, len);
        }
    }

    private static int GetDigitValue(char c)
    {
        if (c >= '0' && c <= '9')
        {
            return c - '0';
        }
        if (c >= 'A' && c <= 'Z')
        {
            return c - 'A' + 10;
        }
        if (c >= 'a' && c <= 'z')
        {
            return c - 'a' + 10;
        }
        return -1;
    }
    
    private static uint[] AddAbs(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        int maxlength = Math.Max(a.Length, b.Length);
        uint[] result = new uint[maxlength + 1];

        ulong carry = 0;

        for (int i = 0; i < maxlength; i++)
        {
            ulong x = i < a.Length ? a[i] : 0;
            ulong y = i < b.Length ? b[i] : 0;

            ulong sum = x + y + carry;

            result[i] = (uint)sum;
            carry = sum >> 32;
        }

        if (carry != 0)
        {
            result[maxlength] = (uint)carry;
        }

        return result;
    }

    private static uint[] SubtractAbs(ReadOnlySpan<uint> bigger, ReadOnlySpan<uint> smaller)
    {
        uint[] result = new uint[bigger.Length];

        long borrow = 0;

        for (int i = 0; i < bigger.Length; i++) 
        {
            long x = bigger[i];
            long y = i < smaller.Length ? smaller[i] : 0;

            long diff = x - y - borrow;

            if (diff < 0) 
            {
                diff += (1L << 32);
                borrow = 1;
            }
            else 
            {
                borrow = 0;
            }

            result[i] = (uint)diff;
        }

        return result;
    }

    private static int CompareAbs(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        if (a.Length != b.Length)
        {
            return a.Length < b.Length ? -1 : 1;
        }

        for (int i = a.Length - 1; i >= 0; i--)
        {
            if (a[i] != b[i])
            {
                return a[i] < b[i] ? -1 : 1;
            }
        }

        return 0;
    }

    private static char GetCharFromDigit(uint digit)
    {
        if (digit < 10) return (char)('0' + digit);

        return (char)('A' + digit - 10);
    }

    private static uint GetRemainder(ref uint[] digits, uint divisor)
    {
        ulong remainder = 0;

        for (int i = digits.Length - 1; i >= 0; i--)
        {
            ulong current = (remainder << 32) | digits[i];
            digits[i] = (uint)(current / divisor);
            remainder = current % divisor;
        }

        int len = digits.Length;

        while (len > 1 && digits[len - 1] == 0)
            len--;

        if (len != digits.Length)
            Array.Resize(ref digits, len);

        return (uint)remainder;
    }

    private static uint[] ShiftLeftAbs(ReadOnlySpan<uint> digits, int shift)
    {
        if (shift == 0) return digits.ToArray();

        if (digits.Length == 1 && digits[0] == 0) return new uint[] { 0 };

        int wordShift = shift / 32;
        int bitShift = shift % 32;

        uint[] result = new uint[digits.Length + wordShift + 1];

        ulong carry = 0;
        for (int i = 0; i < digits.Length; i++)
        {
            ulong current = ((ulong)digits[i] << bitShift) | carry;
            result[i + wordShift] = (uint)current;
            carry = current >> 32;
        }

        if (carry != 0)
        {
            result[digits.Length + wordShift] = (uint)carry;
        }

        return result;
    }

    private static uint[] ShiftRightAbs(ReadOnlySpan<uint> digits, int shift)
    {
        if (shift == 0) return digits.ToArray();

        if (digits.Length == 1 && digits[0] == 0) return new uint[] { 0 };

        int wordShift = shift / 32;
        int bitShift = shift % 32;

        if (wordShift >= digits.Length) return new uint[] { 0 };

        int newLength = digits.Length - wordShift;
        uint[] result = new uint[newLength];

        if (bitShift == 0)
        {
            for (int i = 0; i < newLength; i++)
            {
                result[i] = digits[i + wordShift];
            }

            return result;
        }

        uint BitsFromHigherWord = 0;

        for (int i = newLength - 1; i >= 0; i--)
        {
            uint current = digits[i + wordShift];
            result[i] = (current >> bitShift) | BitsFromHigherWord;
            BitsFromHigherWord = current << (32 - bitShift);
        }

        return result;
    }

    private static bool HasLostBitsOnRightShift(ReadOnlySpan<uint> digits, int shift)
    {
        if (shift <= 0) return false;

        int wordShift = shift / 32;
        int bitShift = shift % 32;

        int fullWordsToCheck = Math.Min(wordShift, digits.Length);

        for (int i = 0; i < fullWordsToCheck; i++)
        {
            if (digits[i] != 0) return true;
        }

        if (wordShift >= digits.Length)
            return false;

        if (bitShift == 0)
            return false;

        uint mask = (uint)((1UL << bitShift) - 1);

        return (digits[wordShift] & mask) != 0;
    }

    private static uint[] AddOneAbs(uint[] digits)
    {
        uint[] result = new uint[digits.Length + 1];
        Array.Copy(digits, result, digits.Length);

        ulong carry = 1;
        for (int i = 0; i < result.Length; i++)
        {
            ulong current = (ulong)result[i] + carry;
            result[i] = (uint)current;
            carry = current >> 32;

            if (carry == 0)
                break;
        }

        return result;
    }

    private static uint[] ToTwosComplement(ReadOnlySpan<uint> digits, bool isNegative, int length)
    {
        uint[] result = new uint[length];

        for (int i = 0; i < digits.Length; i++)
        {
            result[i] = digits[i];
        }

        if (!isNegative)
        {
            return result;
        }

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = ~result[i];
        }

        AddOneFixed(result);
        return result;
    }

    private static BetterBigInteger FromTwosComplement(uint[] digits)
    {
        bool isNegative = (digits[digits.Length - 1] & 0x80000000u) != 0;

        if (!isNegative)
        {
            return new BetterBigInteger(digits, false);
        }
        
        for (int i = 0; i < digits.Length; i++)
        {
            digits[i] = ~digits[i];
        }   

        AddOneFixed(digits);

        return new BetterBigInteger(digits, true);
    }

    private static void AddOneFixed(uint[] digits)
    {
        ulong carry = 1;

        for (int i = 0; i < digits.Length; i++)
        {
            ulong current = (ulong)digits[i] + carry;
            digits[i] = (uint)current;
            carry = current >> 32;

            if (carry == 0)
                return;
        }
    }

    private static bool IsZero(ReadOnlySpan<uint> digits)
    {
        for (int i = 0; i < digits.Length; i++)
        {
            if (digits[i] != 0) return false;
        }
        return true;
    }

    private static (uint[] quotient, uint[] remainder) DivRemAbs(ReadOnlySpan<uint> dividendSpan, ReadOnlySpan<uint> divisorSpan)
    {
        uint[] dividend = TrimHighZeros(dividendSpan.ToArray());
        uint[] divisor = TrimHighZeros(divisorSpan.ToArray());

        int cmp = CompareAbs(dividend, divisor);

        if (cmp < 0)
        {
            return (new uint[] { 0 }, dividend);
        }

        if (cmp == 0)
        {
            return (new uint[] { 1 }, new uint[] { 0 });
        }

        int bitLength = GetBitLength(dividend);

        uint[] quotient = new uint[(bitLength + 31) / 32];
        uint[] remainder = new uint[] { 0 };

        for (int bit = bitLength - 1; bit >= 0; bit--)
        {
            remainder = TrimHighZeros(ShiftLeftAbs(remainder, 1));

            if (GetBit(dividend, bit))
            {
                remainder = TrimHighZeros(AddAbs(remainder, new uint[] { 1 }));
            }

            if (CompareAbs(remainder, divisor) >= 0)
            {
                remainder = TrimHighZeros(SubtractAbs(remainder, divisor));
                SetBit(quotient, bit);
            }
        }

        quotient = TrimHighZeros(quotient);
        remainder = TrimHighZeros(remainder);

        return (quotient, remainder);
    }

    private static uint[] TrimHighZeros(uint[] digits)
    {
        int len = digits.Length;

        while (len > 1 && digits[len - 1] == 0) len--;

        if (len == digits.Length) return digits;

        uint[] result = new uint[len];
        Array.Copy(digits, result, len);

        return result;
    }

    private static int GetBitLength(ReadOnlySpan<uint> digits)
    {
        if (IsZero(digits)) return 0;

        int lastIndex = digits.Length - 1;
        uint top = digits[lastIndex];

        int bitlength = lastIndex * 32;

        while (top != 0)
        {
            bitlength++;
            top >>= 1;
        }

        return bitlength;
    }

    private static bool GetBit(ReadOnlySpan<uint> digits, int bitIndex)
    {
        int wordIndex = bitIndex / 32;
        int bitInWord = bitIndex % 32;

        if (wordIndex >= digits.Length) return false;

        return (digits[wordIndex] & (1u << bitInWord)) != 0;
    }

    private static void SetBit(uint[] digits, int bitIndex)
    {
        int wordIndex = bitIndex / 32;
        int bitInWord = bitIndex % 32;

        if (wordIndex >= digits.Length) return;

        digits[wordIndex] |= (1u << bitInWord);
    }


    // helper обёртки

    public static uint[] AddMagnitudes(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        return TrimHighZeros(AddAbs(a, b));
    }

    public static uint[] SubtractMagnitudes(ReadOnlySpan<uint> bigger, ReadOnlySpan<uint> smaller)
    {
        return TrimHighZeros(SubtractAbs(bigger, smaller));
    }

    public static uint[] TrimMagnitude(uint[] digits)
    {
        return TrimHighZeros(digits);
    }
}