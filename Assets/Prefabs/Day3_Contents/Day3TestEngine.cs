using System;
using System.Collections.Generic;

namespace NullProgrammer.Day3
{
    // Simulates the three authored alternatives, not arbitrary C++ source.
    public static class Day3TestEngine
    {
        public sealed class Run
        {
            public bool passed;
            public string output;
            public int[] cells = Array.Empty<int>();
            public int[] danger = Array.Empty<int>();
        }
        public static Run Evaluate(int exercise, int choice)
        {
            if (choice < 0 || choice > 2) throw new ArgumentOutOfRangeException(nameof(choice));
            var result = new Run();
            switch(exercise)
            {
                case 0:
                    int reportedByOtherTile = choice == 0 ? 0 : choice == 1 ? 3 : 0;
                    result.passed = reportedByOtherTile == 3;
                    result.cells = choice == 2 ? Array.Empty<int>() : new[] {21,22,23};
                    result.output = "Place 3 segments; inspect board next frame.\nExpected: 3 occupied cells in one consistent grid.\nObserved: " + reportedByOtherTile + " occupied cells reported by the board.";
                    break;
                case 1:
                    var cells = new List<int>();
                    for (int i=0;i<3;i++)
                    {
                        int row = 2 + (choice==0 ? i : 0), col = 1 + (choice==1 ? 0 : i);
                        int index = row*10+col; if(!cells.Contains(index)) cells.Add(index);
                    }
                    result.cells=cells.ToArray();
                    result.passed=cells.Count==3 && cells[0]==21 && cells[1]==22 && cells[2]==23;
                    result.output="Horizontal from (row 2, col 1), length 3.\nExpected cells: (2,1), (2,2), (2,3).\nObserved: " + string.Join(", ",cells.ConvertAll(cell=>"("+cell/10+","+cell%10+")"));
                    break;
                case 2:
                    bool rejectsExact=Rejects(7,3,choice), rejectsBeyond=Rejects(8,3,choice);
                    result.passed=!rejectsExact && rejectsBeyond;
                    result.cells=new[] {27,28,29}; result.danger=rejectsBeyond ? Array.Empty<int>() : new[] {28,29};
                    result.output="Column 7 + length 3: " + (rejectsExact?"REJECTED (should fit)":"accepted") +
                        "\nColumn 8 + length 3: " + (rejectsBeyond?"rejected":"ACCEPTED (would reach invalid column 10)") + "\nInvalid accesses are illustrated, not executed.";
                    break;
                case 3:
                    bool[] occupied={false,true,false}; bool rejection=choice==0 ? occupied[0] : choice==1 ? occupied[0]||occupied[1]||occupied[2] : occupied[0]&&occupied[1]&&occupied[2];
                    result.passed=rejection; result.cells=new[] {43}; result.danger=new[] {42,43,44};
                    result.output="Proposed cells (4,2), (4,3), (4,4). Middle cell already occupied.\nExpected: reject.\nObserved: " + (rejection?"rejected":"accepted an overlapping ship");
                    break;
                case 4:
                    int changed=choice==0?2:choice==1?1:0;
                    result.passed=changed==0; result.cells=new[] {44}; result.danger=changed==2?new[] {42,43}:changed==1?new[] {42}:Array.Empty<int>();
                    result.output="Last segment (4,4) collides.\nExpected: reject; zero new occupied cells.\nObserved: " + changed + " cells changed before rejection.";
                    break;
                case 5:
                    float x=choice==0?64:choice==1?96:3, y=choice==0?96:choice==1?64:2;
                    result.passed=x==96 && y==64;
                    result.cells=new[] {(int)(y/32)*10+(int)(x/32)};
                    result.output="Cell (row 2, col 3), tile size 32.\nExpected pixel position: (96,64).\nObserved: ("+x+","+y+").";
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(exercise));
            }
            return result;
        }
        private static bool Rejects(int col,int length,int choice)
            => choice==0 ? col+length>10 : choice==1 ? col+length>=10 : col>10;
    }
}
