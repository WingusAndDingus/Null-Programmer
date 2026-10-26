namespace NullProgrammer.Day3Workshop
{
    public static class WorkshopLesson
    {
        public static readonly string[] Titles = { "01 / Orientation", "02 / Board boundaries", "03 / Safe placement", "04 / SFML graphics", "05 / Final build" };
        public static readonly string[] Objectives = {
            "Place a vertical ship at row 1, column 2. Inspect the direction it grows.",
            "Place a vertical ship at row 5, column 1. It must be rejected safely.",
            "Place a horizontal ship at row 2, column 1. Rejection must leave no extra cells.",
            "Place a vertical ship at row 1, column 3. Compare its graphic with its occupied cells.",
            "Run the regression suite, then experiment with the repaired build." };
        // Slot 1 is correct. Slots 0 and 2 execute distinct faulty implementations.
        public static readonly string[][] Choices = {
            new[] { "Cell{row, column + i}", "Cell{row + (vertical ? i : 0), column + (vertical ? 0 : i)}", "Cell{row + i, column}" },
            new[] { "inside(startRow, startColumn)", "r >= 0 && r < rows && c >= 0 && c < columns", "r >= 0 && r <= rows && c >= 0 && c <= columns" },
            new[] { "checkAndWriteEachCell(cells)", "validateAll(cells); thenWriteAll(cells)", "writeAll(cells) // skip overlap validation" },
            new[] { "{row * cellSize, column * cellSize}; // position", "{column * cellSize, row * cellSize}; // position", "{length * cellSize, cellSize}; // size for both orientations" }
        };
        public static string Code(int exercise, int choice)
        {
            switch (exercise)
            {
                case 0: return "// Board::occupiedCells; i is the segment index\nCell cell = " + Choices[0][choice] + ";";
                case 1: return "// Board::canPlace; r and c describe EACH segment\nif (!(" + Choices[1][choice] + ")) return false;";
                case 2:
                    if (choice == 0) return "for (Cell c : cells) {\n  if (occupied(c)) return false; markOccupied(c);\n}";
                    if (choice == 1) return "for (Cell c : cells) if (occupied(c)) return false;\nfor (Cell c : cells) markOccupied(c);";
                    return "for (Cell c : cells) markOccupied(c);\nreturn true; // existing ships never checked";
                default:
                    string position = choice == 0 ? "row * cellSize, column * cellSize" : "column * cellSize, row * cellSize";
                    string size = choice == 2 ? "length * cellSize, cellSize" : "(vertical ? 1 : length) * cellSize, (vertical ? length : 1) * cellSize";
                    return "ship.setPosition({" + position + "});\nship.setSize({" + size + "});";
            }
        }
        public static int[] DemoRow = { 1, 5, 2, 1 };
        public static int[] DemoColumn = { 2, 1, 1, 3 };
        public static bool[] DemoVertical = { true, true, false, true };
        public static string Event(int exercise) => "Day3.Board.Submitted." + exercise;
    }
}
