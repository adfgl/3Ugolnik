
namespace CDTSharp
{
    public class HeMesh
    {
        readonly List<HeTriangle> _triangles;
        readonly List<HeNode> _nodes;
        readonly QuadTree _qt;

        public HeNode Add(double x, double y)
        {
            object? element = FindContaining(x, y);
            if (element == null)
            {
                throw new Exception($"Point [{x} {y}] is outside of domain.");
            }

            if (element is HeNode)
            {
                return (HeNode)element;
            }

            HeNode node = new HeNode(_nodes.Count, x, y);

            HeTriangle[] newTriangles = element switch
            {
                HeEdge edge => Split(edge, node),
                HeTriangle tri => Split(tri, node),
                _ => throw new Exception("Invalid mesh element returned.")
            };

            // do something before adding?

            Add(node, newTriangles);
            return node;
        }

        public List<HeEdge> Add(double x0, double y0, double x1, double y1)
        {
            Queue<(HeNode, HeNode)> toInsert = new Queue<(HeNode, HeNode)>();
            toInsert.Enqueue((Add(x0, y0), Add(x1, y1)));

            List<HeEdge> segments = new List<HeEdge>();
            while (toInsert.Count > 0)
            {
                var (a, b) = toInsert.Dequeue();
                if (a.Index == b.Index)
                {
                    continue;
                }

                HeEdge segment = InsertConstraintSegment(a, b, toInsert);
                segments.Add(segment);
            }
            return segments;
        }

        HeEdge InsertConstraintSegment(HeNode start, HeNode end, Queue<(HeNode, HeNode)> toInsert)
        {
            while (true)
            {
                HeEdge? existing = FindEdge(start, end, true);
                if (existing is not null)
                {
                    existing.SetConstraint(true);
                    return existing;
                }

                HeTriangle entrance = Entrance(start, end);
                while (true)
                {
                  
                }
            }
        }

        bool WalkAndInsert(HeTriangle triangle, HeNode start, HeNode end, Queue<(HeNode, HeNode)> toInsert)
        {
            HeTriangle current = triangle;
            while (true)
            {
                foreach (HeEdge edge in current.Forward())
                {
                    if (TrySplitOrFlip(edge, start, end, toInsert))
                    {
                        return true;
                    }
                }
            }
        }

        bool TrySplitOrFlip(HeEdge edge, HeNode start, HeNode end, Queue<(HeNode, HeNode)> toInsert)
        {
            var (a, b) = edge;
            if (a == start || b == start || a == end || b == end)
            {
                return false;
            }

            if (!GeometryHelper.Intersect(
                a.X, a.Y, b.X, b.Y, 
                start.X, start.Y, end.X, end.Y, 
                out double x, out double y))
            {
                return false;
            }

            if (CanFlip(edge))
            {
                HeTriangle[] tris = Flip(edge);
                Add(tris);
            }
            else
            {
                HeNode node = new HeNode(_nodes.Count, x, y);
                HeTriangle[] tris = Split(edge, node);
                Add(node, tris);
            }
            return true;
        }

        public HeTriangle Entrance(HeNode a, HeNode b)
        {
            foreach (HeEdge aEdge in a.Around())
            {
                int count = 0;
                foreach (HeEdge edge in aEdge.Triangle.Forward())
                {
                    var (start, end) = edge;

                    if (a == start || b == end)
                    {
                        double cross = GeometryHelper.Cross(start.X, start.Y, end.X, end.Y, b.X, b.Y);
                        if (cross >= 0)
                        {
                            count++;
                        }
                    }

                    if (count == 2)
                    {
                        return aEdge.Triangle;
                    }
                }
            }

            throw new Exception("Could not find entrance triangle.");
        }

        public void Add(HeNode node, HeTriangle[] triangles)
        {
            _nodes.Add(node);
            _qt.Add(node);

            Add(triangles);
        }

        public List<HeTriangle> Legalize(HeTriangle[] triangles)
        {
            List<HeTriangle> affected = new List<HeTriangle>();

            Stack<HeTriangle> toLegalize = new Stack<HeTriangle>(triangles);
            while (toLegalize.Count > 0)
            {
                HeTriangle triangle = toLegalize.Pop();
                HeEdge edge = triangle.Edge;

                affected.Add(triangle);

                if (!CanFlip(edge) || !CanFlip(edge))
                {
                    continue;
                }

                HeTriangle[] flipped = Flip(edge);
                Add(flipped);

                foreach (HeTriangle t in flipped)
                {
                    toLegalize.Push(t);
                }
            }
            return affected;
        }

