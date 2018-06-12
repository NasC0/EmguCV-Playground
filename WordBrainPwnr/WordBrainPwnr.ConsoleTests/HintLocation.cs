namespace WordBrainPwnr.ConsoleTests
{
    public class HintLocation : Location
    {
        public HintLocation()
        {
        }

        public HintLocation(int row, int col, bool isFoundInMatrix)
            : base(row, col)
        {
            IsFoundInMatrix = isFoundInMatrix;
        }

        public bool IsFoundInMatrix { get; set; }
    }
}
