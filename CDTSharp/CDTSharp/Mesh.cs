using CDTGeometryLib;

namespace CDTSharp
{
    public class Mesh
    {
        readonly List<HeTriangle> _triangles;
        readonly QuadTree _qt;
        readonly Rectangle _bounds;

        public Mesh(ClosedPolygon? polygon, List<(Node a, Node b)>? constraintEdges = null, List<Node>? costraintPoints = null)
        {
            List<(Node a, Node b)> conEdges = new List<(Node a, Node b)>();
            List<Node> conPoints = new List<Node>();

            double minX, minY, maxX, maxY;
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;

            void process(Node node)
            {
                double x = node.X;
                double y = node.Y;
                if (minX > x) minX = x;
                if (minY > y) minY = y;
                if (maxX < x) maxX = x;
                if (maxY < y) maxY = y;
            }

            void processPoly(ClosedPolygon polygon)
            {
                for (int i = 0; i < polygon.Points.Count - 1; i++)
                {
                    Node a = polygon.Points[i];
                    Node b = polygon.Points[i + 1];

                    process(a);
                    conEdges.Add((a, b));
                }
            }

            if (polygon is not null)
            {
                processPoly(polygon);
                foreach (ClosedPolygon hole in polygon.Holes)
                {
                    processPoly(hole);
                }
            }

            if (constraintEdges is not null)
            {
                foreach ((Node a, Node b) in constraintEdges)
                {
                    process(a);
                    process(b);
                    conEdges.Add((a, b));
                }
            }

            if (costraintPoints is not null)
            {
                foreach (Node node in costraintPoints)
                {
                    process(node);
                    conPoints.Add(node);
                }
            }

            _bounds = new Rectangle(minX, minY, maxX, maxY);
            _qt = new QuadTree(_bounds);
            _triangles = new List<HeTriangle>();

            AddSuperStructure(_bounds, 5);

            foreach ((Node a, Node b) in conEdges)
            {
                List<HeEdge> added = Add(a.X, a.Y, b.X, b.Y);
            }

            foreach (Node node in conPoints)
            {
                HeNode added = Add(node.X, node.Y, out _);
            }

            if (polygon is not null)
            {
                foreach (HeTriangle item in _triangles)
                {
                    item.Center(out double x, out double y);
                    item.Hole = !polygon.Contains(x, y);
                }
            }
        }

        Mesh AddSuperStructure(Rectangle bounds, double scale)
        {
            double dmax = Math.Max(bounds.maxX - bounds.minX, bounds.maxY - bounds.minY);
            double midx = (bounds.maxX + bounds.minX) * 0.5;
            double midy = (bounds.maxY + bounds.minY) * 0.5;
            double size = Math.Max(scale, 2) * dmax;

            HeNode a = new HeNode(-3, midx - size, midy - size);
            HeNode b = new HeNode(-2, midx + size, midy - size);
            HeNode c = new HeNode(-1, midx, midy + size);

            HeTriangle triangle = new HeTriangle(0, a, b, c);
            _triangles.Add(triangle);

            return this;
        }

        public Mesh RemoveSuperStructure()
        {
            foreach (HeTriangle tri in _triangles)
            {
                bool discard = tri.Hole || tri.ContainsSuper();
                if (!discard) continue;

                tri.Removed = true;
                foreach (HeEdge edge in tri.Forward())
                {
                    HeNode origin = edge.Origin;
                    if (origin.Edge != edge)
                    {
                        continue;
                    }

                    foreach (HeEdge item in origin.Around())
                    {
                        if (item.Triangle.Removed) continue;

                        origin.Edge = item;
                        break;
                    }
                }
            }

            int write = 0;
            int count = 0;
            for (int read = 0; read < _triangles.Count; read++)
            {
                HeTriangle tri = _triangles[read];

                if (tri.Removed)
                {
                    foreach (HeEdge edge in tri.Forward())
                    {
                        if (edge.Twin is not null)
                        {
                            edge.Twin.Twin = null;
                            edge.Twin = null;
                        }
                        edge.Triangle = null!;
                    }
                }
                else
                {
                    tri.Index = count++;
                    _triangles[write++] = tri;
                }
            }

            if (write < _triangles.Count)
            {
                _triangles.RemoveRange(write, _triangles.Count - write);
            }

            return this;
        }

