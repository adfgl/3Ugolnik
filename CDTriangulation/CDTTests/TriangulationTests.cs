using CDTlib;

namespace CDTTests
{
    public class TriangulationTests
    {
        static Mesh TestCase()
        {
            /*
               5-------6------7
               |\  4  /\   5 /|
               | \   /  \   / |
               |  \ /  0 \ /  |
               | 6 3------4  7|
               |  / \  1 / \  |
               | /   \  /   \ |
               |/  2  \/   3 \|
               0-------1------2
             */

            Node n0 = new Node(0, -50, -50);
            Node n1 = new Node(1, 0, -50);
            Node n2 = new Node(2, +50, -50);

            Node n3 = new Node(3, -25, 0);
            Node n4 = new Node(4, +25, 0);

            Node n5 = new Node(5, -50, +50);
            Node n6 = new Node(6, 0, +50);
            Node n7 = new Node(7, +50, +50);

            Mesh mesh = new Mesh();
            mesh.Faces.Add(new Face(0, n3, n4, n6));
            mesh.Faces.Add(new Face(1, n1, n4, n3));
            mesh.Faces.Add(new Face(2, n0, n1, n3));
            mesh.Faces.Add(new Face(3, n1, n2, n4));
            mesh.Faces.Add(new Face(4, n3, n6, n5));
            mesh.Faces.Add(new Face(5, n4, n7, n6));
            mesh.Faces.Add(new Face(6, n0, n3, n5));
            mesh.Faces.Add(new Face(7, n4, n2, n7));

            foreach (Face face in mesh.Faces)
            {
                foreach (Edge edge in face)
                {
                    var (start, end) = edge;

                    Edge? twin = mesh.FindEdgeBrute(end, start);
                    if (twin is null)
                    {
                        throw new Exception("Invalid test data");
                    }

                    edge.Twin = twin;
                    twin.Twin = edge;
                }
            }


            return mesh;
        }
    }
}
