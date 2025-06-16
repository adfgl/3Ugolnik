namespace CDTlib
{
    public readonly struct Affected
    {
        public readonly Triangle triangle;
        public readonly int edge;

        public Affected(Triangle triangle, int edge)
        {
            this.triangle = triangle;
            this.edge = edge;
        }
    }

    public class Mesh
    {
        public readonly static int[] NEXT = [1, 2, 0], PREV = [2, 0, 1];

        readonly List<Node> _nodes = new List<Node>();
        readonly List<Triangle> _triangles = new List<Triangle>();

        public List<Triangle> Triangles => _triangles;
        public List<Node> Nodes => _nodes;

        public void Add(Affected[] tris)
        {
            foreach (Affected item in tris)
            {
                Triangle triangle = item.triangle;
                int edge = item.edge;

                int index = triangle.index;
                int adj = triangle.adjacent[edge];
                if (adj != -1)
                {
                    int start = triangle.indices[edge];
                    int end = triangle.indices[NEXT[edge]];
                    Triangle twin = _triangles[adj];
                    twin.adjacent[twin.IndexOf(end, start)] = index;
                }

                if (index < _triangles.Count)
                {
                    _triangles[index] = triangle;
                }
                else
                {
                    _triangles.Add(triangle);
                }
            }
        }

        public Affected[] Split(Triangle triangle, Node node)
        {
            Node a = _nodes[triangle.indices[0]];
            Node b = _nodes[triangle.indices[1]];
            Node c = _nodes[triangle.indices[2]];
            Node d = node;

            int t0 = triangle.index;
            int t1 = _triangles.Count;
            int t2 = t1 + 1;

            Triangle dab = new Triangle(t0, d, a, b);
            Triangle dbc = new Triangle(t1, d, b, c);
            Triangle dca = new Triangle(t2, d, c, a);

            dab.adjacent[0] = t2;
            dab.adjacent[1] = triangle.adjacent[0];
            dab.adjacent[2] = t1;

            dbc.adjacent[0] = t0;
            dbc.adjacent[1] = triangle.adjacent[1];
            dbc.adjacent[2] = t2;

            dca.adjacent[0] = t1;
            dca.adjacent[1] = triangle.adjacent[2];
            dca.adjacent[2] = t0;

            dab.constrained[1] = triangle.constrained[0];
            dbc.constrained[1] = triangle.constrained[1];
            dca.constrained[1] = triangle.constrained[2];

            return [new Affected(dab, 1), new Affected(dbc, 1), new Affected(dca, 1)];
        }

        public Affected[] Split(Triangle triangle, int edge, Node node)
        {
            int adj = triangle.adjacent[edge];
            if (adj == -1)
            {
                return SplitNoAdjacent(triangle, edge, node);
            }

            /*
                     d                            d            
                     /\                          /|\             
                    /  \                        / | \           
                   /    \                      /  |  \          
                  /      \                    /   |   \       
                 /   f0   \                  /    |    \        
                /          \                / f0  |  f1 \       
               /            \              /      |      \      
            a +--------------+ c        a +-------e-------+ c  
               \            /              \      |      /      
                \          /                \ f3  |  f2 /       
                 \   f1   /                  \    |    /        
                  \      /                    \   |   /      
                   \    /                      \  |  /          
                    \  /                        \ | /           
                     \/                          \|/            
                     b                            b            
         */

            Triangle acd = triangle;
            Triangle cab = _triangles[adj];

            int t0 = triangle.index;
            int t1 = adj;
            int t2 = _triangles.Count;
            int t3 = _triangles.Count + 1;

            int ac = edge;
            int cd = NEXT[ac];
            int da = PREV[ac];

            int ca = cab.IndexOf(cd, ac);
            int ab = NEXT[ca];
            int bc = PREV[ca];

            Node a = _nodes[acd.indices[ac]];
            Node b = _nodes[cab.indices[bc]];
            Node c = _nodes[acd.indices[cd]];
            Node d = _nodes[acd.indices[da]];
            Node e = node;

            Triangle eda = new Triangle(t0, e, d, a);
            Triangle ecd = new Triangle(t1, e, c, d);
            Triangle ebc = new Triangle(t2, e, b, c);
            Triangle eab = new Triangle(t3, e, a, b);

            eda.adjacent[0] = t1;
            eda.adjacent[1] = acd.adjacent[da];
            eda.adjacent[2] = t3;

            ecd.adjacent[0] = t2;
            ecd.adjacent[1] = acd.adjacent[cd];
            ecd.adjacent[2] = t0;

            ebc.adjacent[0] = t3;
            ebc.adjacent[1] = cab.adjacent[bc];
            ebc.adjacent[2] = t1;

            eab.adjacent[0] = t0;
            eab.adjacent[1] = cab.adjacent[ab];
            eab.adjacent[2] = t2;

            bool constrained = acd.constrained[ca];
            eda.constrained[2] = ecd.constrained[0] = ebc.constrained[2] = eab.constrained[0] = constrained;

            eda.constrained[1] = acd.constrained[da];
            ecd.constrained[1] = acd.constrained[cd];
            ebc.constrained[1] = cab.constrained[bc];
            eab.constrained[1] = cab.constrained[ab];

            return [new Affected(eda, 1), new Affected(ecd, 1), new Affected(ebc, 1), new Affected(eab, 1)];
        }

        Affected[] SplitNoAdjacent(Triangle triangle, int edge, Node node)
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
                                                      d
             */

            Triangle abc = triangle;

            Node a = _nodes[triangle.indices[0]];
            Node b = _nodes[triangle.indices[1]];
            Node c = _nodes[triangle.indices[2]];
            Node d = node;

            int t0 = triangle.index;
            int t1 = _triangles.Count;

            Triangle dca = new Triangle(t0, d, c, a);
            Triangle cdb = new Triangle(t1, c, d, b);

            dca.adjacent[0] = t1;
            dca.adjacent[1] = abc.adjacent[2];
            dca.adjacent[2] = -1;

            cdb.adjacent[0] = t0;
            cdb.adjacent[1] = -1;
            cdb.adjacent[2] = abc.adjacent[1];

            dca.constrained[1] = abc.constrained[2];
            cdb.constrained[2] = abc.constrained[1];

            bool constrained = abc.constrained[0];
            dca.constrained[2] = cdb.constrained[1] = constrained;

            return [new Affected(dca, 1), new Affected(cdb, 2)];
        }

        public void Flip(Triangle triangle, int edge)
        {
            int adj = triangle.adjacent[edge];
            if (adj == -1)
            {
                throw new Exception("Can't flip edge with not twin");
            }

            /*
              b - is inserted point, we want to propagate flip away from it, otherwise we 
              are risking ending up in flipping degeneracy
                           d                          d
                           /\                        /|\
                          /  \                      / | \
                         /    \                    /  |  \
                        /      \                  /   |   \ 
                       /   t0   \                /    |    \
                      /          \              /     |     \ 
                     /            \            /      |      \
                  a +--------------+ c      a +   t0  |  t1   + c
                     \            /            \      |      /
                      \          /              \     |     /
                       \   t1   /                \    |    /
                        \      /                  \   |   / 
                         \    /                    \  |  /
                          \  /                      \ | /
                           \/                        \|/
                            b                         b
             */

            Triangle acd = triangle;
            Triangle cba = _triangles[adj];

            int ac = edge;
            int cd = NEXT[ac];
            int da = PREV[ac];

            int ca = cba.IndexOf(cd, ac);
            int ab = NEXT[ca];
            int bc = PREV[ca];

            Node a = _nodes[acd.indices[ac]];
            Node b = _nodes[cba.indices[bc]];
            Node c = _nodes[acd.indices[cd]];
            Node d = _nodes[acd.indices[da]];

            int t0 = acd.index;
            int t1 = cba.index;

            Triangle new0 = new Triangle(t0, b, d, a);
            Triangle new1 = new Triangle(t1, d, b, c);

            new0.adjacent[0] = t1;
            new0.adjacent[1] = acd.adjacent[da];
            new0.adjacent[2] = cba.adjacent[ab];

            new1.adjacent[0] = t0;
            new1.adjacent[1] = cba.adjacent[bc];
            new1.adjacent[2] = acd.adjacent[cd];

            bool constrained = cba.constrained[ac];
            new0.constrained[0] = new1.constrained[1] = constrained;
        }
    }

    public struct TriangleWalker
    {
        readonly List<Triangle> _triangles;
        readonly int _start;
        readonly int _vertex;
        int _current;

        public int Current => _current;

        public TriangleWalker(List<Triangle> triangles, int triangleIndex, int globalVertexIndex)
        {
            _triangles = triangles;
            _vertex = globalVertexIndex;
            _current = _start = triangleIndex;
        }

        public bool MoveNextCCW()
        {
            Triangle tri = _triangles[_current];
            int next = tri.adjacent[tri.IndexOf(_vertex)];
            if (next == _start) return false;
            _current = next;
            return true;
        }

        public bool MoveNextCW()
        {
            Triangle tri = _triangles[_current];
            int indexOfVertex = tri.IndexOf(_vertex);
            if (indexOfVertex == -1)
            {
                return false;
            }

            int next = tri.adjacent[Mesh.PREV[indexOfVertex]];
            if (next == _start) return false;
            _current = next;
            return true;
        }
    }
}
