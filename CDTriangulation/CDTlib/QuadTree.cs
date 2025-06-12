namespace CDTlib
{
    public class QuadTree
    {
        readonly QuadNode root;
        readonly List<Node> _items;

        public QuadTree(Rectangle bounds, int maxDepth = 10, int maxItems = 8)
        {
            bounds = ExpandedBounds(bounds);
            root = new QuadNode(bounds, 0, maxDepth, maxItems);
            Bounds = bounds;
            _items = new List<Node>();
        }

        public Rectangle Bounds { get; }
        public IReadOnlyList<Node> Items => _items;
        public int Count => _items.Count;

        public void Add(Node node)
        {
            if (!Bounds.Contains(node.X, node.Y))
            {
                throw new Exception();
            }
            root.Insert(node);
            _items.Add(node);
        }

        public List<Node> Query(Rectangle area)
        {
            List<Node> results = new List<Node>();
            root.Query(area, results);
            return results;
        }

        Rectangle ExpandedBounds(Rectangle rect)
        {
            double dx = rect.maxX - rect.minX;
            double dy = rect.maxY - rect.minY;
            double dm = Math.Max(dx, dy);
            double exp = Math.Max(10, dm * 1.01);
            return rect.Expand(dm);
        }

        public Node? TryGet(double x, double y, double precision = 1e-10)
        {
            return root.TryGet(x, y, precision);
        }

        class QuadNode
        {
            const int TOP_L = 0, TOP_R = 1, BOT_L = 2, BOT_R = 3;

            readonly Rectangle _bounds;
            readonly double _cx, _cy;
            readonly int _depth, _maxDepth, _maxItems;
            readonly List<Node> _items;
            QuadNode[]? _children;

            public QuadNode(Rectangle bounds, int depth, int maxDepth, int maxItems)
            {
                _bounds = bounds;
                _depth = depth;
                _maxDepth = maxDepth;
                _maxItems = maxItems;
                _items = new List<Node>();

                _cx = (bounds.minX + bounds.maxX) * 0.5;
                _cy = (bounds.minY + bounds.maxY) * 0.5;
            }

            public void Insert(Node node)
            {
                if (_children is not null)
                {
                    GetChild(node.X, node.Y).Insert(node);
                    return;
                }

                _items.Add(node);
                if (_items.Count > _maxItems && _depth < _maxDepth)
                {
                    Subdivide();
                    foreach (Node item in _items)
                    {
                        GetChild(item.X, item.Y)._items.Add(item);
                    }
                    _items.Clear();
                }
            }

            public Node? TryGet(double x, double y, double eps = 1e-10)
            {
                if (_children is null)
                {
                    double epsSqr = eps * eps;
                    foreach (Node item in _items)
                    {
                        double dx = item.X - x;
                        double dy = item.Y - y;
                        if (dx * dx + dy * dy <= epsSqr)
                        {
                            return item;
                        }
                    }
                    return null;
                }

                for (int i = 0; i < 4; i++)
                {
                    if (_children[i]._bounds.IntersectsCircle(x, y, eps))
                    {
                        Node? result = _children[i].TryGet(x, y, eps);
                        if (result is not null)
                        {
                            return result;
                        }
                    }
                }
                return null;
            }

            public void Query(Rectangle area, List<Node> results)
            {
                if (!_bounds.Intersects(area))
                    return;

                if (_children is null)
                {
                    foreach (Node item in _items)
                    {
                        if (area.Contains(item.X, item.Y))
                        {
                            results.Add(item);
                        }
                    }
                    return;
                }

                for (int i = 0; i < 4; i++)
                {
                    _children[i].Query(area, results);
                }
            }


            void Subdivide()
            {
                double minX = _bounds.minX;
                double minY = _bounds.minY;
                double maxX = _bounds.maxX;
                double maxY = _bounds.maxY;

                /*             maxY
                 *      +--------+--------+
                 *      |        |        |
                 *      |        |        |
                 * minX +--------C--------+ maxX
                 *      |        |        |
                 *      |        |        |
                 *      +--------+--------+
                 *             minY
                 */

                int depth = _depth + 1;
                _children = new QuadNode[4];
                _children[TOP_L] = new QuadNode(new Rectangle(minX, _cy, _cx, maxY), depth, _maxDepth, _maxItems);
                _children[TOP_R] = new QuadNode(new Rectangle(_cx, _cy, maxX, maxY), depth, _maxDepth, _maxItems);
                _children[BOT_L] = new QuadNode(new Rectangle(minX, minY, _cx, _cy), depth, _maxDepth, _maxItems);
                _children[BOT_R] = new QuadNode(new Rectangle(_cx, minY, maxX, _cy), depth, _maxDepth, _maxItems);
            }

            QuadNode GetChild(double x, double y)
            {
                bool right = x >= _cx;
                bool top = y >= _cy;

                int index =
                    top
                    ? (right ? TOP_R : TOP_L)
                    : (right ? BOT_R : BOT_L);

                return _children![index];
            }
        }
    }
}
