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

            CDTNode center = CDTNode.Between(a, c);
            CDTArcSegment arc0 = new CDTArcSegment(a, c, center, true) { NumSegments = 18 };
            CDTArcSegment arc1 = new CDTArcSegment(c, a, center, true) { NumSegments = 18 };

            CDTPolygon polygon = new CDTPolygon();
            polygon.Contour = [arc0, arc1];

            CDTInput input = new CDTInput()
            {
                Polygons = [polygon],
                Quality = new CDTQuality()
                {
                    MaxArea = 2
                }
            };

            var cdt = new CDT(input);
            var mesh = cdt.Mesh;

            Console.WriteLine(mesh.ToSvg(fill: false));

        }
    }
}
