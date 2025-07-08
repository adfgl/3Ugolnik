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

            var ab = new CDTLineSegment(a, b);
            var bc = new CDTLineSegment(b, c);
            var cd = new CDTLineSegment(c, d);
            var da = new CDTLineSegment(d, a);

            var input = new CDTInput()
            {
                Contour = [ab, bc, cd, da],
                Quality = new CDTQuality()
                {
                    MaxArea = 400
                }
            };

            CDT.Triangulate(input);
        }
    }
}
