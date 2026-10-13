using System;
using UnityEngine;

namespace NullProgrammer.Day3
{
    [CreateAssetMenu(menuName = "Null Programmer/Day 3 Lesson")]
    public sealed class Day3LessonData : ScriptableObject
    {
        [Serializable]
        public sealed class Exercise
        {
            public string title;
            public HeadPersonality reward;
            [TextArea(2, 6)] public string architect, sentinel, faultyCode, question;
            [TextArea(2, 10)] public string[] options;
            public int correctIndex;
            [TextArea] public string architectHint, sentinelHint, success, transfer;
            [TextArea] public string[] failures;
        }
        public string subject = "Wednesday - Ship placement";
        [TextArea(8, 16)] public string email;
        public DialogueLine[] call, introduction, conclusion;
        public Exercise[] exercises;
        public void SetDefaults()
        {
            email = "Morning,\n\nYesterday, we got individual tiles rendering on the screen. Today, we need to organize those tiles into a board and place ships on it.\n\nShips should fit inside the board and must not overlap. We'll also need horizontal and vertical placement.\n\nJoin the Zoom call when you've read this. We'll go over the plan.\n\n- Steve";
            call = new[] {
                Line("Steve", HeadPersonality.None, "Morning. We've got tiles on the screen, but a collection of tiles isn't quite a working board yet."),
                Line("Steve", HeadPersonality.None, "The Board should organize the grid and decide whether a ship can be placed. Individual tiles keep track of whether they're occupied."),
                Line("Steve", HeadPersonality.None, "Make placement work horizontally and vertically. Test ships hanging off the edge or crossing another ship."),
                Line("Steve", HeadPersonality.None, "If a placement fails, the board should stay exactly as it was. No half-placed ships. How will you approach it?") };
            introduction = new[] {
                Line("Architect", HeadPersonality.GrandArchitect, "Yesterday, we gave each tile a position. Today, we give those tiles a structure."),
                Line("Sentinel", HeadPersonality.Sentinel, "A structure with boundaries. A ship doesn't get to invent an eleventh column."),
                Line("Architect", HeadPersonality.GrandArchitect, "The Board handles operations involving several tiles. Each Tile handles its own state."),
                Line("Sentinel", HeadPersonality.Sentinel, "Read the code, choose a correction, then run the test. Check the entire placement before changing anything.") };
            conclusion = new[] {
                Line("Architect", HeadPersonality.GrandArchitect, "The Board organizes the tiles, checks placements, and updates their state. The visualization works because the code follows those rules."),
                Line("Sentinel", HeadPersonality.Sentinel, "Those habits carry into Minesweeper: stay inside the grid, avoid duplicate mine locations, and keep the board's state consistent."),
                Line("Architect", HeadPersonality.GrandArchitect, "Ship orientation belongs to Battleship. Board organization, indexing, and SFML positioning are useful in both games.") };
            exercises = new[] {
                E("Board ownership", HeadPersonality.GrandArchitect,
                    "A Tile needs information about itself. The Board coordinates the whole grid.", "Separate copies of the grid can disagree about occupied cells.",
                    "// Fault: every Tile stores its own complete grid.\nclass Tile { /* a copy of the entire board */ };", "Which structure keeps one consistent board?",
                    new[] { "Each Tile stores a separate copy of the complete grid.", "Board stores the grid; each Tile stores its own cell state.", "The drawing function creates a new empty grid every frame." }, 1,
                    "Which object coordinates operations across several cells?", "Where should a placed ship remain stored on the next frame?",
                    "One Board coordinates the cells. Occupied state survives the next frame.",
                    new[] { "Independent grid copies disagree about occupied cells.", "", "A new grid every frame loses all placed ships." },
                    "Minesweeper: Board coordinates a grid of tiles; each tile keeps its own mine, flag, and reveal state."),
                E("Ship orientation", HeadPersonality.GrandArchitect,
                    "Horizontal movement changes columns; vertical movement changes rows.", "Trace all three segments. They must visit different cells in a straight line.",
                    "int checkRow = r + (horizontal ? i : 0);\nint checkCol = c + (horizontal ? 0 : i);", "Replace the coordinate calculation:",
                    new[] { "int checkRow = r + i;\nint checkCol = c + i;", "int checkRow = r;\nint checkCol = c;", "int checkRow = r + (horizontal ? 0 : i);\nint checkCol = c + (horizontal ? i : 0);" }, 2,
                    "A horizontal ship stays in one row.", "Trace i = 0, 1, 2. Do you visit three unique cells?",
                    "Horizontal advances columns; vertical advances rows.",
                    new[] { "Both coordinates advance, producing a diagonal.", "All segments target the starting cell.", "" },
                    "Minesweeper: use row/column indexing consistently. Mines do not have ship orientation."),
                E("Board boundaries", HeadPersonality.Sentinel,
                    "Indices run from zero to dimension minus one.", "A 10-column board uses columns 0-9. Touching the edge is valid; crossing it is not.",
                    "// Negative starts and nonpositive lengths already rejected.\nif (c > cols) return false;", "Which condition rejects a horizontal ship beyond the right edge?",
                    new[] { "if (c + length > cols) return false;", "if (c + length >= cols) return false;", "if (c > cols) return false;" }, 0,
                    "Starting at column 7, a length-3 ship uses columns 7, 8, 9.", "Test column 7 AND column 8 with length 3.",
                    "An exact-edge placement fits. An over-edge placement is rejected.",
                    new[] { "", "This rejects ships that fit exactly at the edge.", "This only checks the start, not the full length." },
                    "Minesweeper: check bounds before accessing neighbors of edge or corner tiles."),
                E("Overlap checks", HeadPersonality.Sentinel,
                    "Visit every proposed ship cell using the orientation calculation.", "An empty starting cell does not guarantee that the rest are empty.",
                    "// Fault: only the first cell is checked.\nif (grid[r][c].occupied()) return false;", "Which check prevents a partial overlap?",
                    new[] { "Check only the starting cell.", "Check every proposed cell; reject if ANY cell is occupied.", "Reject only if ALL proposed cells are occupied." }, 1,
                    "The loop must cover every ship segment.", "One collision is enough. Try a collision in the middle segment.",
                    "A collision in the middle rejects the entire placement.",
                    new[] { "The starting cell is clear, but the middle cell overlaps.", "", "A partial overlap is still invalid." },
                    "Minesweeper: random mine placement must not count the same occupied tile twice."),
                E("Validate before updating", HeadPersonality.Sentinel,
                    "Separate checking the proposed change from applying it.", "A collision on the last segment must not leave the first segments occupied.",
                    "// Fault: mutate each cell before the full check succeeds.\nmarkOccupied(cell);\nif (collision) return false;", "Choose the safe placement sequence:",
                    new[] { "Update each cell, then check whether it was valid.", "Update the first cell, then validate the rest.", "Validate ALL cells; update only after every check passes." }, 2,
                    "Use a validation pass followed by an update pass.", "Compare the board before and after a rejected placement.",
                    "The final-segment collision is rejected. The board remains unchanged.",
                    new[] { "Earlier cells were changed before discovering the collision.", "Even one early update leaves a partial ship.", "" },
                    "Minesweeper: rejected operations should preserve board state. Flagged tiles, for example, should stay unchanged when left-clicked."),
                E("SFML positioning", HeadPersonality.GrandArchitect,
                    "Grid indices are row/column; SFML position is X/Y. Column controls X.", "Tiles here are 32 pixels square, with the board starting at (0, 0).",
                    "sprite.setPosition({row * 32.f, col * 32.f});", "Which statement aligns the sprite with its cell?",
                    new[] { "sprite.setPosition({row * 32.f, col * 32.f});", "sprite.setPosition({col * 32.f, row * 32.f});", "sprite.setPosition({col * 1.f, row * 1.f});" }, 1,
                    "Increasing the column should move right.", "At row 2, column 3, expect X = 96, Y = 64.",
                    "Column maps to X, row maps to Y. The sprite aligns with its logical cell.",
                    new[] { "Rows and columns are swapped.", "", "The sprite advances one pixel instead of one tile." },
                    "Minesweeper: its tiles are 32 pixels square. SFML 3 setPosition receives one Vector2f; brace initialization supplies it.") };
        }
        private static Exercise E(string title, HeadPersonality reward, string architect, string sentinel,
            string faulty, string question, string[] options, int correct, string aHint, string sHint, string success, string[] failures, string transfer)
            => new Exercise { title=title, reward=reward, architect=architect, sentinel=sentinel, faultyCode=faulty,
                question=question, options=options, correctIndex=correct, architectHint=aHint, sentinelHint=sHint,
                success=success, failures=failures, transfer=transfer };
        public static DialogueLine Line(string speaker, HeadPersonality personality, string text)
            => new DialogueLine { speakerName=speaker, personality=personality, text=text, displayDuration=5f };
    }
}
