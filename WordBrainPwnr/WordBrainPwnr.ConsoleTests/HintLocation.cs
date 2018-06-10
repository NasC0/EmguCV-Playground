using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordBrainPwnr.ConsoleTests
{
    public class HintLocation
    {
        public HintLocation()
        {
        }

        public HintLocation(int row, int col, bool isFoundInMatrix)
        {
            Row = row;
            Col = col;
            IsFoundInMatrix = isFoundInMatrix;
        }

        public int Row { get; set; }

        public int Col { get; set; }

        public bool IsFoundInMatrix { get; set; }
    }
}
