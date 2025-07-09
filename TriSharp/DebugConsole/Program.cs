using System.Runtime.InteropServices;
using TriSharp;

namespace DebugConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Int3 a = new Int3(1, 2, 3);
            a.Set(0, 3);
        }




    }

    public static class ExactMath
    {
        public static (double sum, double error) TwoSum(double a, double b)
        {
            double sum = a + b;
            double bvl = sum - a;
            double avl = sum - bvl;
            double brd = b - bvl;
            double ard = a - avl;
            double error = brd + ard;
            return (sum, error);
        }
    }
}
