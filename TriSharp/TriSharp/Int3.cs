namespace TriSharp
{
    using System;
    using System.Runtime.CompilerServices;

    public struct Int3
    {
        public const int NO_INDEX = -1;

        public int a, b, c;

        public Int3(int value)
        {
            a = b = c = value;
        }

        public Int3(int a, int b, int c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Pair(int i, out int s, out int e)
        {
            if (i == 0) { s = a; e = b; return; }
            if (i == 1) { s = b; e = c; return; }
            if (i == 2) { s = c; e = a; return; }
            throw new ArgumentOutOfRangeException($"Index must be 0, 1, or 2 but got {i}.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(int value)
        {
            if (a == value) return 0;
            if (b == value) return 1;
            if (c == value) return 2;
            return NO_INDEX;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(int start, int end)
        {
            if (a == start) return b == end ? 0 : NO_INDEX;
            if (b == start) return c == end ? 1 : NO_INDEX;
            if (c == start) return a == end ? 2 : NO_INDEX;
            return NO_INDEX;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Get(int i)
        {
            if (i == 0) return a;
            if (i == 1) return b;
            if (i == 2) return c;
            throw new ArgumentOutOfRangeException($"Index must be 0, 1, or 2 but got {i}.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int i, int value)
        {
            if (i == 0) { a = value; return; }
            if (i == 1) { b = value; return; }
            if (i == 2) { c = value; return; }
            throw new ArgumentOutOfRangeException($"Index must be 0, 1, or 2 but got {i}.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Next(int i)
        {
            if (i == 0) return 1;
            if (i == 1) return 2;
            if (i == 2) return 0;
            throw new ArgumentOutOfRangeException($"Index must be 0, 1, or 2 but got {i}.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Prev(int i)
        {
            if (i == 0) return 2;
            if (i == 1) return 0;
            if (i == 2) return 1;
            throw new ArgumentOutOfRangeException($"Index must be 0, 1, or 2 but got {i}.");
        }

        public override string ToString()
        {
            return $"{a} {b} {c}";
        }
    }
}
