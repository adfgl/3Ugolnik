
namespace CDTlib
{
    public class CSG
    {
        public const double EPS = 1e-6;

        static List<Polygon> Operation(List<Polygon> aPoly, List<Polygon> bPoly, Func<Node, Node, Node> func)
        {
            Node a = new Node(aPoly);
            Node b = new Node(bPoly);
            Node ab = func(a, b);
            return ab.AllPolygons();
        }

        public static List<Polygon> Union(List<Polygon> a, List<Polygon> b)
        {
            return Operation(a, b, Node.Union);
        }
        public static List<Polygon> Subtract(List<Polygon> a, List<Polygon> b)
        {
            return Operation(a, b, Node.Subtract);
        }
        public static List<Polygon> Intersect(List<Polygon> a, List<Polygon> b)
        {
            return Operation(a, b, Node.Intersect);
        }

        public class Node
        {
            static readonly List<Polygon> s_tmpFront = new List<Polygon>();
            static readonly List<Polygon> s_tmpBack = new List<Polygon>();
            static readonly List<Polygon> s_tmpCoplanarFront = new List<Polygon>();
            static readonly List<Polygon> s_tmpCoplanarBack = new List<Polygon>();

            List<Polygon> _polygons = new List<Polygon>();
            Node? _front = null, _back = null;
            Plane _plane;

            public Node() { }

            public Node(List<Polygon> polygons)
            {
                Build(polygons, EPS);
            }

            public void Build(List<Polygon> polygons, double eps)
            {
                if (polygons.Count == 0) return;

                _plane = ChooseBestSplitPlane(polygons, eps);

                s_tmpFront.Clear();
                s_tmpBack.Clear();
                foreach (Polygon polygon in polygons)
                {
                    Split(_plane, polygon, _polygons, _polygons, s_tmpFront, s_tmpBack, eps);
                }

                if (s_tmpFront.Count > 0)
                {
                    if (_front is null)
                    {
                        _front = new Node();
                    }
                    _front.Build(s_tmpFront, eps);
                }

                if (s_tmpBack.Count > 0)
                {
                    if (_back is null)
                    {
                        _back = new Node();
                    }
                    _back.Build(s_tmpBack, eps);
                }
            }

            public void Invert()
            {
                for (int i = 0; i < _polygons.Count; i++)
                {
                    _polygons[i] = _polygons[i].Flip();

                }
                _plane = _plane.Flip();

                if (_front != null)
                {
                    _front.Invert();
                }

                if (_back != null)
                {
                    _back.Invert();
                }

                Node? tmp = this._front;
                this._front = this._back;
                this._back = tmp;
            }

            public Node Clone()
            {
                Node ret = new Node()
                {
                    _plane = _plane,
                    _polygons = _polygons.ToList()
                };

                if (_front != null)
                {
                    ret._front = _front.Clone();
                }

                if (_back != null)
                {
                    ret._back = _back.Clone();
                }
                return ret;
            }


            public void ClipTo(Node other)
            {
                _polygons = other.ClipPolygons(_polygons, EPS);

                if (_front != null)
                {
                    _front.ClipTo(other);
                }

                if (_back != null)
                {
                    _back.ClipTo(other);
                }
            }

            public List<Polygon> AllPolygons()
            {
                List<Polygon> list = _polygons;
                List<Polygon> front = new List<Polygon>();
                if (_front != null)
                {
                    front = _front.AllPolygons();
                }
                list.AddRange(front);

                List<Polygon> back = new List<Polygon>();
                if (_back != null)
                {
                    back = _back.AllPolygons();
                }

                list.AddRange(back);
                return list;
            }

            public List<Polygon> ClipPolygons(List<Polygon> polygons, double eps)
            {
                List<Polygon> front = new List<Polygon>();
                List<Polygon> back = new List<Polygon>();

                for (int i = 0; i < polygons.Count; i++)
                {
                    Split(_plane, polygons[i], front, back, front, back, eps);
                }

                if (_front != null)
                {
                    front = _front.ClipPolygons(front, eps);
                }

                if (_back != null)
                {
                    back = _back.ClipPolygons(back, eps);
                }
                else
                {
                    back.Clear();
                }

                front.AddRange(back);
                return front;
            }

            public static Node Union(Node a1, Node b1)
            {
                Node a = a1.Clone();
                Node b = b1.Clone();

                a.ClipTo(b);
                b.ClipTo(a);
                b.Invert();
                b.ClipTo(a);
                b.Invert();

                a.Build(b.AllPolygons(), EPS);

                Node ret = new Node(a.AllPolygons());
                return ret;
            }

            public static Node Subtract(Node a1, Node b1)
            {
                Node a = a1.Clone();
                Node b = b1.Clone();

                a.Invert();
                a.ClipTo(b);
                b.ClipTo(a);
                b.Invert();
                b.ClipTo(a);
                b.Invert();
                a.Build(b.AllPolygons(), EPS);
                a.Invert();

                Node ret = new Node(a.AllPolygons());
                return ret;
            }

            public static Node Intersect(Node a1, Node b1)
            {
                Node a = a1.Clone();
                Node b = b1.Clone();

                a.Invert();
                b.ClipTo(a);
                b.Invert();
                a.ClipTo(b);
                b.ClipTo(a);

                a.Build(b.AllPolygons(), EPS);
                a.Invert();

                Node ret = new Node(a.AllPolygons());
                return ret;
            }

