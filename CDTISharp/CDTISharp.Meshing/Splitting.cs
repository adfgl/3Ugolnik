using CDTISharp.Geometry;
using System.Diagnostics;

namespace CDTISharp.Meshing
{
    public static class Splitting
    {
        public static Triangle[] SplitEdgeWithAdjacent(List<Triangle> triangles, List<Node> nodes, int triangle, int edge, Node node)
        {
            /*
                         c                            c            
                         /\                          /|\             
                        /  \                        / | \           
                       /    \                      /  |  \          
                      /      \                    /   |   \       
                     /  old0  \                  /    |    \        
                    /          \                / new0|new1 \       
                   /            \              /      |      \      
                a +--------------+ b        a +-------e-------+ b  
                   \            /              \      |      /      
                    \          /                \ new3|new2 /       
                     \  old1  /                  \    |    /        
                      \      /                    \   |   /      
                       \    /                      \  |  /          
                        \  /                        \ | /           
                         \/                          \|/            
                         d                            d            
             */

            int t0 = triangle;
            Triangle old0 = triangles[triangle];
            Debug.Assert(t0 == old0.index);

            int t1 = old0.adjacent[edge];
            Triangle old1 = triangles[t1];
            Debug.Assert(t1 == old1.index);

            int t2 = triangles.Count;
            int t3 = t2 + 1;


            int ab = edge;
            int bc = Mesh.NEXT[edge];
            int ca = Mesh.PREV[edge];

            Node a = nodes[old0.indices[ab]];
            Node b = nodes[old0.indices[bc]];
            Node c = nodes[old0.indices[ca]];

            int ba = old1.IndexOf(b.Index, a.Index);
            int ad = Mesh.NEXT[ba];
            int db = Mesh.PREV[ba];

            Node e = node;
            Node d = nodes[old1.indices[db]];

            int constraint = old0.constraints[ab];
            Debug.Assert(constraint == old1.constraints[ba]);

            // CAE
            Triangle new0 = new Triangle(t0, c, a, e);
            new0.adjacent[0] = old0.adjacent[ca];
            new0.adjacent[1] = t3;
            new0.adjacent[2] = t1;

            new0.constraints[0] = old0.constraints[ca];
            new0.constraints[1] = constraint;
            new0.constraints[2] = -1;

            // BCE
            Triangle new1 = new Triangle(t1, b, c, e);
            new1.adjacent[0] = old0.adjacent[bc];
            new1.adjacent[1] = t0;
            new1.adjacent[2] = t2;

            new1.constraints[0] = old0.constraints[bc];
            new1.constraints[1] = -1;
            new1.constraints[2] = constraint;

            // DBE
            Triangle new2 = new Triangle(t2, d, b, e);
            new2.adjacent[0] = old1.adjacent[db];
            new2.adjacent[1] = t1;
            new2.adjacent[2] = t3;

            new2.constraints[0] = old1.constraints[db];
            new2.constraints[1] = constraint;
            new2.constraints[2] = -1;

            // ADE
            Triangle new3 = new Triangle(t3, a, d, e);
            new3.adjacent[0] = old1.adjacent[ad];
            new3.adjacent[1] = t2;
            new3.adjacent[2] = t0;

            new3.constraints[0] = old1.constraints[ad];
            new3.constraints[1] = -1;
            new3.constraints[2] = constraint;

            return [new0, new1, new2, new3];
        }

        public static Triangle[] SplitEdgeNoAdjacent(List<Triangle> triangles, List<Node> nodes, int triangle, int edge, Node node)
        {
            /*
                       c                            c        
                       /\                          /|\        
                      /  \                        / | \       
                     /    \                      /  |  \      
                    /      \                    /   |   \      
                   /  old   \                  /    |    \    
                  /          \                /     |     \   
                 /            \              /  new0|new1  \  
              a +--------------+ b        a +-------+-------+ b
                                                    e
           */

            int t0 = triangle;
            Triangle old = triangles[t0];
            Debug.Assert(t0 == old.index);

            int t1 = triangles.Count;

            int ab = edge;
            int bc = Mesh.NEXT[edge];
            int ca = Mesh.PREV[edge];

            Node a = nodes[old.indices[ab]];
            Node b = nodes[old.indices[bc]];
            Node c = nodes[old.indices[ca]];
            Node e = node;

            int constraint = old.constraints[edge];

            // CAE
            Triangle new0 = new Triangle(t0, c, a, e);
            new0.adjacent[0] = old.adjacent[ca];
            new0.adjacent[1] = -1;
            new0.adjacent[2] = t1;

            new0.constraints[0] = old.constraints[ca];
            new0.constraints[1] = constraint;
            new0.constraints[2] = -1;

            // BCE
            Triangle new1 = new Triangle(t1, b, c, e);
            new1.adjacent[0] = old.adjacent[bc];
            new1.adjacent[1] = t0;
            new1.adjacent[2] = -1;

            new1.constraints[0] = old.constraints[bc];
            new1.constraints[1] = -1;
            new1.constraints[2] = constraint;

            return [new0, new1];
        }

        public static Triangle[] Split(List<Triangle> triangles, List<Node> nodes, int triangle, int edge, Node node)
        {
            if (triangles[triangle].adjacent[edge] == -1)
            {
                return SplitEdgeNoAdjacent(triangles, nodes, triangle, edge, node);
            }
            return SplitEdgeWithAdjacent(triangles, nodes, triangle, edge, node);
        }

        public static Triangle[] Split(List<Triangle> triangles, List<Node> nodes, int triangle, Node node)
        {
            /*
                         * c



                     /           ^
                    ^ new2*d new1\

                         new0

               a *        ->         * b

            */

            Triangle old = triangles[triangle];
            Node a = nodes[old.indices[0]];
            Node b = nodes[old.indices[1]];
            Node c = nodes[old.indices[2]];
            Node d = node;

            int t0 = triangle;
            int t1 = triangles.Count;
            int t2 = triangles.Count + 1;

            // ABD
            Triangle new0 = new Triangle(t0, a, b, d);
            new0.adjacent[0] = old.adjacent[0];
            new0.adjacent[1] = t1;
            new0.adjacent[2] = t2;

            new0.constraints[0] = old.constraints[0];

            // BCD
            Triangle new1 = new Triangle(t1, b, c, d);
            new1.adjacent[0] = old.adjacent[1];
            new1.adjacent[1] = t2;
            new1.adjacent[2] = t0;

            new1.constraints[0] = old.constraints[1];

            // CAD
            Triangle new2 = new Triangle(t2, c, a, d);
            new2.adjacent[0] = old.adjacent[2];
            new2.adjacent[1] = t0;
            new2.adjacent[2] = t1;

            new2.constraints[0] = old.constraints[2];

            return [new0, new1, new2];
        }
    }
}
