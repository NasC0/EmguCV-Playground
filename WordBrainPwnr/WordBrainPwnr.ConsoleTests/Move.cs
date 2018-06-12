using System.Collections.Generic;
using System.Linq;

namespace WordBrainPwnr.ConsoleTests
{
    public class Move
    {
        private readonly Dictionary<Location, bool> _visitedLocations;

        public Move(string value, int depth, Location location)
            : this(null, value, depth, location)
        {
        }

        public Move(Move parent, string value, int depth, Location location)
        {
            Parent = parent;
            Value = value;
            Depth = depth;
            Location = location;
            Children = new List<Move>();
            _visitedLocations = new Dictionary<Location, bool>();
        }

        public Move Parent { get; set; }

        public string Value { get; set; }

        public int Depth { get; set; }

        public Location Location { get; set; }

        public List<Move> Children { get; set; }

        public bool HasVisitedAllChildren { get; set; }

        public void AddChild(Move child)
        {
            Children.Add(child);
            _visitedLocations.Add(child.Location, false);
        }

        public void VisitChild(Move child)
        {
            if (!_visitedLocations.ContainsKey(child.Location))
            {
                return;
            }

            _visitedLocations[child.Location] = true;

            if (_visitedLocations.All(v => v.Value))
            {
                HasVisitedAllChildren = true;
                Parent?.VisitChild(this);
            }
        }

        public string GetWord()
        {
            List<string> wordUpToHere = new List<string>();
            Move currentMove = this;
            wordUpToHere.Add(currentMove.Value);

            while (currentMove.Parent != null)
            {
                currentMove = currentMove.Parent;
                wordUpToHere.Add(currentMove.Value);
            }

            string word = string.Join("", wordUpToHere.Reverse<string>());

            return word;
        }

        public IEnumerable<Location> GetTraversedLocations()
        {
            List<Location> traversedLocations = new List<Location>();
            Move currentMove = this;
            traversedLocations.Add(currentMove.Location);

            while (currentMove.Parent != null)
            {
                currentMove = currentMove.Parent;
                traversedLocations.Add(currentMove.Location);
            }

            return traversedLocations;
        }
    }
}
