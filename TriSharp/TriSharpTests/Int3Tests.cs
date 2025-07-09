using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriSharp;

namespace TriSharpTests
{
    public class Int3Tests
    {
        [Fact]
        public void Constructor_SetsValuesCorrectly()
        {
            var i = new Int3(1, 2, 3);
            Assert.Equal(1, i.a);
            Assert.Equal(2, i.b);
            Assert.Equal(3, i.c);
        }

        [Fact]
        public void Constructor_SingleValue_SetsAll()
        {
            var i = new Int3(5);
            Assert.Equal(5, i.a);
            Assert.Equal(5, i.b);
            Assert.Equal(5, i.c);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Next_ReturnsCorrect(int input)
        {
            int expected = (input + 1) % 3;
            Assert.Equal(expected, Int3.Next(input));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void Prev_ReturnsCorrect(int input)
        {
            int expected = (input + 2) % 3;
            Assert.Equal(expected, Int3.Prev(input));
        }

        [Theory]
        [InlineData(0, 1, 2)]
        [InlineData(1, 2, 3)]
        [InlineData(2, 3, 1)]
        public void Pair_ReturnsCorrect(int index, int expectedStart, int expectedEnd)
        {
            var i = new Int3(1, 2, 3);
            i.Pair(index, out int s, out int e);
            Assert.Equal(expectedStart, s);
            Assert.Equal(expectedEnd, e);
        }

        [Fact]
        public void Get_ReturnsCorrectValue()
        {
            var i = new Int3(4, 5, 6);
            Assert.Equal(4, i.Get(0));
            Assert.Equal(5, i.Get(1));
            Assert.Equal(6, i.Get(2));
        }

        [Fact]
        public void Set_ChangesValue()
        {
            var i = new Int3(0, 0, 0);
            i.Set(1, 42);
            Assert.Equal(0, i.a);
            Assert.Equal(42, i.b);
            Assert.Equal(0, i.c);
        }

        [Theory]
        [InlineData(1, 2, 0)]
        [InlineData(2, 3, 1)]
        [InlineData(3, 1, 2)]
        [InlineData(9, 9, Int3.NO_INDEX)]
        public void IndexOfEdge_ReturnsExpected(int start, int end, int expected)
        {
            var i = new Int3(1, 2, 3);
            Assert.Equal(expected, i.IndexOf(start, end));
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(2, 1)]
        [InlineData(3, 2)]
        [InlineData(42, Int3.NO_INDEX)]
        public void IndexOfVertex_ReturnsExpected(int value, int expected)
        {
            var i = new Int3(1, 2, 3);
            Assert.Equal(expected, i.IndexOf(value));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        [InlineData(42)]
        public void Get_InvalidIndex_Throws(int index)
        {
            var i = new Int3(1, 2, 3);
            Assert.Throws<ArgumentOutOfRangeException>(() => i.Get(index));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        [InlineData(99)]
        public void Set_InvalidIndex_Throws(int index)
        {
            var i = new Int3(0, 0, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => i.Set(index, 123));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        public void Pair_InvalidIndex_Throws(int index)
        {
            var i = new Int3(1, 2, 3);
            Assert.Throws<ArgumentOutOfRangeException>(() => i.Pair(index, out _, out _));
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(3)]
        public void Next_Invalid_Throws(int i)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Int3.Next(i));
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(3)]
        public void Prev_Invalid_Throws(int i)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Int3.Prev(i));
        }

        [Fact]
        public void ToString_ReturnsCorrectFormat()
        {
            var i = new Int3(7, 8, 9);
            Assert.Equal("7 8 9", i.ToString());
        }
    }
}
