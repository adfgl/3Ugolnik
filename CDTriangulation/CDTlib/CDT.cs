using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace CDTlib
{
    public static class CDT
    {
        public static List<Face> Triangulate<T>(IEnumerable<T> points, Func<T, double> getX, Func<T, double> getY)
        {
            Rectangle rectangle = Rectangle.FromPoints(points, getX, getY);

            Mesh mesh = new Mesh();
            QuadTree nodes = new QuadTree(rectangle);
            foreach (T pt in points)
            {
                double x = getX(pt);
                double y = getY(pt);
                Node node = Insert(mesh, nodes, x, y, out _);
            }


            List<Face> faces = new List<Face>();
            return faces;
        }


        public static void InsertConstraint(List<Face> triangles, QuadTree nodes, Node a, Node b)
        {
            if (a == b)
                return;

            while (true)
            {

            }
        }

        public static Node Insert(Mesh mesh, QuadTree nodes, double x, double y, out List<Face> affected, Face? start = null)
        {
            Node? newNode = nodes.TryGet(x, y);
            if (newNode != null)
            {
                affected = new List<Face>();
                return newNode;
            }

            (Face? face, Edge? edge, Node? node) = mesh.FindContaining(x, y, start);
            if (node != null)
            {
                throw new Exception("Logic error. Should not have duplicate points at this point.");
            }

            if (face == null)
            {
                throw new Exception($"Point [{x} {y}] is outside of topology.");
            }

            newNode = new Node(nodes.Count, x, y);
            nodes.Add(newNode);

            TopologyChange topologyChange;
            if (edge == null)
            {
                topologyChange = face.Split(newNode);
            }
            else
            {
                topologyChange = edge.Split(newNode);
            }

            // do something before adding?

            mesh.Add(topologyChange);
            affected = Legalize(mesh, topologyChange);
            return newNode;
        }

        public static List<Face> Legalize(Mesh mesh, TopologyChange topologyChange)
        {
            List<Face> affected = new List<Face>();
            Stack<Edge> toLegalize = new Stack<Edge>(topologyChange.AffectedEdges);
            while (toLegalize.Count > 0)
            {
                Edge edge = toLegalize.Pop();
                if (!edge.ShouldFlip())
                {
                    continue;
                }

                TopologyChange flipped = edge.Flip();
                mesh.Add(flipped);

                foreach (Edge item in flipped.AffectedEdges)
                {
                    toLegalize.Push(item);
                }
            }
            return affected;
        }
    }
}
