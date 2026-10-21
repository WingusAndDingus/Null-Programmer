using System;
using System.Collections.Generic;

namespace NullProgrammer.Day2
{
    // Evaluates the authored alternatives deterministically; it does not execute arbitrary C++.
    public static class Day2TestEngine
    {
        public struct Cell
        {
            public float row, col;
            public Cell(float row, float col) { this.row = row; this.col = col; }
        }

        public sealed class Run
        {
            public bool passed = true;
            public bool examplePassed;
            public int qualityPoints;
            public string output;
            public int rows, cols;
            public Cell[] expected = Array.Empty<Cell>();
            public Cell[] observed = Array.Empty<Cell>();
            public string previewCaption;
            internal bool captured, capturedFailure;
            internal bool exampleEvaluated;
        }

        private struct Position
        {
            public float x, y;
            public Position(float x, float y) { this.x = x; this.y = y; }
            public override string ToString() => "(" + x + ", " + y + ")";
        }

        public static Run Evaluate(int exercise, int choice)
        {
            if (exercise < 0 || exercise >= Day2LessonData.ExerciseCount)
                throw new ArgumentOutOfRangeException(nameof(exercise));
            if (choice < 0 || choice > 2) throw new ArgumentOutOfRangeException(nameof(choice));
            var run = new Run();
            var lines = new List<string>();

            if (exercise == 0)
            {
                CheckPosition(run, lines, choice, 2, 6, 40, 100, 80);
                CheckPosition(run, lines, choice, 0, 0, 40, 100, 80);
                CheckPosition(run, lines, choice, 9, 3, 24, 15, 25);
            }
            else if (exercise == 1)
            {
                CheckLoops(run, lines, choice, 8, 8);
                CheckLoops(run, lines, choice, 6, 10);
                CheckLoops(run, lines, choice, 1, 3);
            }
            else if (exercise == 2)
            {
                CheckResize(run, lines, choice, 10, 10);
                CheckResize(run, lines, choice, 6, 10);
                CheckResize(run, lines, choice, 3, 1);
            }
            else
            {
                Cell[] tests = choice == 0 ? new[] { new Cell(0, 0) }
                    : choice == 1 ? new[] { new Cell(0, 0), new Cell(2, 2) }
                    : new[] { new Cell(0, 0), new Cell(2, 6), new Cell(0, 9), new Cell(9, 0), new Cell(9, 9) };
                bool fixedPasses = true, swappedPasses = true;
                foreach (Cell cell in tests)
                {
                    Position expected = Convert(cell.row, cell.col, 40, 100, 80, 1);
                    fixedPasses &= Equal(expected, Convert(cell.row, cell.col, 40, 100, 80, 1));
                    swappedPasses &= Equal(expected, Convert(cell.row, cell.col, 40, 100, 80, 0));
                }
                run.passed = fixedPasses && !swappedPasses;
                run.examplePassed = run.passed;
                lines.Add("Correct implementation: " + (fixedPasses ? "accepted" : "rejected"));
                lines.Add("Swapped implementation: " + (swappedPasses ? "ACCEPTED (bug escaped)" : "rejected (bug detected)"));
                lines.Add("Required: accept fixed code and reject the bug.");
                run.rows = run.cols = 10;
                run.expected = new[] { new Cell(2, 6) };
                run.observed = new[] { new Cell(6, 2) };
                run.previewCaption = "Reference cell (2,6); orange is the swapped bug " +
                    (swappedPasses ? "your tests missed." : "your tests caught.");
            }

            run.qualityPoints = run.passed ? 5 : run.examplePassed ? 2 : 0;
            run.output = (run.passed ? "PASS - all required checks\n" : "FAIL - some checks failed\n") +
                string.Join("\n\n", lines);
            return run;
        }

        private static Position Convert(float row, float col, float size, float ox, float oy, int choice)
        {
            if (choice == 0) return new Position(ox + row * size, oy + col * size);
            if (choice == 1) return new Position(ox + col * size, oy + row * size);
            return new Position(col * size, row * size);
        }

        private static bool Equal(Position a, Position b)
            => Math.Abs(a.x - b.x) < .001f && Math.Abs(a.y - b.y) < .001f;

        private static void CheckPosition(Run run, List<string> lines, int choice,
            int row, int col, float size, float ox, float oy)
        {
            Position expected = Convert(row, col, size, ox, oy, 1);
            Position observed = Convert(row, col, size, ox, oy, choice);
            bool pass = Equal(expected, observed);
            RecordOutcome(run, pass);
            lines.Add("Cell (" + row + "," + col + "), size " + size + ": " + (pass ? "PASS" : "FAIL") +
                "\nExpected " + expected + "; got " + observed);
            Capture(run, pass, 10, 10, new[] { new Cell(row, col) },
                new[] { new Cell((observed.y - oy) / size, (observed.x - ox) / size) },
                "Tile positions relative to this test's origin and cell size.");
        }

        private static void CheckLoops(Run run, List<string> lines, int choice, int rows, int cols)
        {
            var observed = new List<Cell>();
            // These are the actual loop bounds represented by the three alternatives.
            int rowLimit = rows + (choice == 0 ? 1 : 0);
            int colLimit = choice == 1 ? rows : cols + (choice == 0 ? 1 : 0);
            for (int row = 0; row < rowLimit; row++)
                for (int col = 0; col < colLimit; col++) observed.Add(new Cell(row, col));
            bool pass = rowLimit == rows && colLimit == cols && observed.Count == rows * cols;
            RecordOutcome(run, pass);
            lines.Add(rows + " x " + cols + ": " + (pass ? "PASS" : "FAIL") +
                "\nExpected " + rows * cols + " cells; got " + observed.Count);
            Capture(run, pass, rows, cols, Grid(rows, cols), observed.ToArray(),
                "Teal cells are required; orange cells are outside the requested grid.");
        }

        private static void CheckResize(Run run, List<string> lines, int choice, int rows, int cols)
        {
            int drawnRows = choice == 0 ? 8 : rows;
            int drawnCols = choice == 0 ? 8 : choice == 1 ? cols : rows;
            Cell[] observed = Grid(drawnRows, drawnCols);
            bool pass = drawnRows == rows && drawnCols == cols;
            RecordOutcome(run, pass);
            lines.Add("Request " + rows + " x " + cols + ": " + (pass ? "PASS" : "FAIL") +
                "\nExpected " + rows * cols + " cells; got " + observed.Length);
            Capture(run, pass, rows, cols, Grid(rows, cols), observed,
                "Teal-only cells were requested but not drawn; orange cells are extra.");
        }

        private static Cell[] Grid(int rows, int cols)
        {
            var cells = new List<Cell>();
            for (int row = 0; row < rows; row++)
                for (int col = 0; col < cols; col++) cells.Add(new Cell(row, col));
            return cells.ToArray();
        }

        private static void RecordOutcome(Run run, bool pass)
        {
            if (!run.exampleEvaluated)
            {
                run.examplePassed = pass;
                run.exampleEvaluated = true;
            }
            run.passed &= pass;
        }

        private static void Capture(Run run, bool pass, int rows, int cols,
            Cell[] expected, Cell[] observed, string caption)
        {
            // Keep the first failing test visible; otherwise show the first passing test.
            if (run.captured && (pass || run.capturedFailure)) return;
            run.captured = true;
            run.capturedFailure = !pass;
            run.rows = rows; run.cols = cols;
            run.expected = expected; run.observed = observed; run.previewCaption = caption;
        }
    }
}
