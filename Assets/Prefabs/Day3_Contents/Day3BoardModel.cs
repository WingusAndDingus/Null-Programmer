using System;

namespace NullProgrammer.Day3
{
    public sealed class Day3BoardModel
    {
        public enum Result { Placed, Bounds, Overlap, InvalidLength }
        public const int Size = 10;
        private readonly int[,] cells = new int[Size, Size];
        private int nextId = 1;
        public int At(int row, int col) => cells[row, col];
        public Result Validate(int row, int col, int length, bool horizontal)
        {
            if (length <= 0) return Result.InvalidLength;
            if (row < 0 || col < 0 || row >= Size || col >= Size) return Result.Bounds;
            if (length > (horizontal ? Size - col : Size - row)) return Result.Bounds;
            for (int i = 0; i < length; i++)
                if (At(row + (horizontal ? 0 : i), col + (horizontal ? i : 0)) != 0) return Result.Overlap;
            return Result.Placed;
        }
        public Result Place(int row, int col, int length, bool horizontal)
        {
            var result = Validate(row, col, length, horizontal);
            if (result != Result.Placed) return result;
            int id = nextId++;
            for (int i = 0; i < length; i++) cells[row + (horizontal ? 0 : i), col + (horizontal ? i : 0)] = id;
            return result;
        }
    }
}
