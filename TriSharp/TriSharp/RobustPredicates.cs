using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TriSharp
{
    public static class RobustPredicates
    {
        const double Epsilon = 1.1102230246251565e-16; // 2^-53

        public static double Orient2D((double X, double Y) a, (double X, double Y) b, (double X, double Y) c)
        {
            double detLeft = (a.X - c.X) * (b.Y - c.Y);
            double detRight = (a.Y - c.Y) * (b.X - c.X);
            double det = detLeft - detRight;

            double errBound = (3.0 + 16.0 * Epsilon) * Math.Abs(detLeft + detRight);
            if (Math.Abs(det) > errBound)
                return det;

            // Fallback to adaptive exact version
            return Orient2D_Exact(a, b, c);
        }

        private static double Orient2D_Exact((double X, double Y) a, (double X, double Y) b, (double X, double Y) c)
        {
            TwoProduct(a.X - c.X, b.Y - c.Y, out double p1, out double p0);
            TwoProduct(a.Y - c.Y, b.X - c.X, out double q1, out double q0);

            double s1, s0, t1, t0;
            TwoDiff(p1, q1, out s1, out t1);
            TwoDiff(p0, q0, out s0, out t0);

            return s1 + t1 + s0 + t0;
        }

        public static double InCircle(
            (double X, double Y) a,
            (double X, double Y) b,
            (double X, double Y) c,
            (double X, double Y) d)
        {
            double adx = a.X - d.X;
            double ady = a.Y - d.Y;
            double bdx = b.X - d.X;
            double bdy = b.Y - d.Y;
            double cdx = c.X - d.X;
            double cdy = c.Y - d.Y;

            double abdet = adx * bdy - bdx * ady;
            double bcdet = bdx * cdy - cdx * bdy;
            double cadet = cdx * ady - adx * cdy;

            double alift = adx * adx + ady * ady;
            double blift = bdx * bdx + bdy * bdy;
            double clift = cdx * cdx + cdy * cdy;

            double det = alift * bcdet + blift * cadet + clift * abdet;

            double errBound = 1e-12 * (Math.Abs(alift * bcdet) + Math.Abs(blift * cadet) + Math.Abs(clift * abdet));
            if (Math.Abs(det) > errBound)
                return det;

            // TODO: implement InCircle exact fallback using expansion
            return det; // For now, return the same
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TwoProduct(double a, double b, out double x, out double y)
        {
            x = a * b;
            Split(a, out double aHi, out double aLo);
            Split(b, out double bHi, out double bLo);
            y = ((aHi * bHi - x) + aHi * bLo + aLo * bHi) + aLo * bLo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TwoDiff(double a, double b, out double x, out double y)
        {
            x = a - b;
            double bv = a - x;
            y = (a - (x + bv)) + (bv - b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Split(double a, out double hi, out double lo)
        {
            const double splitter = (1 << 27) + 1.0;
            double c = splitter * a;
            double abig = c - a;
            hi = c - abig;
            lo = a - hi;
        }
    }
}
