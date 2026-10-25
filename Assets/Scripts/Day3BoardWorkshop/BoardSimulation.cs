using System;
using System.Collections.Generic;

namespace NullProgrammer.Day3Workshop
{
    // A deterministic teaching interpreter, not a C++ compiler. Each patch changes
    // the actual simulated algorithm used by both the preview and regression suite.
    public sealed class BoardSimulation
    {
        public const int Rows = 6, Columns = 8;
        public readonly bool[,] occupied = new bool[Rows, Columns];
        public readonly int[] patches;
        public BoardSimulation(int[] choices)
        {
            if (choices == null || choices.Length != 4) throw new ArgumentException("Four patches required.");
            patches = (int[])choices.Clone();
            foreach (int choice in patches) if (choice < 0 || choice > 2) throw new ArgumentOutOfRangeException(nameof(choices));
        }
        public struct Cell : IEquatable<Cell>
        {
            public int row, column;
            public Cell(int r, int c) { row = r; column = c; }
            public bool Equals(Cell other) => row == other.row && column == other.column;
            public override bool Equals(object other) => other is Cell && Equals((Cell)other);
            public override int GetHashCode() => row * 397 ^ column;
        }
        public sealed class Run
        {
            public bool accepted, unsafeAccess;
            public string reason;
            public readonly List<Cell> cells = new List<Cell>();
            public readonly List<Cell> written = new List<Cell>();
            public int visualRow, visualColumn, visualRows, visualColumns;
        }
        public static bool Inside(Cell c) => c.row >= 0 && c.row < Rows && c.column >= 0 && c.column < Columns;
        public List<Cell> Footprint(int row, int column, int length, bool vertical)
        {
            var cells = new List<Cell>();
            for (int i = 0; i < length; i++)
            {
                int dr = vertical ? i : 0, dc = vertical ? 0 : i;
                if (patches[0] == 0) { dr = 0; dc = i; }
                if (patches[0] == 2) { dr = i; dc = 0; }
                cells.Add(new Cell(row + dr, column + dc));
            }
            return cells;
        }
        private bool Allowed(Cell c, int row, int column)
        {
            if (patches[1] == 0) return Inside(new Cell(row, column)); // Checks only the start.
            if (patches[1] == 2) return c.row >= 0 && c.row <= Rows && c.column >= 0 && c.column <= Columns;
            return Inside(c);
        }
        public Run Place(int row, int column, int length, bool vertical)
        {
            var run = new Run();
            if (length < 1) { run.reason = "Ship length must be positive."; return run; }
            run.cells.AddRange(Footprint(row, column, length, vertical));
            foreach (Cell c in run.cells)
            {
                if (!Allowed(c, row, column)) { run.reason = "Rejected: ship crosses the board edge."; return run; }
                // Model the invalid access visibly; never index the real array out of range.
                if (!Inside(c)) { run.unsafeAccess = true; run.reason = "Unsafe access: code accepted an out-of-range cell."; return run; }
            }
            // Bounds are checked for the whole footprint before the separate overlap pass.
            foreach (Cell c in run.cells)
            {
                if (patches[2] != 2 && occupied[c.row, c.column])
                { run.reason = "Rejected: cell is already occupied."; return run; }
                if (patches[2] == 0)
                { occupied[c.row, c.column] = true; run.written.Add(c); }
            }
            if (patches[2] != 0)
                foreach (Cell c in run.cells) { occupied[c.row, c.column] = true; run.written.Add(c); }
            run.accepted = true;
            run.reason = "Accepted: ship placed.";
            run.visualRow = patches[3] == 0 ? column : row;
            run.visualColumn = patches[3] == 0 ? row : column;
            run.visualRows = vertical && patches[3] != 2 ? length : 1;
            run.visualColumns = vertical && patches[3] != 2 ? 1 : length;
            return run;
        }
        public void Seed() { occupied[2, 3] = occupied[2, 4] = true; }
        public bool[,] Snapshot() => (bool[,])occupied.Clone();
        public static bool Equal(bool[,] a, bool[,] b)
        {
            for (int r = 0; r < Rows; r++) for (int c = 0; c < Columns; c++) if (a[r,c] != b[r,c]) return false;
            return true;
        }
        public sealed class Check
        {
            public string name;
            public bool passed;
            public Check(string n, bool p) { name = n; passed = p; }
        }
        public static List<Check> CheckPatch(int exercise, int[] patches)
        {
            if (exercise < 0 || exercise > 4) throw new ArgumentOutOfRangeException(nameof(exercise));
            var checks = new List<Check>();
            if (exercise == 0 || exercise == 4)
            {
                foreach (bool vertical in new[] { false, true })
                {
                    var board = new BoardSimulation(patches);
                    Run run = board.Place(1, 2, 3, vertical);
                    bool ok = run.accepted;
                    for (int i = 0; i < 3; i++) ok &= run.cells.Contains(new Cell(1 + (vertical ? i : 0), 2 + (vertical ? 0 : i)));
                    checks.Add(new Check(vertical ? "Vertical occupied cells" : "Horizontal occupied cells", ok));
                }
            }
            if (exercise == 1 || exercise == 4)
            {
                int[,] starts = { {5,1}, {1,7}, {-1,1}, {1,-1}, {6,1}, {1,8} };
                for (int i = 0; i < 6; i++)
                {
                    var board = new BoardSimulation(patches);
                    Run run = board.Place(starts[i,0], starts[i,1], 3, i % 2 == 0);
                    checks.Add(new Check("Reject edge case " + (i + 1), !run.accepted && !run.unsafeAccess && Equal(board.occupied, new bool[Rows,Columns])));
                }
                var valid = new BoardSimulation(patches);
                checks.Add(new Check("Accept exact bottom edge", valid.Place(3, 0, 3, true).accepted));
                valid = new BoardSimulation(patches);
                checks.Add(new Check("Accept exact right edge", valid.Place(0, 5, 3, false).accepted));
            }
            if (exercise == 2 || exercise == 4)
            {
                var board = new BoardSimulation(patches); board.Seed();
                bool[,] before = board.Snapshot();
                Run run = board.Place(2, 1, 3, false);
                checks.Add(new Check("Reject overlap", !run.accepted));
                checks.Add(new Check("Rejected ship leaves board unchanged", Equal(before, board.occupied)));
                board = new BoardSimulation(patches); board.Seed();
                checks.Add(new Check("Allow a separate ship", board.Place(4, 1, 3, false).accepted));
            }
            if (exercise == 3 || exercise == 4)
            {
                foreach (bool vertical in new[] { false, true })
                {
                    var board = new BoardSimulation(patches);
                    Run run = board.Place(1, 3, 3, vertical);
                    checks.Add(new Check(vertical ? "Vertical SFML transform" : "Horizontal SFML transform",
                        run.accepted && run.visualRow == 1 && run.visualColumn == 3 &&
                        run.visualRows == (vertical ? 3 : 1) && run.visualColumns == (vertical ? 1 : 3)));
                }
            }
            return checks;
        }
        public static bool Passed(List<Check> checks) => checks.Count > 0 && checks.TrueForAll(c => c.passed);
    }
}