        public Mesh Refine(Quality quality)
        {
            HashSet<Constraint> seen = new HashSet<Constraint>();

            Queue<HeTriangle> triangleQueue = new Queue<HeTriangle>();
            Queue<Constraint> segmentQueue = new Queue<Constraint>();
            foreach (HeTriangle t in _triangles)
            {
                if (Bad(t, quality))
                {
                    triangleQueue.Enqueue(t);
                }

                foreach (HeEdge e in t.Forward())
                {
                    if (!e.Constrained)
                    {
                        continue;
                    }

                    Constraint segment = new Constraint(e.Origin, e.Next.Origin, EConstraint.Undefined);
                    if (seen.Add(segment) && segment.Enchrouched(_qt))
                    {
                        segmentQueue.Enqueue(segment);
                    }
                }
            }

            while (segmentQueue.Count > 0 || triangleQueue.Count > 0)
            {
                if (segmentQueue.Count > 0)
                {
                    Constraint seg = segmentQueue.Dequeue();
                    HeEdge? edge = FindEdge((HeNode)seg.a, (HeNode)seg.b, true);
                    if (edge is null)
                    {
                        continue;
                    }

                    HeNode node = new HeNode(_qt.Count, seg.circle.x, seg.circle.y);
                    HeTriangle[] tris = Split(edge, node);
                    List<HeTriangle> affected = Add(node, tris);
                    foreach (HeTriangle t in affected)
                    {
                        triangleQueue.Enqueue(t);
                    }

                    seen.Remove(seg);
                    foreach (Constraint e in seg.Split(node))
                    {
                        seen.Add(e);
                        if (e.Enchrouched(_qt) && e.VisibleFromInterior(seen, node.X, node.Y))
                        {
                            segmentQueue.Enqueue(e);
                        }
                    }
                }

                if (triangleQueue.Count > 0)
                {
                    HeTriangle t = triangleQueue.Dequeue();
                    if (!Bad(t, quality))
                    {
                        continue;
                    }

                    double x = t.Circle.x;
                    double y = t.Circle.y;
                    if (!_bounds.Contains(x, y))
                    {
                        continue;
                    }

                    bool encroaches = false;
                    foreach (Constraint seg in seen)
                    {
                        if (seg.circle.Contains(x, y) && seg.VisibleFromInterior(seen, x, y))
                        {
                            segmentQueue.Enqueue(seg);
                            encroaches = true;
                        }
                    }

                    if (!encroaches)
                    {
                        HeNode node = Add(x, y, out List<HeTriangle> affected);
                        foreach (HeTriangle item in affected)
                        {
                            triangleQueue.Enqueue(item);
                        }
                    }
                }
            }
            return this;
        }

        public bool Bad(HeTriangle t, Quality q)
        {
            if (t.Removed)
            {
                return false;
            }

            double edgeLenLimSqr = q.MaxEdgeLength;
            edgeLenLimSqr *= edgeLenLimSqr;

            double minEdgeSqr = double.MaxValue;
            foreach (HeEdge e in t.Forward())
            {
                if (e.Origin.Index < 0)
                {
                    return false;
                }

                double lenSqr = e.SquareLength();
                if (lenSqr > edgeLenLimSqr)
                {
                    return true;
                }

                if (minEdgeSqr > lenSqr)
                {
                    minEdgeSqr = lenSqr;
                }

                double rad = GeometryHelper.Angle(e.Prev.Origin, e.Origin, e.Next.Origin);
                if (rad > q.MinAngle)
                {
                    return true;
                }
            }

            if (t.Area() > q.MaxArea)
            {
                return true;
            }
            return t.Circle.radiusSqr / minEdgeSqr > 2;
        }

