using CDTISharp.Geometry;
using System.Runtime.CompilerServices;

namespace CDTISharp.Meshing
{
    public static class Flipping
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool QuadConvex(Node a, Node b, Node c, Node d)
        {
            double ab_bc = GeometryHelper.Cross(a, b, c.X, c.Y);
            double bc_cd = GeometryHelper.Cross(b, c, d.X, d.Y);
            double cd_da = GeometryHelper.Cross(c, d, a.X, a.Y);
            double da_ab = GeometryHelper.Cross(d, a, b.X, b.Y);
            return ab_bc > 0 && bc_cd > 0 && cd_da > 0 && da_ab > 0;
        }

        public static bool ShouldFlip(List<Triangle> triangles, List<Node> nodes, int triangle, int edge)
        {
            Triangle t0 = triangles[triangle];
            int adjIndex = t0.adjacent[edge];
            Triangle t1 = triangles[adjIndex];

            Node a = nodes[t0.indices[edge]];
            Node b = nodes[t0.indices[Mesh.NEXT[edge]]];
            int twin = t1.IndexOf(b.Index, a.Index);

            Node d = nodes[t1.indices[Mesh.PREV[twin]]];
            return t0.circle.Contains(d.X, d.Y);
        }

        public static bool CanFlip(List<Triangle> triangles, List<Node> nodes, int triangle, int edge)
        {
            /*
                           c           
                           /\          
                          /  \         
                         /    \        
                        /      \       
                       /   t0   \      
                      /          \     
                     /            \     
                  a +--------------+ b 
                     \            /    
                      \          /     
                       \   t1   /      
                        \      /       
                         \    /        
                          \  /         
                           \/          
                            d          
             */


            Triangle t0 = triangles[triangle];
            if (t0.constraints[edge] != -1)
            {
                return false;
            }

            int adjIndex = t0.adjacent[edge];
            if (adjIndex == -1)
            {
                return false;
            }

            Node a = nodes[t0.indices[edge]];
            Node b = nodes[t0.indices[Mesh.NEXT[edge]]];
            Node c = nodes[t0.indices[Mesh.PREV[edge]]];

            Triangle t1 = triangles[adjIndex];
            int twin = t1.IndexOf(b.Index, a.Index);
            Node d = nodes[t1.indices[Mesh.PREV[twin]]];

            return QuadConvex(b, c, a, d);
        }

        public static Triangle[] Flip(List<Triangle> triangles, List<Node> nodes, int triangle, int edge)
        {
            Triangle old0 = triangles[triangle];
            int adjIndex = old0.adjacent[edge];
            Triangle old1 = triangles[adjIndex];

            /*
                  d - is inserted point, we want to propagate flip away from it, otherwise we 
                  are risking ending up in flipping degeneracy
                       c                          c
                       /\                        /|\
                      /  \                      / | \
                     /    \                    /  |  \
                    /      \                  /   |   \ 
                   /   t0   \                /    |    \
                  /          \              /     |     \ 
                 /            \            /      |      \
              a +--------------+ b      a +   t0  |  t1   + b
                 \            /            \      |      /
                  \          /              \     |     /
                   \   t1   /                \    |    /
                    \      /                  \   |   / 
                     \    /                    \  |  /
                      \  /                      \ | /
                       \/                        \|/
                        d                         d
            */

            Node a = nodes[old0.indices[edge]];
            Node b = nodes[old0.indices[Mesh.NEXT[edge]]];
            Node c = nodes[old0.indices[Mesh.PREV[edge]]];

            int twin = old1.IndexOf(b.Index, a.Index);
            Node d = nodes[old1.indices[Mesh.PREV[twin]]];

            int t0 = old0.index;
            int t1 = old1.index;

            int bc = Mesh.NEXT[edge];
            int ca = Mesh.PREV[edge];
            int ad = Mesh.NEXT[twin];
            int db = Mesh.PREV[twin];

            Triangle new0 = new Triangle(t0, a, d, c);
            new0.adjacent[0] = old1.adjacent[ad];
            new0.adjacent[1] = t1;
            new0.adjacent[2] = old0.adjacent[ca];

            new0.constraints[0] = old1.constraints[ad];
            new0.constraints[1] = -1;
            new0.constraints[2] = old0.constraints[ca];

            Triangle new1 = new Triangle(t1, d, b, c);
            new1.adjacent[0] = old1.adjacent[db];
            new1.adjacent[1] = old0.adjacent[bc];
            new1.adjacent[2] = t0;

            new1.constraints[0] = old1.constraints[db];
            new1.constraints[1] = old0.constraints[bc];
            new1.constraints[2] = -1;

            return [new0, new1];
        }

    }
}
