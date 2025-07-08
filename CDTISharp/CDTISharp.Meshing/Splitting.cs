using CDTISharp.Geometry;

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

            Triangle old0 = triangles[triangle];
            int constraint = old0.constraints[edge];

            int ab = edge;
            int bc = Mesh.NEXT[edge];
            int ca = Mesh.PREV[edge];

            Node a = nodes[old0.indices[ab]];
            Node b = nodes[old0.indices[bc]];
            Node c = nodes[old0.indices[ca]];
            Node e = node;

            int adjIndex = old0.adjacent[ab];
            if (adjIndex == -1)
            {
                throw new Exception($"{nameof(SplitEdgeWithAdjacent)}: {old0} is supposed to have an adjacent triangle.");
            }

            Triangle old1 = triangles[adjIndex];

            int t0 = old0.index;
            int t1 = triangles.Count;
            int t2 = old1.index;
            int t3 = triangles.Count + 1;

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

            int ba = old1.IndexOf(b.Index, a.Index);
            int ad = Mesh.NEXT[ba];
            int db = Mesh.PREV[ba];
            Node d = nodes[old1.indices[db]];

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
                   /  t0    \                  /    |    \    
                  /          \                /     |     \   
                 /            \              /  t0  |  t1  \  
              a +--------------+ b        a +-------+-------+ b
                                                    e
           */

            Triangle old0 = triangles[triangle];
            int constraint = old0.constraints[edge];

            int ab = edge;
            int bc = Mesh.NEXT[edge];
            int ca = Mesh.PREV[edge];

            Node a = nodes[old0.indices[ab]];
            Node b = nodes[old0.indices[bc]];
            Node c = nodes[old0.indices[ca]];
            Node e = node;

            int adjIndex = old0.adjacent[ab];
            if (adjIndex != -1)
            {
                throw new Exception($"{nameof(SplitEdgeNoAdjacent)}: {old0} not supposed to have an adjacent triangle.");
            }

            int t0 = old0.index;
            int t1 = triangles.Count;

            // CAE
            Triangle new0 = new Triangle(t0, c, a, e);
            new0.adjacent[0] = old0.adjacent[ca];
            new0.adjacent[1] = -1;
            new0.adjacent[2] = t1;

            new0.constraints[0] = old0.constraints[ca];
            new0.constraints[1] = constraint;
            new0.constraints[2] = -1;

            // BCE
            Triangle new1 = new Triangle(t1, b, c, e);
            new1.adjacent[0] = old0.adjacent[bc];
            new1.adjacent[1] = t0;
            new1.adjacent[2] = -1;

            new1.constraints[0] = old0.constraints[bc];
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
                         * v2



                     /           ^
                    ^      *v3     \



               v0 *        ->         * v1

            */

            Triangle old = triangles[triangle];
            Node v0 = nodes[old.indices[0]];
            Node v1 = nodes[old.indices[1]];
            Node v2 = nodes[old.indices[2]];
            Node v3 = node;

            int t0 = triangle;
            int t1 = triangles.Count;
            int t2 = t1 + 1;

            Triangle new0 = new Triangle(t0, v0, v1, v3);
            new0.adjacent[0] = old.adjacent[0];
            new0.adjacent[1] = t1;
            new0.adjacent[2] = t2;

            new0.constraints[0] = old.constraints[0];

            Triangle new1 = new Triangle(t1, v1, v2, v3);
            new1.adjacent[0] = old.adjacent[1];
            new1.adjacent[1] = t2;
            new1.adjacent[2] = t0;

            new1.constraints[0] = old.constraints[1];

            Triangle new2 = new Triangle(t2, v2, v0, v3);
            new2.adjacent[0] = old.adjacent[2];
            new2.adjacent[1] = t0;
            new2.adjacent[2] = t1;

            new2.constraints[0] = old.constraints[2];

            return [new0, new1, new2];
        }
    }
}
