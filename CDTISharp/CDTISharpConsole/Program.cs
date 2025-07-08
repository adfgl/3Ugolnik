using CDTISharp;
using CDTISharp.IO;

namespace CDTISharpConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var a = new CDTNode(-100, -100);
            var b = new CDTNode(+100, -100);
            var c = new CDTNode(+100, +100);
            var d = new CDTNode(-100, +100);

            var e = new CDTNode(-50, -50);  
            var f = new CDTNode(+50, +50);

            var g = new CDTNode(-50, +50);
            var h = new CDTNode(+50, -50);


            var ab = new CDTLineSegment(a, b);
            var bc = new CDTLineSegment(b, c);
            var cd = new CDTLineSegment(c, d);
            var da = new CDTLineSegment(d, a);


            var input = new CDTInput()
            {
                Contour = [ab, bc, cd, da],
                ConstraintEdges = [new CDTArcSegment(e, f, new CDTNode(0, 0), true ) {  Segments = 16}], // 

                Quality = new CDTQuality()
                {
                    MaxArea = 450
                }
            };

            CDT.Triangulate(input);
        }
    }
}
