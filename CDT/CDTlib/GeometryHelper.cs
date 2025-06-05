using CDTlib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CDTlib
{
    public enum EOrientation
    {
        Left, Right, Colinear
    }

    public static class GeometryHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Area(double x0, double y0, double x1, double y1, double x2, double y2)
        {
            return Cross(x0, y0, x1, y1, x2, y2) * 0.5;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Cross(double x0, double y0, double x1, double y1, double x2, double y2)
        {
            double abx = x1 - x0, aby = y1 - y0;
            double acx = x2 - x0, acy = y2 - y0;
            return abx * acy - aby * acx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rational Cross(Rational x0, Rational y0, Rational x1, Rational y1, Rational x2, Rational y2)
        {
            Rational abx = x1 - x0, aby = y1 - y0;
            Rational acx = x2 - x0, acy = y2 - y0;
            return abx * acy - aby * acx;
        }

        public static EOrientation Orientation(double x0, double y0, double x1, double y1, double x2, double y2, double eps = 1e-6)
        {
            double cross = Cross(x0, y0, x1, y1, x2, y2);

            int sign;
            if (Math.Abs(cross) > eps || cross == 0)
            {
                sign = double.Sign(cross);
            }
            else
            {
                Rational rx0 = new Rational(x0), ry0 = new Rational(y0);
                Rational rx1 = new Rational(x1), ry1 = new Rational(y1);
                Rational rx2 = new Rational(x2), ry2 = new Rational(y2);

                Rational exactCross = Cross(rx0, ry0, rx1, ry1, rx2, ry2);
                sign = exactCross.Sign();
            }

            if (sign == 0)
            {
                return EOrientation.Colinear;
            }
            return sign > 0 ? EOrientation.Left : EOrientation.Right;
        }
    }
}