        public object? FindContaining(double x, double y, double eps = 1e-6, int searchStart = -1)
        {
            if (_triangles.Count == 0) return null;

            int maxSteps = _triangles.Count * 3;
            int trianglesChecked = 0;

            HeTriangle current = _triangles.Last();
            if (searchStart < 0 || searchStart >= _triangles.Count)
            {
                current = _triangles[searchStart];
            }

            HeEdge? skipEdge = null;
            while (true)
            {
                if (trianglesChecked++ > maxSteps)
                {
                    throw new Exception("FindContaining exceeded max steps. Likely invalid topology.");
                }

                HeEdge? bestExit = null;
                double worstCross = 0;
                bool inside = true;
                foreach (HeEdge edge in current.Forward())
                {
                    if (edge == skipEdge)
                    {
                        continue;
                    }

                    var (a, b) = edge;
                    if (a.Close(x, y, eps))
                    {
                        return a;
                    }

                    if (b.Close(x, y, eps))
                    {
                        return b;
                    }

                    double cross = GeometryHelper.Cross(a.X, a.Y, b.X, b.Y, x, y);
                    if (Math.Abs(cross) <= eps)
                    {
                        double dx = b.X - a.X;
                        double dy = b.Y - a.Y;
                        double dot = (x - a.X) * dx + (y - a.Y) * dy;
                        double lenSq = dx * dx + dy * dy;

                        if (dot >= -eps && dot <= lenSq + eps)
                        {
                            return edge;
                        }
                    }

                    if (cross < 0)
                    {
                        inside = false;
                        if (bestExit is null || cross < worstCross)
                        {
                            worstCross = cross;
                            bestExit = edge;
                        }
                    }
                }

                if (inside)
                {
                    return current;
                }

                if (bestExit is null || bestExit.Twin is null)
                {
                    return null;
                }

                skipEdge = bestExit.Twin;
                current = bestExit.Triangle;
            }
        }

        public HeEdge? FindEdge(HeNode a, HeNode b, bool invariant)
        {
            foreach (HeEdge e in a.Around())
            {
                var (start, end) = e;
                if (start == a && end == b)
                {
                    return e;
                }

                if (invariant && start == b && end == a)
                {
                    return e.Twin;
                }
            }
            return null;
        }

        public HeEdge? FindEdgeBrute(HeNode a, HeNode b)
        {
            foreach (HeTriangle t in _triangles)
            {
                foreach (HeEdge e in t.Forward())
                {
                    var (start, end) = e;
                    if (start == a && end == b)
                    {
                        return e;
                    }
                }
            }
            return null;
        }

        public void Add(HeTriangle[] triangles)
        {
            foreach (HeTriangle t in triangles)
            {
                int index = t.Index;
                if (index < _triangles.Count)
                {
                    HeTriangle old = _triangles[index];
                    old.Edges(out HeEdge ab, out HeEdge bc, out HeEdge ca);

                    HeNode a = ab.Origin;
                    HeNode b = bc.Origin;
                    HeNode c = ca.Origin;

                    old.Area = -1;

                    if (ab.Twin is not null && ab.Twin.Twin == ab) ab.Twin.Twin = null;
                    if (bc.Twin is not null && bc.Twin.Twin == bc) bc.Twin.Twin = null;
                    if (ca.Twin is not null && ca.Twin.Twin == ca) ca.Twin.Twin = null;

                    old.Edge = null!;
                    ab.Triangle = bc.Triangle = ca.Triangle = null!;

                    if (a.Edge == ab) a.Edge = null!;
                    if (b.Edge == bc) b.Edge = null!;
                    if (c.Edge == ca) c.Edge = null!;

                    _triangles[index] = t;
                }
                else
                {
                    _triangles.Add(t);
                }
            }

            foreach (HeTriangle t in triangles)
            {
                t.Edges(out HeEdge ab, out HeEdge bc, out HeEdge ca);

                HeNode a = ab.Origin;
                HeNode b = bc.Origin;
                HeNode c = ca.Origin;

                a.Edge = ab;
                b.Edge = bc;
                c.Edge = ca;

                HeEdge? twin = t.Edge.Twin;
                if (twin is not null)
                {
                    twin.Twin = t.Edge;
                }
            }

            Legalize(triangles);
        }

        public bool CanFlip(HeEdge edge)
        {
            if (edge.Constrained || edge.Twin is null)
            {
                return false;
            }

            var (a, c) = edge;
            HeNode d = edge.Prev.Origin;
            HeNode b = edge.Twin.Prev.Origin;

            return GeometryHelper.QuadConvex(a, b, c, d);
        }

