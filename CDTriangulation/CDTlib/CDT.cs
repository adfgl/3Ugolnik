namespace CDTlib
{
    public static class CDT
    {

        public static void Add(List<Face> faces, TopologyChange source)
        {
            int n = source.NewFaces.Length;
            if (n != source.AffectedEdges.Length)
            {
                throw new Exception();
            }

            for (int i = 0; i < n; i++)
            {
                Face f = source.NewFaces[i];
                Edge e = source.AffectedEdges[i];

                Edge? twin = e.Twin;
                if (twin is not null)
                {
                    twin.Twin = e;
                }

                e.Origin.Edge = e;

                if (f.Index < 0)
                {
                    f.Index = faces.Count;
                    faces.Add(f);
                }
                else
                {
                    faces[f.Index] = f;
                }
            }

            foreach (Face f in source.OldFaces)
            {
                f.Dead = true;
                //foreach (Edge e in f)
                //{
                //    e.Face = null!;
                //    if (e.Twin?.Face == f)
                //    {
                //        e.Twin.Twin = null;
                //    }
                //    if (e.Origin.Edge == e)
                //    {
                //        e.Origin.Edge = null!;
                //    }

                //    e.Next = null!;
                //    e.Prev = null!;
                //    e.Twin = null!;
                //}
            }
        }
    }
}
