namespace NullProgrammer.Day2
{
    public sealed class Day2Exercise
    {
        public string title, objective, faultyCode;
        public string[] replacements, explanations;
        public int correctIndex;
        public string[][] hints;
    }

    public static class Day2LessonData
    {
        public const int ExerciseCount = 4;

        public static readonly Day2Exercise[] Exercises =
        {
            new Day2Exercise
            {
                title = "Tile.cpp / Row and column mapping",
                objective = "Choose a replacement for the two faulty lines. Run it against several cells and origins.",
                faultyCode = "// row = 2, col = 6; cellSize = 40\n// originX = 100, originY = 80\nfloat x = originX + row * cellSize;\nfloat y = originY + col * cellSize;\n// Expected: (340, 160)",
                replacements = new[]
                {
                    "float x = originX + row * cellSize;\nfloat y = originY + col * cellSize;",
                    "float x = originX + col * cellSize;\nfloat y = originY + row * cellSize;",
                    "float x = col * cellSize;\nfloat y = row * cellSize;"
                },
                correctIndex = 1,
                explanations = new[]
                {
                    "Rows move down the screen; columns move right. These inputs are still swapped.",
                    "Correct. Column controls X, row controls Y, and both include the origin.",
                    "The axes are right, but the origin was omitted. This only works when the grid starts at (0, 0)."
                },
                hints = new[]
                {
                    new[]
                    {
                        "Architect: Moving across a row should move the tile right. Which index changes as you move right?",
                        "Architect: Columns change X; rows change Y. Start counting pixels from the grid origin.",
                        "Architect: Each coordinate is its origin plus its matching index times the cell size.",
                        "Architect: At (row 2, col 6), expect X = 100 + 6 * 40 and Y = 80 + 2 * 40. Check (0, 0) too.",
                        "Architect: The conversion must work for unequal indices, nonzero origins, and different sizes. Swapped axes and missing origins fail different cases."
                    },
                    new[]
                    {
                        "Pragmatist: Put the tile where it looks right and move on. That will not satisfy every test here.",
                        "Pragmatist: Hardcode (340, 160) for the demo. We will need a general rule for the other cells.",
                        "Pragmatist: Use index times forty. Remember that a grid starting away from zero needs an offset too.",
                        "Pragmatist: Fix the axis swap and spot-check this cell. The rest of the tests may catch something we skipped.",
                        "Pragmatist: Keep the conversion short. A single formula beats patching each tile, even when we are rushing."
                    },
                    new[]
                    {
                        "Detective: Mention yesterday's SFML setup. A familiar detail may reassure Steve, but it does not position this tile.",
                        "Detective: Recall the CLion project you used yesterday. That buys a calmer conversation, not an X or Y calculation.",
                        "Detective: Yesterday's CMake configuration is something you can confidently refer to. It does not tell us which index controls X.",
                        "Detective: Remember how Steve set SFML_DIR yesterday. Use that concrete detail to reassure him while you decide what code to submit.",
                        "Detective: Recalling the external/ library folder and CMake linking sounds experienced. The coordinate conversion is still a separate problem."
                    }
                }
            },
            new Day2Exercise
            {
                title = "Grid.cpp / Generate every cell once",
                objective = "Choose the loop headers. The test checks a square grid, a rectangle, and a single-row grid.",
                faultyCode = "// rows and cols are cell COUNTS\nfor (int row = 0; row <= rows; ++row) {\n    for (int col = 0; col <= cols; ++col) {\n        drawTile(row, col);\n    }\n}",
                replacements = new[]
                {
                    "for (int row = 0; row <= rows; ++row)\n    for (int col = 0; col <= cols; ++col) drawTile(row, col);",
                    "for (int row = 0; row < rows; ++row)\n    for (int col = 0; col < rows; ++col) drawTile(row, col);",
                    "for (int row = 0; row < rows; ++row)\n    for (int col = 0; col < cols; ++col) drawTile(row, col);"
                },
                correctIndex = 2,
                explanations = new[]
                {
                    "Using <= includes the count itself as an index. An 8-by-8 grid becomes 9 by 9.",
                    "Both loops use rows. This passes a square example but truncates or extends a rectangular grid.",
                    "Correct. Each index stays below its own count, so exactly rows * cols cells are generated."
                },
                hints = new[]
                {
                    new[]
                    {
                        "Architect: Count from zero. Eight entries have indices zero through seven.",
                        "Architect: Stop before the count, not at it. Check both loop conditions.",
                        "Architect: The outer loop covers rows; the inner loop covers columns. Each has its own limit.",
                        "Architect: An 8-by-8 example cannot reveal a column loop that incorrectly uses rows. Try 6 by 10.",
                        "Architect: Verify exactly rows * cols distinct cells, with final indices rows - 1 and cols - 1. Test 1 by 3 as well as square and rectangular grids."
                    },
                    new[]
                    {
                        "Pragmatist: Draw until the screen looks full. An extra row can hide off the edge.",
                        "Pragmatist: Change the loop signs and check the screenshot. Be careful: a square image can hide the wrong variable.",
                        "Pragmatist: Reusing one dimension in both loops is shorter, but only safe if we promise the grid is always square.",
                        "Pragmatist: Use separate row and column counts, then spot-check the square case. The rectangle test is still worth running.",
                        "Pragmatist: Two short loops can still use the correct bounds. Speed does not require generating extra cells."
                    },
                    new[]
                    {
                        "Detective: Bring up getting SFML running yesterday if Steve seems uncertain. It will not change a loop bound.",
                        "Detective: Recall the CLion setup from yesterday. Familiarity can lower suspicion without generating any cells.",
                        "Detective: Knowing where the library is installed says nothing about whether the inner loop uses rows or columns.",
                        "Detective: The configuration details from yesterday can make Steve more comfortable. The rectangular test still measures the code you choose.",
                        "Detective: A confident recap of yesterday's CMake setup can reassure Steve. It cannot keep an inclusive loop from drawing extra cells."
                    }
                }
            },
            new Day2Exercise
            {
                title = "Grid.cpp / Resize without rewriting",
                objective = "Replace the drawing call. It must follow BOTH requested dimensions, including rectangular grids.",
                faultyCode = "void showGrid(int requestedRows,\n              int requestedCols) {\n    // Fault: ignores the requested size\n    drawGrid(8, 8);\n}",
                replacements = new[]
                {
                    "drawGrid(8, 8);",
                    "drawGrid(requestedRows, requestedCols);",
                    "drawGrid(requestedRows, requestedRows);"
                },
                correctIndex = 1,
                explanations = new[]
                {
                    "The requested size is still ignored. A ten-by-ten request produces only 64 cells.",
                    "Correct. Both dimensions come from the request. Resizing does not require editing fixed numbers.",
                    "The requested row count is reused for columns. Ten by ten passes, but six by ten becomes six by six."
                },
                hints = new[]
                {
                    new[]
                    {
                        "Architect: A request for ten rows cannot keep drawing only eight rows.",
                        "Architect: Use the requested values instead of the two fixed eights.",
                        "Architect: The first argument controls rows; the second controls columns. Preserve both inputs.",
                        "Architect: Ten by ten should produce 100 cells; six by ten should produce 60. The second case catches a reused row count.",
                        "Architect: Treat dimensions as configuration. Test both a rectangular board and a narrow board so independent inputs cannot accidentally become one shared number."
                    },
                    new[]
                    {
                        "Pragmatist: Replace eight with ten for today's screenshot. The next requested size will break that patch.",
                        "Pragmatist: Copy enough tiles to fill the new preview. We are creating more manual work for later.",
                        "Pragmatist: Passing the same requested number twice works for squares. The rectangular test is the catch.",
                        "Pragmatist: Forward both requested dimensions and check ten by ten. We can at least avoid editing tile coordinates.",
                        "Pragmatist: Passing through the inputs is also the shortest useful fix. Skip repeated constants, even in a quick prototype."
                    },
                    new[]
                    {
                        "Detective: Ask about yesterday's first SFML window. The familiar topic may buy time, but it is not a resize implementation.",
                        "Detective: Keeping the same CLion project is an easy detail to discuss confidently. It does not decide the new dimensions.",
                        "Detective: Steve can be reassured that yesterday's build setup remains intact. The drawing call still needs its own arguments.",
                        "Detective: Recalling the SFML library configuration lowers concern about the environment. A six-by-ten grid still depends on the code you submit.",
                        "Detective: Remembering yesterday's external/ folder and CMake setup sounds experienced. It cannot turn a fixed eight-by-eight call into a configurable grid."
                    }
                }
            },
            new Day2Exercise
            {
                title = "GridTests.cpp / Catch the regression",
                objective = "Choose tests that accept the correct mapping AND reject a version with row and column swapped.",
                faultyCode = "// This weak test passes even with swapped axes!\nassert(position(0, 0) == expected(0, 0));\n// position(row, col) is the code under test.\n// expected() uses column -> X and row -> Y.",
                replacements = new[]
                {
                    "check(0, 0); // origin only",
                    "check(0, 0);\ncheck(2, 2); // equal row and column",
                    "check(0, 0); check(2, 6);\ncheck(0, 9); check(9, 0); check(9, 9);"
                },
                correctIndex = 2,
                explanations = new[]
                {
                    "At the origin both indices are zero, so swapping them changes nothing. The bug escaped.",
                    "Equal row and column indices hide an axis swap. Both implementations still pass these tests.",
                    "Correct. Unequal indices and off-diagonal corners expose the swap while the correct implementation passes."
                },
                hints = new[]
                {
                    new[]
                    {
                        "Architect: Swapping two equal numbers changes nothing. Choose at least one cell with different row and column values.",
                        "Architect: The origin and (2, 2) cannot expose swapped axes. Consider (2, 6).",
                        "Architect: A good regression test must fail for the known bug and pass for the fixed calculation.",
                        "Architect: Check (2, 6), then the top-right and bottom-left corners. Those cases use unequal indices.",
                        "Architect: Test the correct and deliberately swapped implementations. Your test set must distinguish them; a passing test that never exposes the fault provides false confidence."
                    },
                    new[]
                    {
                        "Pragmatist: The origin test is green. We could stop there, but it has not challenged the swapped calculation.",
                        "Pragmatist: Add another easy square-diagonal cell. That adds a test without necessarily detecting this bug.",
                        "Pragmatist: Try one unequal pair. A short useful test beats several tests that cannot fail for this fault.",
                        "Pragmatist: Check the known failing cell before the demo. Corner tests would improve confidence further.",
                        "Pragmatist: Keep the test set compact, but include a case that actually rejects the swapped implementation."
                    },
                    new[]
                    {
                        "Detective: The working SFML setup from yesterday is a reassuring topic. It cannot prove that this test catches a bug.",
                        "Detective: Mention the CLion setup if you need a calmer conversation. Your selected tests must provide their own evidence.",
                        "Detective: Yesterday's library configuration is a concrete detail to remember. It cannot make an origin-only check detect swapped axes.",
                        "Detective: Steve may trust you more when you recall the setup steps. That trust does not change what a regression test can detect.",
                        "Detective: We can sound like the developer Steve expects by remembering yesterday's environment setup. The quality of the submitted tests remains a separate matter."
                    }
                }
            }
        };
    }
}