        public bool ShouldFlip(HeEdge edge)
        {
            HeNode opposite = edge.Twin!.Prev.Origin;
            return edge.Triangle.Circle.Contains(opposite.X, opposite.Y);
        }

        public HeTriangle[] Flip(HeEdge edge)
        {
            HeEdge? twin = edge.Twin;
            if (twin is null || edge.Constrained)
            {
                return [];
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

            HeTriangle old0 = edge.Triangle;
            HeTriangle old1 = twin.Triangle;

            HeEdge cd = edge.Next;
            HeEdge da = cd.Next;
            HeEdge ab = twin.Next;
            HeEdge bc = ab.Next;

            var (a, c) = edge;
            HeNode d = da.Origin;
            HeNode b = bc.Origin;

            HeTriangle new0 = new HeTriangle(old0.Index, a, b, d);
            HeTriangle new1 = new HeTriangle(old1.Index, b, c, d, old0.Area + old1.Area - new0.Area);

            new0.Edge.CopyProperties(ab);
            new0.Edge.Prev.CopyProperties(da);
            new1.Edge.CopyProperties(bc);
            new1.Edge.Next.CopyProperties(cd);

            new0.Edge.Next.SetTwin(new1.Edge.Prev);

            return [new0, new1];
        }

        public HeTriangle[] Split(HeEdge edge, HeNode node)
        {
            HeEdge? twin = edge.Twin;
            if (twin is null)
            {
                return SplitNoTwin(edge, node);
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

            HeTriangle old0 = edge.Triangle;
            HeTriangle old1 = twin.Triangle;

            HeEdge cd = edge.Next;
            HeEdge da = cd.Next;
            HeEdge ab = twin.Next;
            HeEdge bc = ab.Next;

            var (a, c) = edge;
            HeNode d = da.Origin;
            HeNode b = bc.Origin;
            HeNode e = node;

            int baseIndex = _triangles.Count;
            HeTriangle new0 = new HeTriangle(old0.Index, d, a, e);
            HeTriangle new1 = new HeTriangle(old1.Index, c, d, e, old0.Area - new0.Area);
            HeTriangle new2 = new HeTriangle(baseIndex, b, c, e);
            HeTriangle new3 = new HeTriangle(baseIndex + 1, a, b, e, old1.Area - new2.Area);

            new0.Edge.CopyProperties(da);
            new1.Edge.CopyProperties(cd);
            new2.Edge.CopyProperties(bc);
            new3.Edge.CopyProperties(ab);

            new0.Edge.Next.SetTwin(new1.Edge.Next);
            new1.Edge.Next.SetTwin(new2.Edge.Next);
            new2.Edge.Next.SetTwin(new3.Edge.Next);
            new3.Edge.Next.SetTwin(new0.Edge.Next);

            if (edge.Constrained)
            {
                new0.Edge.Next.SetConstraint(true);
                new1.Edge.Prev.SetConstraint(true);
            }
            return [new0, new1, new2, new3];
        }

        public HeTriangle[] SplitNoTwin(HeEdge edge, HeNode node)
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

            HeTriangle old0 = edge.Triangle;

            HeEdge ab = old0.Edge;
            HeEdge bc = ab.Next;
            HeEdge ca = bc.Next;

            HeNode a = ab.Origin;
            HeNode b = bc.Origin;
            HeNode c = ca.Origin;
            HeNode d = node;

            HeTriangle new0 = new HeTriangle(old0.Index, c, a, d);
            HeTriangle new1 = new HeTriangle(_triangles.Count, b, c, d);

            new0.Edge.Prev.SetTwin(new1.Edge.Next);
            
            new0.Edge.CopyProperties(ca.Twin);
            new1.Edge.CopyProperties(bc.Twin);

            if (edge.Constrained)
            {
                new0.Edge.Next.Constrained = new1.Edge.Prev.Constrained = true;
            }

            return [new0, new1];
        }

        public HeTriangle[] Split(HeTriangle triangle, HeNode node)
        {
            /*
                        *C



                   /           ^
                  ^      *D     \



              A*        ->         *B

            */

            HeEdge ab = triangle.Edge;
            HeEdge bc = ab.Next;
            HeEdge ca = bc.Next;

            HeNode a = ab.Origin;
            HeNode b = bc.Origin;
            HeNode c = ca.Origin;
            HeNode d = node;

            HeTriangle new0 = new HeTriangle(triangle.Index, a, b, d);
            HeTriangle new1 = new HeTriangle(_triangles.Count, b, c, d);
            HeTriangle new2 = new HeTriangle(_triangles.Count + 1, c, a, d, triangle.Area - new0.Area - new1.Area);

            new0.Edge.CopyProperties(ab);
            new1.Edge.CopyProperties(bc);
            new2.Edge.CopyProperties(ca);

            new0.Edge.Next.SetTwin(new1.Edge.Prev);
            new1.Edge.Next.SetTwin(new2.Edge.Prev);
            new2.Edge.Next.SetTwin(new0.Edge.Prev);

            return [new0, new1, new2];
        }
    }

