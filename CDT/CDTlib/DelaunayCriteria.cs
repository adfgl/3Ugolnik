namespace CDTlib
{
    public static class DelaunayCriteria
    {
        public static bool DelaunayFulfilled(
            double x0, double y0,
            double x1, double y1, 
            double x2, double y2,
            double x3, double y3)
        {
            double dx01 = x0 - x1, dy01 = y0 - y1;
            double dx03 = x0 - x3, dy03 = y0 - y3;
            double dx21 = x2 - x1, dy21 = y2 - y1;
            double dx23 = x2 - x3, dy23 = y2 - y3;

            double sAlpha = dx01 * dx03 + dy01 * dy03;
            if (sAlpha >= 0 && dx21 * dx23 + dy21 * dy23 >= 0)
            {
                return true;
            }

            double term1 = dx01 * dy03 - dx03 * dy01;
            double term2 = dx23 * dx21 + dy23 * dy21;
            double term3 = sAlpha;
            double term4 = dx23 * dy21 - dx21 * dy23;

            double result = term1 * term2 + term3 * term4;
            return result >= 0;
        }

        public static bool SumOfOppositeAnglesLessThanPi(
    double x0, double y0, // P0
    double x1, double y1, // P1 (shared)
    double x2, double y2, // P2 (shared)
    double x3, double y3  // P3
)
        {
            // ∠P0–P1–P2
            double ux1 = x0 - x1, uy1 = y0 - y1;
            double vx1 = x2 - x1, vy1 = y2 - y1;
            double dot1 = ux1 * vx1 + uy1 * vy1;
            double len1 = Math.Sqrt((ux1 * ux1 + uy1 * uy1) * (vx1 * vx1 + vy1 * vy1));
            double angle1 = Math.Acos(Clamp(dot1 / len1));

            // ∠P1–P2–P3
            double ux2 = x1 - x2, uy2 = y1 - y2;
            double vx2 = x3 - x2, vy2 = y3 - y2;
            double dot2 = ux2 * vx2 + uy2 * vy2;
            double len2 = Math.Sqrt((ux2 * ux2 + uy2 * uy2) * (vx2 * vx2 + vy2 * vy2));
            double angle2 = Math.Acos(Clamp(dot2 / len2));

            return angle1 + angle2 < Math.PI;
        }

        // Clamp to avoid NaN due to floating-point rounding
        private static double Clamp(double x)
        {
            if (x < -1.0) return -1.0;
            if (x > 1.0) return 1.0;
            return x;
        }

        public static bool InCircle(
            double x0, double y0,
            double x1, double y1,
            double x2, double y2,
            double x3, double y3)
        {
            double dx1 = x1 - x0, dy1 = y1 - y0;
            double dx2 = x2 - x0, dy2 = y2 - y0;
            double dx3 = x3 - x0, dy3 = y3 - y0;

            double det =
                (dx1 * dx1 + dy1 * dy1) * (dx2 * dy3 - dx3 * dy2) -
                (dx2 * dx2 + dy2 * dy2) * (dx1 * dy3 - dx3 * dy1) +
                (dx3 * dx3 + dy3 * dy3) * (dx1 * dy2 - dx2 * dy1);

            return det > 0;
        }
    }
}
