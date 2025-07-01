
using System.Data;

namespace CDTISharp.Meshing
{
    public class Mesh
    {
        public const int CONTOUR = 0;
        public const int HOLE = 1;
        public const int USER = 2;

        public readonly static int[] NEXT = [1, 2, 0];
        public readonly static int[] PREV = [2, 0, 1];

        readonly List<Triangle> _triangles;
        readonly QuadTree _qt;
        readonly Rectangle _bounds;

        public List<Triangle> Triangles => _triangles;
        public List<Node> Nodes => _qt.Items;
        public Rectangle Bounds => _bounds;

        public Mesh(Rectangle rectangle)
        {
            _triangles = new List<Triangle>();
            _bounds = rectangle;
            _qt = new QuadTree(rectangle);
        }

        public Triangle[] Flip(int triangle, int edge)
        {
            Triangle old0 = _triangles[triangle];
            int adjIndex = old0.adjacent[edge];
            Triangle old1 = _triangles[adjIndex];

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

            Node a = Nodes[old0.indices[0]];
            Node b = Nodes[old0.indices[1]];
            Node c = Nodes[old0.indices[2]];

            int twin = old1.IndexOf(c.Index, a.Index);

            Node d = Nodes[old1.indices[PREV[twin]]];

            int t0 = old0.index;
            int t1 = old1.index;

            int bc = NEXT[edge];
            int ca = PREV[edge];
            int ad = NEXT[twin];
            int db = PREV[twin];

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

        public Triangle[] Split(int triangle, int edge, int node)
        {
            Triangle old0 = _triangles[triangle];
            int constraint = old0.constraints[edge];

            Node a = Nodes[old0.indices[0]];
            Node b = Nodes[old0.indices[1]];
            Node c = Nodes[old0.indices[2]];
            Node e = Nodes[node];

            int bc = NEXT[edge];
            int ca = PREV[edge];

            int adjIndex = old0.adjacent[edge];

            Triangle[] triangles;
            if (adjIndex == -1)
            {
                /*
                             c                            c        
                             /\                          /|\        
                            /  \                        / | \       
                           /    \                      /  |  \      
                          /      \                    /   |   \      
                         /  f0    \                  /    |    \    
                        /          \                /     |     \   
                       /            \              /  f0  |  f1  \  
                    a +--------------+ b        a +-------+-------+ b
                                                          e
                 */

                int t0 = triangle;
                int t1 = _triangles.Count;

                Triangle new0 = new Triangle(t0, c, a, e);
                new0.adjacent[0] = old0.adjacent[ca];
                new0.adjacent[1] = -1;
                new0.adjacent[2] = t1;

                new0.constraints[0] = old0.constraints[ca];
                new0.constraints[1] = constraint;
                new0.constraints[2] = -1;

                Triangle new1 = new Triangle(t1, b, c, e);
                new1.adjacent[0] = old0.adjacent[bc];
                new1.adjacent[1] = t0;
                new1.adjacent[2] = -1;

                new1.constraints[0] = old0.constraints[bc];
                new1.constraints[1] = -1;
                new1.constraints[2] = constraint;

                triangles = [new0, new1];
            }
            else
            {
                Triangle old1 = _triangles[adjIndex];


                /*
                         c                            c            
                         /\                          /|\             
                        /  \                        / | \           
                       /    \                      /  |  \          
                      /      \                    /   |   \       
                     /   f0   \                  /    |    \        
                    /          \                / f0  |  f1 \       
                   /            \              /      |      \      
                a +--------------+ b        a +-------e-------+ b  
                   \            /              \      |      /      
                    \          /                \ f3  |  f2 /       
                     \   f1   /                  \    |    /        
                      \      /                    \   |   /      
                       \    /                      \  |  /          
                        \  /                        \ | /           
                         \/                          \|/            
                         d                            d            
             */

                int twin = old1.IndexOf(c.Index, a.Index);
                Node d = Nodes[old1.indices[PREV[twin]]];

                int ad = NEXT[edge];
                int db = PREV[edge];

                int t0 = old0.index;
                int t1 = old1.index;
                int t2 = _triangles.Count;
                int t3 = t2 + 1;

                Triangle new0 = new Triangle(t0, c, a, e);
                new0.adjacent[0] = old0.adjacent[ca];
                new0.adjacent[1] = t3;
                new0.adjacent[2] = t1;

                new0.constraints[0] = old0.constraints[ca];
                new0.constraints[1] = constraint;
                new0.constraints[2] = -1;

                Triangle new1 = new Triangle(t1, b, c, e);
                new1.adjacent[0] = old0.adjacent[bc];
                new1.adjacent[1] = t0;
                new1.adjacent[2] = t2;

                new1.constraints[0] = old0.constraints[bc];
                new1.constraints[1] = -1;
                new1.constraints[2] = constraint;

                Triangle new2 = new Triangle(t2, d, b, e);
                new2.adjacent[0] = old1.adjacent[db];
                new2.adjacent[1] = t1;
                new2.adjacent[2] = t3;

                new2.constraints[0] = old0.constraints[db];
                new2.constraints[1] = constraint;
                new2.constraints[2] = -1;

                Triangle new3 = new Triangle(t3, a, d, e);
                new3.adjacent[0] = old1.adjacent[ad];
                new3.adjacent[1] = t2;
                new3.adjacent[2] = t0;

                new3.constraints[0] = old0.constraints[ad];
                new3.constraints[1] = -1;
                new3.constraints[2] = constraint;

                triangles = [new0, new1, new2, new3];
            }
            return triangles;
        }

        public Triangle[] Split(int triangle, int node)
        {
            /*
                         *C



                    /           ^
                   ^      *D     \



               A*        ->         *B

            */

            Triangle old = _triangles[triangle];
            Node a = Nodes[old.indices[0]];
            Node b = Nodes[old.indices[1]];
            Node c = Nodes[old.indices[2]];
            Node d = Nodes[node];

            int t0 = triangle;
            int t1 = _triangles.Count;
            int t2 = t1 + 1;

            Triangle new0 = new Triangle(t0, a, b, d);
            new0.adjacent[0] = old.adjacent[0];
            new0.adjacent[1] = t1;
            new0.adjacent[2] = t2;

            new0.constraints[0] = old.constraints[0];

            Triangle new1 = new Triangle(t1, b, c, d);
            new1.adjacent[0] = old.adjacent[1];
            new1.adjacent[1] = t2;
            new1.adjacent[2] = t0;

            new1.constraints[0] = old.constraints[1];

            Triangle new2 = new Triangle(t2, b, c, d);
            new2.adjacent[0] = old.adjacent[2];
            new2.adjacent[1] = t0;
            new2.adjacent[2] = t1;

            new2.constraints[0] = old.constraints[2];

            return [new0, new1, new2];
        }
    }
}
