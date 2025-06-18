using CDTlib;

namespace CDTConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CDTPoint a = new CDTPoint(-50, -50);
            CDTPoint b = new CDTPoint(+50, -50);
            CDTPoint c = new CDTPoint(+50, +50);
            CDTPoint d = new CDTPoint(-50, +50);

            CDTLineSegment ab = new CDTLineSegment(a, b);
            CDTLineSegment bc = new CDTLineSegment(b, c);
            CDTLineSegment cd = new CDTLineSegment(c, d);
            CDTLineSegment da = new CDTLineSegment(d, a);

            CDTPolygon polygon = new CDTPolygon();
            polygon.Contour = [ab, bc, cd, da];

            CDTInput input = new CDTInput()
            {
                Polygons = [polygon],
            };

            var cdt = new CDT(input);


            Console.WriteLine(cdt.Mesh.ToSvg());
        }
    }
}
