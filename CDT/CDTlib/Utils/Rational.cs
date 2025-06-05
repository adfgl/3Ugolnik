namespace CDTlib.Utils
{
    public readonly struct Rational : IEquatable<Rational>, IComparable<Rational>
    {
        public readonly long num;
        public readonly long den;

        public Rational(long num, long den)
        {
            if (den == 0)
                throw new DivideByZeroException("Denominator cannot be zero.");

            if (num == 0)
            {
                this.num = 0;
                this.den = 1;
                return;
            }

            if (den < 0)
            {
                num = -num;
                den = -den;
            }

            long gcd = GreatestCommonDivisor(Math.Abs(num), den);
            this.num = num / gcd;
            this.den = den / gcd;
        }

        public Rational(double value, long maxDenominator = 1_000_000)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentException("Invalid double value.");

            bool isNegative = value < 0;
            value = Math.Abs(value);

            long lowerNumerator = 0;
            long lowerDenominator = 1;
            long upperNumerator = 1;
            long upperDenominator = 0;

            long bestNumerator = 0;
            long bestDenominator = 1;

            while (true)
            {
                long middleNumerator = lowerNumerator + upperNumerator;
                long middleDenominator = lowerDenominator + upperDenominator;

                if (middleDenominator > maxDenominator)
                    break;

                double middleValue = (double)middleNumerator / middleDenominator;

                if (middleValue < value)
                {
                    lowerNumerator = middleNumerator;
                    lowerDenominator = middleDenominator;
                }
                else
                {
                    upperNumerator = middleNumerator;
                    upperDenominator = middleDenominator;
                }

                bestNumerator = middleNumerator;
                bestDenominator = middleDenominator;

                if (Math.Abs(middleValue - value) < 1e-15)
                    break;
            }

            long gcd = GreatestCommonDivisor(bestNumerator, bestDenominator);
            num = (isNegative ? -bestNumerator : bestNumerator) / gcd;
            den = bestDenominator / gcd;
        }


        public static Rational operator +(Rational a, Rational b)
        {
            long lcm = LeastCommonMultiple(a.den, b.den);
            long n1 = a.num * (lcm / a.den);
            long n2 = b.num * (lcm / b.den);
            return new Rational(n1 + n2, lcm);
        }

        public static Rational operator -(Rational a, Rational b) => new Rational(a.num * b.den - b.num * a.den, a.den * b.den);
        public static Rational operator *(Rational a, Rational b) => new Rational(a.num * b.num, a.den * b.den);
        public static Rational operator /(Rational a, Rational b)
        {
            if (b.num == 0) throw new DivideByZeroException();
            return new Rational(a.num * b.den, a.den * b.num);
        }

        public static Rational operator -(Rational r) => new Rational(-r.num, r.den);

        public int CompareTo(Rational other)
        {
            return (num * other.den).CompareTo(other.num * den);
        }

        public override string ToString() => $"{num}/{den}";
        public override bool Equals(object obj) => obj is Rational r && Equals(r);
        public bool Equals(Rational other) => num == other.num && den == other.den;
        public override int GetHashCode() => HashCode.Combine(num, den);

        public static bool operator ==(Rational a, Rational b) => a.Equals(b);
        public static bool operator !=(Rational a, Rational b) => !a.Equals(b);
        public static bool operator <(Rational a, Rational b) => a.CompareTo(b) < 0;
        public static bool operator >(Rational a, Rational b) => a.CompareTo(b) > 0;
        public static bool operator <=(Rational a, Rational b) => a.CompareTo(b) <= 0;
        public static bool operator >=(Rational a, Rational b) => a.CompareTo(b) >= 0;

        static long GreatestCommonDivisor(long a, long b)
        {
            if (a == 0) return Math.Abs(b);
            if (b == 0) return Math.Abs(a);

            a = Math.Abs(a);
            b = Math.Abs(b);

            int shift = 0;

            // Remove common factors of 2
            while (((a | b) & 1) == 0)
            {
                a >>= 1;
                b >>= 1;
                shift++;
            }

            // Make sure 'a' is odd
            while ((a & 1) == 0)
                a >>= 1;

            while (b != 0)
            {
                while ((b & 1) == 0)
                    b >>= 1;

                if (a > b)
                {
                    long temp = a;
                    a = b;
                    b = temp;
                }

                b -= a;
            }
            return a << shift;
        }

        static long LeastCommonMultiple(long a, long b)
        {
            return a / GreatestCommonDivisor(a, b) * b;
        }

        public double ToDouble()
        {
            return num / (double)den;
        }
    }
}
