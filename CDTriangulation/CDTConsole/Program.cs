using CDTlib;

namespace CDTConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var points = Square(0, 0, 100);
            //points = RandomPointCloud(0, 0, 100, 50);

            var mesh = CDT.Triangulate(o => o.X, o => o.Y, points);


            Console.WriteLine(mesh.ToSvg());
        }


        public static List<CDTPoint> RandomPointCloud(double cx, double cy, double r, int n)
        {
            var rand = new Random();
            var points = new List<CDTPoint>(n);

            for (int i = 0; i < n; i++)
            {
                double x = cx - r + rand.NextDouble() * 2 * r;
                double y = cy - r + rand.NextDouble() * 2 * r;
                points.Add(new CDTPoint(x, y));
            }

            return points;
        }

        public static List<CDTPoint> Square(double cx, double cy, double r)
        {
            return new List<CDTPoint>
            {
                new CDTPoint(cx - r, cy - r),
                new CDTPoint(cx + r, cy - r),
                new CDTPoint(cx + r, cy + r),
                new CDTPoint(cx - r, cy + r)
            };
        }

        public static List<CDTPoint> Circle(double cx, double cy, double r, int steps)
        {
            List<CDTPoint> result = new List<CDTPoint>(steps);
            for (int i = 0; i < steps; i++)
            {
                double angle = 2 * MathF.PI * i / steps;
                result.Add(new CDTPoint(
                    cx + r * Math.Cos(angle),
                    cy + r * Math.Sin(angle)
                ));
            }
            return result;
        }

        public static List<CDTPoint> Star(double cx, double cy, double outerRadius, double innerRadius, int points)
        {
            int totalSteps = points * 2;
            List<CDTPoint> result = new List<CDTPoint>(totalSteps);
            for (int i = 0; i < totalSteps; i++)
            {
                double angle = 2 * Math.PI * i / totalSteps;
                double r = (i % 2 == 0) ? outerRadius : innerRadius;

                result.Add(new CDTPoint(
                    cx + r * Math.Cos(angle),
                    cy + r * Math.Sin(angle)
                ));
            }
            return result;
        }
    }
}
