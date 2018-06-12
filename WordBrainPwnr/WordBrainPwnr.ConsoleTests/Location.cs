using System;

namespace WordBrainPwnr.ConsoleTests
{
    public class Location
    {
        public Location()
        {
        }

        public Location(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public int Row { get; }

        public int Col { get; }

        public override bool Equals(object obj)
        {
            if (!(obj is Location locationObject))
            {
                throw new ArgumentException($"Passed parameter must of type ${nameof(Location)}", nameof(obj));
            }

            return Row == locationObject.Row && Col == locationObject.Col;
        }

        protected bool Equals(Location other)
        {
            return Row == other.Row && Col == other.Col;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Row * 397) ^ Col;
            }
        }
    }
}
