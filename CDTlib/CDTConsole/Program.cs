using CDTlib;

namespace CDTConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CDTNode a = new CDTNode(-50, -50);
            CDTNode b = new CDTNode(+50, -50);
            CDTNode c = new CDTNode(+50, +50);
            CDTNode d = new CDTNode(-50, +50);

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
