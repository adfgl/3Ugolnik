namespace CDTSharp
{
    public class SplitResult
    {
        public Triangle[] Triangles { get; set; } = Array.Empty<Triangle>();
        public TriangleEdge[] ToLegalize {  get; set; } = Array.Empty<TriangleEdge>();
    }

    public class Mesh
    {
        public const int SUPER_INDEX = 3;
        
        public readonly static int[] NEXT = [1, 2, 0], PREV = [2, 0, 1];

        readonly List<Node> _nodes = new List<Node>();
        readonly List<Triangle> _triangles = new List<Triangle>();

        public List<Triangle> Triangles => _triangles;
        public List<Node> Nodes => _nodes;

        public void SetAdjacent(int triangle, int edge, int adjacent)
        {
            Triangle t = _triangles[triangle];
            t.adjacent[edge] = adjacent;

            if (adjacent != -1)
            {
                Triangle adj = _triangles[adjacent];
                t.Edge(edge, out int a, out int b);

                int adjEdge = adj.IndexOf(b, a);
                if (adjEdge == -1)
                {
                    throw new Exception($"{nameof(SetAdjacent)}: Broken topology!");
                }
                adj.adjacent[adjEdge] = triangle;
            }
        }

        public void SetConstraint(int triangle, int edge, bool value)
        {
            Triangle t = _triangles[triangle];
            t.constrained[edge] = value;

            int adjIndex = t.adjacent[edge];
            if (adjIndex == -1)
            {
                return;
            }

            t.Edge(edge, out int a, out int b);

            Triangle adj = _triangles[adjIndex];
            adj.constrained[adj.IndexOf(b, a)] = value;
        }

        public TriangleEdge FindEdge(int a, int b)
        {
            Node nodeA = _nodes[a];
            TriangleWalker walker = new TriangleWalker(_triangles, nodeA.Triangle, nodeA.Index);
            do
            {
                int triIndex = walker.Current;
                int e = _triangles[triIndex].IndexOf(a, b);
                if (e != -1)
                {
                    return new TriangleEdge(triIndex, e);
                }
            }
            while (walker.MoveNextCW());

            return new TriangleEdge();
        }

        public TriangleEdge FindEdgeBrute(int a, int b)
        {
            foreach (Triangle t in _triangles)
            {
                int e = t.IndexOf(a, b);
                if (e != -1)
                {
                    return new TriangleEdge(t.index, e);
                }
            }
            return new TriangleEdge();
        }

        public int[] Quad(Triangle triangle, int edge, out int twinEdge)
        {
            int adj = triangle.adjacent[edge];
            if (adj == -1)
            {
                twinEdge = -1;
                return [-1, -1, -1, -1];
            }
            /*
                     d             
                     /\            
                    /  \           
                   /    \          
                  /      \         
                 /        \        
                /          \       
               /            \      
            a +--------------+ c   
               \            /      
                \          /       
                 \        /        
                  \      /         
                   \    /          
                    \  /           
                     \/            
                     b             
         */

            Triangle acd = triangle;
            int a = acd.indices[edge];
            int c = acd.indices[NEXT[edge]];
            int d = acd.indices[PREV[edge]];

            Triangle cab = _triangles[adj];
            twinEdge = cab.IndexOf(c, a);
            int b = cab.indices[PREV[twinEdge]];

            return [a, b, c, d];
        }


        public SplitResult Split(Triangle triangle, int edge, Node node)
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

            bool constrained = triangle.constrained[edge];
            Triangle acd = triangle;
            Triangle cab = _triangles[adj];

            int t0 = triangle.index;
            int t1 = _triangles.Count;
            int t2 = _triangles.Count + 1;
            int t3 = adj;

            int[] abcd = Quad(triangle, edge, out int twinEdge);
            Node a = _nodes[abcd[0]];
            Node b = _nodes[abcd[1]];
            Node c = _nodes[abcd[2]];
            Node d = _nodes[abcd[3]];
            Node e = node;

            Triangle eda = new Triangle(t0, e, d, a, null, acd.parents);
            Triangle ecd = new Triangle(t1, e, c, d, acd.area - eda.area, acd.parents);
            Triangle ebc = new Triangle(t2, e, b, c, null, cab.parents);
            Triangle eab = new Triangle(t3, e, a, b, cab.area - ebc.area, cab.parents);

            int ac = edge;
            int cd = NEXT[ac];
            int da = PREV[ac];

            int ca = twinEdge;
            int ab = NEXT[ca];
            int bc = PREV[ca];

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

            eda.constrained[1] = acd.constrained[da];
            eda.constrained[2] = constrained;

            ecd.constrained[0] = constrained;
            ecd.constrained[1] = acd.constrained[cd];

            ebc.constrained[1] = cab.constrained[bc];
            ebc.constrained[2] = constrained;

            eab.constrained[0] = constrained;
            eab.constrained[1] = cab.constrained[ab];

            a.Triangle = d.Triangle = e.Triangle = t0;
            c.Triangle = t1;
            b.Triangle = t2;

            return new SplitResult()
            {
                Triangles = [eda, ecd, ebc, eab],
                ToLegalize = [new TriangleEdge(t0, 1), new TriangleEdge(t1, 1), new TriangleEdge(t2, 1), new TriangleEdge(t3, 1)]
            };
        }

        public SplitResult SplitNoAdjacent(Triangle triangle, int edge, Node node)
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
                                                    d
           */

            Triangle abc = triangle;

            Node a = _nodes[triangle.indices[0]];
            Node b = _nodes[triangle.indices[1]];
            Node c = _nodes[triangle.indices[2]];
            Node d = node;

            int t0 = triangle.index;
            int t1 = _triangles.Count;

            List<int> parents = triangle.parents;
            Triangle dca = new Triangle(t0, d, c, a, null, parents);
            Triangle cdb = new Triangle(t1, c, d, b, abc.area - dca.area, parents);

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

            a.Triangle = c.Triangle = d.Triangle = t0;
            b.Triangle = t1;

            return new SplitResult()
            {
                Triangles = [dca, cdb],
                ToLegalize = [new TriangleEdge(t0, 1), new TriangleEdge(t1, 2)]
            };
        }

        public SplitResult Split(Triangle triangle, Node node)
        {
            /*
                         *C
                        


                    /           ^
                   ^      *D     \

                
              
               A*        ->         *B
              
             */

            Triangle abc = triangle;
            Node a = _nodes[abc.indices[0]];
            Node b = _nodes[abc.indices[1]];
            Node c = _nodes[abc.indices[2]];
            Node d = node;

            int t0 = abc.index;
            int t1 = _triangles.Count;
            int t2 = t1 + 1;

            List<int> parents = abc.parents;
            Triangle dab = new Triangle(t0, d, a, b, null, parents);
            Triangle dbc = new Triangle(t1, d, b, c, null, parents);
            Triangle dca = new Triangle(t2, d, c, a, abc.area - dab.area - dbc.area, parents);

            dab.adjacent[0] = t2;
            dab.adjacent[1] = abc.adjacent[0];
            dab.adjacent[2] = t1;

            dbc.adjacent[0] = t0;
            dbc.adjacent[1] = abc.adjacent[1];
            dbc.adjacent[2] = t2;

            dca.adjacent[0] = t1;
            dca.adjacent[1] = abc.adjacent[2];
            dca.adjacent[2] = t0;

            dab.constrained[1] = abc.constrained[0];
            dbc.constrained[1] = abc.constrained[1];
            dca.constrained[1] = abc.constrained[2];

            a.Triangle = d.Triangle = t0;
            b.Triangle = t1;
            c.Triangle = t2;

            return new SplitResult()
            {
                Triangles = [dab, dbc, dca],
                ToLegalize = [new TriangleEdge(t0, 1), new TriangleEdge(t1, 1), new TriangleEdge(t2, 1)]
            };
        }
    }
}
