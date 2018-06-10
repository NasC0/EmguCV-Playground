using System.Collections.Generic;
using WordBrainPwnr.DataStructures.Core.Structures;

namespace WordBrainPwnr.DataStructures.Core
{
    public interface ITrie
    {
        Node RootNode { get; }

        bool WordExists(string word);

        Node Prefix(string word);
    }
}
