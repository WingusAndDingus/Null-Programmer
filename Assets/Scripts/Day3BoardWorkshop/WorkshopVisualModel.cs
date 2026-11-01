using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NullProgrammer.Day3Workshop
{
    // Presentation-only calculations. The workshop still grades with BoardSimulation.CheckPatch.
    public static class WorkshopVisualModel
    {
        public struct Sample
        {
            public int row, column, length;
            public bool vertical, seeded;
            public Sample(int r, int c, int len, bool v, bool seed = false)
            { row = r; column = c; length = len; vertical = v; seeded = seed; }
        }
        // Same order as CheckPatch(4): orientation, bounds, overlap, rendering.
        public static readonly Sample[] FinalSamples = {
            new Sample(1,2,3,false), new Sample(1,2,3,true),
            new Sample(5,1,3,true), new Sample(1,7,3,false),
            new Sample(-1,1,3,true), new Sample(1,-1,3,false),
            new Sample(6,1,3,true), new Sample(1,8,3,false),
            new Sample(3,0,3,true), new Sample(0,5,3,false),
            new Sample(2,1,3,false,true), new Sample(2,1,3,false,true),
            new Sample(4,1,3,false,true),
            new Sample(1,3,3,false), new Sample(1,3,3,true)
        };
        public static List<BoardSimulation.Cell> IntendedCells(Sample sample)
        {
            var cells = new List<BoardSimulation.Cell>();
            for (int i = 0; i < sample.length; i++)
                cells.Add(new BoardSimulation.Cell(sample.row + (sample.vertical ? i : 0),
                    sample.column + (sample.vertical ? 0 : i)));
            return cells;
        }
        public static bool Conflict(BoardSimulation.Cell cell, bool seeded)
        { return !BoardSimulation.Inside(cell) || (seeded && cell.row == 2 && (cell.column == 3 || cell.column == 4)); }
        public static string PlacementSummary(BoardSimulation.Run result)
        {
            if (result == null) return "Choose a start cell, then run the build.";
            if (result.unsafeAccess) return "Unsafe access: an out-of-range cell was accepted.";
            if (result.accepted) return "Ship placement executed. Inspect the occupied cells.";
            if (result.written.Count > 0)
                return "Placement rejected, but " + result.written.Count + " cells remained occupied.";
            return result.reason.IndexOf("occupied", StringComparison.OrdinalIgnoreCase) >= 0
                ? "Overlap rejected. Board unchanged."
                : "Out-of-bounds placement rejected. Board unchanged.";
        }
        private static readonly Regex Tokens = new Regex(@"//[^\n]*|\b(?:if|for|return|false|true|int|bool|float|const|Cell)\b|\b\d+\b|[<>]", RegexOptions.Compiled);
        public static string HighlightCode(string source)
        {
            if (source == null) return "";
            return Tokens.Replace(source, match =>
            {
                string token = match.Value;
                if (token == "<" || token == ">") return "<noparse>" + token + "</noparse>";
                string color = token.StartsWith("//", StringComparison.Ordinal) ? "7896A8"
                    : char.IsDigit(token[0]) ? "E6AD51" : "A6C8FF";
                return "<color=#" + color + ">" + token + "</color>";
            });
        }
        public static string LineNumbers(string source)
        {
            int count = (source ?? "").Split('\n').Length;
            var text = new StringBuilder();
            for (int i = 0; i < count; i++) { if (i > 0) text.Append('\n'); text.Append(42 + i); }
            return text.ToString();
        }
        public static int EditableLine(string source)
        {
            string[] lines = (source ?? "").Split('\n');
            for (int i = 0; i < lines.Length; i++)
                if (!lines[i].TrimStart().StartsWith("//", StringComparison.Ordinal)) return i;
            return 0;
        }
    }
}