    public class HeNode : INode
    {
        public HeNode(int index, double x, double y)
        {
            Index = index;
            X = x;
            Y = y;
            Edge = null!;
        }

        public void Deconstruct(out double x, out double y)
        {
            x = X; y = Y;
        }

        public int Index { get; }
        public HeEdge Edge { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public bool Close(double x, double y, double eps = 1e-6)
        {
            double dx = x - X;
            double dy = y - Y;
            return dx * dx + dy * dy < eps;
        }

        public IEnumerable<HeEdge> Around()
        {
            HeEdge start = Edge;
            HeEdge current = Edge;
            do
            {
                yield return current;
                current = current.Prev.Twin!;
            } while (current != null && current != start);
        }
    }

    public class HeEdge
    {
        public HeEdge(HeNode origin)
        {
            Origin = origin;
        }

        public void Deconstruct(out HeNode start, out HeNode end)
        {
            start = Origin;
            end = Next.Origin;
        }

        public HeNode Origin { get; }
        public HeEdge Next { get; set; } = null!;
        public HeEdge Prev => Next.Next;
        public HeEdge? Twin { get; set; } = null;
        public HeTriangle Triangle { get; set; } = null!;
        public bool Constrained { get; set; } = false;

        public EOrientation Orientation(double x, double y, double eps = 0)
        {
            var (sx, sy) = Origin;
            var (ex, ey) = Next.Origin;

            double cross = GeometryHelper.Cross(sx, sy, ex, ey, x, y);
            if (Math.Abs(cross) <= eps)
            {
                return EOrientation.Colinear;
            }
            return cross > 0 ? EOrientation.Left : EOrientation.Right;
        }

        public void CopyProperties(HeEdge? twin)
        {
            Twin = twin;
            if (twin is not null)
            {
                Constrained = twin.Constrained;
            }
        }

        public void SetTwin(HeEdge? twin)
        {
            Twin = twin;
            if (twin is not null)
            {
                twin.Twin = this;
            }
        }

        public void SetConstraint(bool value)
        {
            Constrained = value;
            if (Twin is not null)
            {
                Twin.Constrained = value;
            }
        }
    }

    public class HeTriangle
    {
        public HeTriangle(int index, HeNode a, HeNode b, HeNode c, double? area = null)
        {
            Index = index;

            HeEdge ab = new HeEdge(a);
            HeEdge bc = new HeEdge(b);
            HeEdge ca = new HeEdge(c);

            a.Edge = ab;
            b.Edge = bc;
            c.Edge = ca;

            Edge = ab;
            ab.Triangle = bc.Triangle = ca.Triangle = this;

            ab.Next = bc;
            bc.Next = ca;
            ca.Next = ab;

            Circle = new Circle(a.X, a.Y, b.X, b.Y, c.X, c.Y);
            Area = area.HasValue ? area.Value : GeometryHelper.Cross(a.X, a.Y, b.X, b.Y, c.X, c.Y) * 0.5;
        }

        public void Nodes(out HeNode a, out HeNode b, out HeNode c)
        {
            a = Edge.Origin;
            b = Edge.Next.Origin;
            c = Edge.Next.Next.Origin;
        }

        public void Edges(out HeEdge ab, out HeEdge bc, out HeEdge ca)
        {
            ab = Edge;
            bc = ab.Next;
            ca = bc.Next;
        }

        public int Index { get; }
        public HeEdge Edge { get; set; }
        public Circle Circle { get; set; }
        public double Area { get; set; }

        public IEnumerable<HeEdge> Forward()
        {
            HeEdge he = Edge;
            HeEdge current = he;
            do
            {
                yield return current;
                current = current.Next;
            } while (current != he);
        }

        public IEnumerable<HeEdge> Backward()
        {
            HeEdge he = Edge;
            HeEdge current = he;
            do
            {
                yield return current;
                current = current.Prev;
            } while (current != he);
        }
    }
}
