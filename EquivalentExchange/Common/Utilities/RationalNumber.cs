using System;

namespace EquivalentExchange.Common.Utilities
{
    /// <summary>
    /// Represents an arbitrary rational number (fraction).
    /// </summary>
    public class RationalNumber : IEquatable<RationalNumber>, IComparable<RationalNumber>
    {
        public long Numerator { get; private set; }
        public long Denominator { get; private set; }

        // Constructors
        public RationalNumber(long numerator, long denominator)
        {
            if (denominator == 0)
                throw new DivideByZeroException("Denominator cannot be zero.");

            // Ensure denominator is positive
            if (denominator < 0)
            {
                numerator = -numerator;
                denominator = -denominator;
            }

            // Simplify the fraction
            Simplify(ref numerator, ref denominator);

            Numerator = numerator;
            Denominator = denominator;
        }

        public RationalNumber(long wholeNumber) : this(wholeNumber, 1) { }

        // Common values
        public static RationalNumber Zero => new RationalNumber(0, 1);
        public static RationalNumber One => new RationalNumber(1, 1);

        // Simplify the fraction using GCD
        private static void Simplify(ref long numerator, ref long denominator)
        {
            if (numerator == 0)
            {
                denominator = 1;
                return;
            }

            long gcd = GreatestCommonDivisor(Math.Abs(numerator), Math.Abs(denominator));
            numerator /= gcd;
            denominator /= gcd;
        }

        // Calculate GCD using Euclidean algorithm
        private static long GreatestCommonDivisor(long a, long b)
        {
            while (b != 0)
            {
                long temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        // Arithmetic operations
        public static RationalNumber operator +(RationalNumber a, RationalNumber b)
        {
            return new RationalNumber(
                a.Numerator * b.Denominator + b.Numerator * a.Denominator,
                a.Denominator * b.Denominator);
        }

        public static RationalNumber operator -(RationalNumber a, RationalNumber b)
        {
            return new RationalNumber(
                a.Numerator * b.Denominator - b.Numerator * a.Denominator,
                a.Denominator * b.Denominator);
        }

        public static RationalNumber operator *(RationalNumber a, RationalNumber b)
        {
            return new RationalNumber(
                a.Numerator * b.Numerator,
                a.Denominator * b.Denominator);
        }

        public static RationalNumber operator /(RationalNumber a, RationalNumber b)
        {
            if (b.Numerator == 0)
                throw new DivideByZeroException();

            return new RationalNumber(
                a.Numerator * b.Denominator,
                a.Denominator * b.Numerator);
        }

        public static RationalNumber operator -(RationalNumber a)
        {
            return new RationalNumber(-a.Numerator, a.Denominator);
        }

        // Comparison operators
        public static bool operator ==(RationalNumber a, RationalNumber b)
        {
            if (ReferenceEquals(a, null))
                return ReferenceEquals(b, null);
            return a.Equals(b);
        }

        public static bool operator !=(RationalNumber a, RationalNumber b) => !(a == b);

        public static bool operator <(RationalNumber a, RationalNumber b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator >(RationalNumber a, RationalNumber b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator <=(RationalNumber a, RationalNumber b)
        {
            return a.CompareTo(b) <= 0;
        }

        public static bool operator >=(RationalNumber a, RationalNumber b)
        {
            return a.CompareTo(b) >= 0;
        }

        // Conversion operators
        public static implicit operator RationalNumber(int value) => new RationalNumber(value, 1);
        public static implicit operator RationalNumber(long value) => new RationalNumber(value, 1);
        public static explicit operator double(RationalNumber value) => (double)value.Numerator / value.Denominator;
        public static explicit operator decimal(RationalNumber value) => (decimal)value.Numerator / value.Denominator;

        // Additional methods
        public RationalNumber Abs() => new RationalNumber(Math.Abs(Numerator), Denominator);
        public RationalNumber Reciprocal() => Numerator == 0 ? throw new DivideByZeroException() : new RationalNumber(Denominator, Numerator);
        public double ToDouble() => (double)Numerator / Denominator;
        public decimal ToDecimal() => (decimal)Numerator / Denominator;

        // Override methods
        public override string ToString()
        {
            return Denominator == 1 ? Numerator.ToString() : $"{Numerator}/{Denominator}";
        }

        public override bool Equals(object obj) => Equals(obj as RationalNumber);

        public bool Equals(RationalNumber other)
        {
            if (other is null) return false;
            return Numerator == other.Numerator && Denominator == other.Denominator;
        }

        public override int GetHashCode() => HashCode.Combine(Numerator, Denominator);

        public int CompareTo(RationalNumber other)
        {
            if (other is null) return 1;
            long lhs = Numerator * other.Denominator;
            long rhs = other.Numerator * Denominator;
            return lhs.CompareTo(rhs);
        }
    }
}