        public HeNode Add(double x, double y, out List<HeTriangle> affected)
        {
            object? element = FindContaining(x, y);
            if (element == null)
            {
                throw new Exception($"Point [{x} {y}] is outside of domain.");
            }

            if (element is HeNode)
            {
                affected = new List<HeTriangle>();
                return (HeNode)element;
            }

            HeNode node = new HeNode(_qt.Count, x, y);

            HeTriangle[] newTriangles = element switch
            {
                HeEdge edge => Split(edge, node),
                HeTriangle tri => Split(tri, node),
                _ => throw new Exception("Invalid mesh element returned.")
            };

            // do something before adding?

            affected = Add(node, newTriangles);
            return node;
        }

        public List<HeEdge> Add(double x0, double y0, double x1, double y1)
        {
            Queue<(HeNode, HeNode)> toInsert = new Queue<(HeNode, HeNode)>();
            toInsert.Enqueue((Add(x0, y0, out _), Add(x1, y1, out _)));

            List<HeEdge> segments = new List<HeEdge>();
            while (toInsert.Count > 0)
            {
                var (start, end) = toInsert.Dequeue();
                if (start.Index == end.Index)
                {
                    continue;
                }

                HeEdge? existing = FindEdge(start, end, true);
                if (existing is not null)
                {
                    existing.SetConstraint(true);
                    segments.Add(existing);
                    continue;
                }

                while (true)
                {
                    HeTriangle entrance = Entrance(start, end);
                    if (WalkAndInsert(entrance, start, end, toInsert))
                    {
                        break;
                    }
                }
            }
            return segments;
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

                HeEdge exit = FindExitEdge(current, end);
                if (exit.Twin is null)
                {
                    throw new Exception("Constraint insertion failed: no adjacent triangle during walk.");
                }
                current = exit.Twin.Triangle;
            }
        }

        HeEdge FindExitEdge(HeTriangle triangle, HeNode target)
        {
            var (x, y) = target;
            HeEdge exit = null!;
            double bestCross = 0;
            foreach (HeEdge edge in triangle.Forward())
            {
                double cross = edge.Orientation(x, y);
                if (exit is null || cross < bestCross)
                {
                    bestCross = cross;
                    exit = edge;
                }
            }
            return exit;
        }

        bool TrySplitOrFlip(HeEdge edge, HeNode start, HeNode end, Queue<(HeNode, HeNode)> toInsert)
        {
            if (edge.Contains(start) || edge.Contains(end))
            {
                return false;
            }

            var (a, b) = edge;
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
                HeNode node = new HeNode(_qt.Count, x, y);
                HeTriangle[] tris = Split(edge, node);

                toInsert.Enqueue((start, node));
                toInsert.Enqueue((node, end));
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
                        double cross = edge.Orientation(b.X, b.Y);
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

        public List<HeTriangle> Add(HeNode node, HeTriangle[] triangles)
        {
            _qt.Add(node);
            return Add(triangles);
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

                    double cross = edge.Orientation(x, y);
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

        public List<HeTriangle> Add(HeTriangle[] triangles)
        {
            foreach (HeTriangle t in triangles)
            {
                int index = t.Index;
                if (index < _triangles.Count)
                {
                    HeTriangle old = _triangles[index];
                    old.Removed = true;
                    old.Edges(out HeEdge ab, out HeEdge bc, out HeEdge ca);

                    HeNode a = ab.Origin;
                    HeNode b = bc.Origin;
                    HeNode c = ca.Origin;

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

            return Legalize(triangles);
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
            HeTriangle new1 = new HeTriangle(old1.Index, b, c, d);

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
            HeTriangle new1 = new HeTriangle(old1.Index, c, d, e);
            HeTriangle new2 = new HeTriangle(baseIndex, b, c, e);
            HeTriangle new3 = new HeTriangle(baseIndex + 1, a, b, e);

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
            HeTriangle new2 = new HeTriangle(_triangles.Count + 1, c, a, d);

            new0.Edge.CopyProperties(ab);
            new1.Edge.CopyProperties(bc);
            new2.Edge.CopyProperties(ca);

            new0.Edge.Next.SetTwin(new1.Edge.Prev);
            new1.Edge.Next.SetTwin(new2.Edge.Prev);
            new2.Edge.Next.SetTwin(new0.Edge.Prev);

            return [new0, new1, new2];
        }
    }
}