            static Plane ChooseBestSplitPlane(List<Polygon> polygons, double eps)
            {
                int bestScore = int.MaxValue;
                int best = 0;

                for (int i = 0; i < polygons.Count; i++)
                {
                    var candidatePlane = polygons[i].plane;
                    int score = 0;
                    for (int j = 0; j < polygons.Count; j++)
                    {
                        if (i == j) continue;
                        s_tmpFront.Clear();
                        s_tmpBack.Clear();
                        s_tmpCoplanarFront.Clear();
                        s_tmpCoplanarBack.Clear();
                        Split(candidatePlane, polygons[j], s_tmpCoplanarFront, s_tmpCoplanarBack, s_tmpFront, s_tmpBack, eps);
                        if (s_tmpFront.Count > 0 && s_tmpBack.Count > 0)
                        {
                            score++;
                        }
                    }
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = i;
                    }
                }
                return polygons[best].plane;
            }

            public static void Split(Plane plane, Polygon poly,
                      List<Polygon> coplanarFront,
                      List<Polygon> coplanarBack,
                      List<Polygon> front,
                      List<Polygon> back, double eps)
            {

                ECSGType type = ECSGType.Coplanar;

                int n = poly.vertices.Count;
                ECSGType[] types = new ECSGType[n];
                for (int i = 0; i < n; i++)
                {
                    var t = Orientation(plane, poly.vertices[i], eps);
                    types[i] = t;
                    type |= t;
                }

                switch (type)
                {
                    case ECSGType.Spanning:
                        List<Vec3> f = new List<Vec3>(n);
                        List<Vec3> b = new List<Vec3>(n);
                        for (int i = 0; i < n; i++)
                        {
                            int j = (i + 1) % n;

                            Vec3 vi = poly.vertices[i], vj = poly.vertices[j];
                            ECSGType ti = types[i], tj = types[j];

                            if (ti != ECSGType.Back)
                            {
                                f.Add(vi);
                            }

                            if (ti != ECSGType.Front)
                            {
                                b.Add(vi);
                            }

                            if ((ti | tj) == ECSGType.Spanning)
                            {
                                double t = (plane.distance - Vec3.Dot(plane.normal, vi)) / Vec3.Dot(plane.normal, vj - vi);
                                Vec3 intersectionPoint = Lerp(vi, vj, t);
                                b.Add(intersectionPoint);
                                f.Add(intersectionPoint);
                            }
                        }

                        if (f.Count >= 3 && !IsDegenerate(f))
                        {
                            front.Add(new Polygon(poly.plane, f));
                        }

                        if (b.Count >= 3 && !IsDegenerate(b))
                        {
                            back.Add(new Polygon(poly.plane, b));
                        }
                        break;

                    case ECSGType.Coplanar:
                        double dot = Vec3.Dot(plane.normal, poly.plane.normal);
                        if (dot > eps)
                        {
                            coplanarFront.Add(poly);
                        }
                        else if (dot < -eps)
                        {
                            coplanarBack.Add(poly);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                        break;

                    case ECSGType.Front:
                        front.Add(poly);
                        break;

                    case ECSGType.Back:
                        back.Add(poly);
                        break;

                    default:
                        throw new NotImplementedException(type.ToString());
                }
            }
        }

        public static Vec3 Lerp(Vec3 a, Vec3 b, double t)
        {
            return new Vec3(
                    a.x + (b.x - a.x) * t,
                    a.y + (b.y - a.y) * t,
                    a.z + (b.z - a.z) * t);
        }

        public static ECSGType Orientation(Plane plane, Vec3 point, double eps)
        {
            double dist = plane.SignedDistanceTo(point);
            ECSGType type = ECSGType.Coplanar;
            if (dist < -eps)
            {
                type = ECSGType.Back;
            }
            else if (dist > eps)
            {
                type = ECSGType.Front;
            }
            return type;
        }

        public static bool IsDegenerate(List<Vec3> vertices)
        {
            if (vertices.Count < 3) return true;

            var a = vertices[0];
            for (int i = 1; i < vertices.Count - 1; i++)
            {
                Vec3 b = vertices[i];
                Vec3 c = vertices[i + 1];

                Vec3 cross = Vec3.Cross(b - a, c - a);
                if (cross.x * cross.x + cross.y * cross.y + cross.z * cross.z > 1e-12)
                    return false;
            }
            return true;
        }

        public readonly struct Polygon
        {
            public readonly Plane plane;
            public readonly List<Vec3> vertices;

            public Polygon(Plane plane, List<Vec3> vertices)
            {
                this.plane = plane;
                this.vertices = vertices;
            }

            public Polygon(Vec3 a, Vec3 b, Vec3 c)
            {
                this.plane = new Plane(a, b, c);
                this.vertices = [a, b, c];
            }


      

            public override string ToString()
            {
                return $"{plane.normal} [{vertices.Count}]";
            }

            public Polygon Flip()
            {
                vertices.Reverse();
                return new Polygon(plane.Flip(), vertices);
            }
        }

        [Flags]
        public enum ECSGType
        {
            Coplanar = 0,
            Front = 1,
            Back = 2,
            Spanning = 3
        }
    }
}
