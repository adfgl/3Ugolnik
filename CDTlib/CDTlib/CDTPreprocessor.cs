
namespace CDTlib
{
    public class CDTPreprocessor
    {
        readonly CDTInput _input;
        readonly List<Node> _contourPoints, _constraintPoints;
        readonly List<Constraint> _constraintSegments;
        readonly List<(Polygon, List<Polygon>)> _polygons;
        readonly Rectangle _rectangle;

        public CDTPreprocessor(CDTInput input)
        {
            _input = input;

            _polygons = new List<(Polygon, List<Polygon>)>();

            _contourPoints = new List<Node>();
            _constraintPoints = new List<Node>();
            _constraintSegments = new List<Constraint>();

            Rectangle rectangle = Rectangle.Empty;
            foreach (CDTPolygon polygon in input.Polygons)
            {
                List<Constraint> polygonConstraints = new List<Constraint>();

                Polygon contour = new Polygon(polygon.GetPoints());
                List<Polygon> holes = ExtractHoles(contour, polygon.Holes);
                _polygons.Add((contour, holes));

                rectangle = rectangle.Union(contour.Rect);

                ExtractConstraints(polygonConstraints, EConstraint.Contour, contour);
                foreach (Polygon hole in holes)
                {
                    ExtractConstraints(polygonConstraints, EConstraint.Hole, hole);
                }

                foreach (CDTSegment segment in input.Segments)
                {
                    foreach (CDTSegment seg in segment.Split())
                    {
                        CDTPoint start = seg.Start;
                        CDTPoint end = seg.End;

                        Constraint constraint = new Constraint(new Node(-1, start.X, start.Y, 0), new Node(-1, end.X, end.Y, 0), EConstraint.User);
                        AddConstraint(polygonConstraints, constraint);
                    }
                }

                foreach (CDTPoint pt in input.Points)
                {
                    if (Polygon.Contains(contour, holes, pt.X, pt.Y))
                    {
                        _constraintPoints.Add(new Node(-1, pt.X, pt.Y, 0));
                    }
                }

                foreach (Constraint item in polygonConstraints)
                {
                    if (ValidConstraint(item, contour, holes))
                    {
                        AddConstraint(_constraintSegments, item);
                    }
                }
            }
            _rectangle = rectangle;
        }

        public CDTInput Input => _input;

        public List<(Polygon, List<Polygon>)> Polygons => _polygons;
        public List<Node> ContourPoints => _contourPoints;
        public List<Node > ConstraintPoints => _constraintPoints;
        public List<Constraint> ConstraintSegments => _constraintSegments;
        public Rectangle Rectangle => _rectangle;

        static void AddConstraint(List<Constraint> constraints, Constraint constraint)
        {
            Stack<Constraint> toProcess = new Stack<Constraint>();
            toProcess.Push(constraint); 

            while (toProcess.Count > 0)
            {
                Constraint current = toProcess.Pop();

                bool split = false;
                for (int i = constraints.Count - 1; i >= 0; i--)
                {
                    Constraint existing = constraints[i];

                    List<Constraint> segments = existing.Split(current);
                    if (segments.Count == 1)
                    {
                        continue;
                    }

                    constraints.RemoveAt(i);
                    foreach (Constraint item in segments)
                    {
                        toProcess.Push(item);
                    }

                    split = true;
                    break;
                }

                if (!split)
                {
                    constraints.Add(current);
                }
            }
        }

        static bool ValidConstraint(Constraint constraint, Polygon contour, List<Polygon> holes)
        {
            double x = (constraint.a.X + constraint.b.X) * 0.5;
            double y = (constraint.a.Y + constraint.b.Y) * 0.5;

            switch (constraint.type)
            {
                case EConstraint.User:
                    return Polygon.Contains(contour, holes, x, y);

                case EConstraint.Contour:
                    foreach (var item in holes)
                    {
                        if (item.Contains(x, y))
                        {
                            return false;
                        }
                    }
                    break;

                case EConstraint.Hole:
                    return contour.Contains(x, y);

                default:
                    throw new NotImplementedException($"Unknown constraint type '{constraint.type}'.");
            }
            return false;
        }

        static List<Polygon> ExtractHoles(Polygon contour, List<CDTPolygon> holes)
        {
            List<Polygon> items = new List<Polygon>(holes.Count);
            foreach (CDTPolygon item in holes)
            {
                Polygon hole = new Polygon(item.GetPoints());
                if (!contour.Contains(hole) && !contour.Intersects(hole))
                {
                    continue;
                }

                bool shouldProcess = true;
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    Polygon existing = items[i];
                    if (existing.Contains(hole))
                    {
                        shouldProcess = false;
                        break;
                    }

                    if (hole.Contains(existing))
                    {
                        items.RemoveAt(i);
                    }
                }

                if (shouldProcess)
                {
                    items.Add(hole);
                }
            }
            return items;
        }

        void ExtractConstraints(List<Constraint> constraints, EConstraint type, Polygon polygon)
        {
            int num = polygon.Nodes.Count;
            for (int i = 0; i < num - 1; i++)
            {
                Constraint constraint = new Constraint(polygon.Nodes[i], polygon.Nodes[i + 1], type);
                AddConstraint(constraints, constraint);
            }
        }
    }

    public readonly struct Constraint
    {
        public readonly Node a, b;
        public readonly EConstraint type;

        public Constraint(Node a, Node b, EConstraint type)
        {
            this.a = a;
            this.b = b;
            this.type = type;
        }

        static bool CloseOrEqual(Node a, Node b, double eps = 1e-6)
        {
            if (a == b) return true;
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return dx * dx + dy * dy < eps;
        }

        public List<Constraint> Split(Node other)
        {
            if (CloseOrEqual(a, other) || CloseOrEqual(b, other))
            {
                return [this];
            }
            return [new Constraint(a, other, type), new Constraint(other, b, type)];
        }

        public List<Constraint> Split(Constraint other)
        {
            Node? inter = Node.Intersect(a, b, other.a, other.b);
            if (inter == null ||
                CloseOrEqual(a, other.a) || CloseOrEqual(b, other.b) ||
                CloseOrEqual(a, other.b) || CloseOrEqual(b, other.a))
            {
                return [this];
            }

            List<Constraint> segments = Split(inter);
            segments.AddRange(other.Split(inter));
            return segments;
        }
    }
}
