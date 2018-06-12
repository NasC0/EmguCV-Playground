using System;
using System.Collections.Generic;

namespace WordBrainPwnr.DataStructures.Core.Structures
{
    public class Node
    {
        private const int ChildNodesSize = 26;

        public Node(Node parent, int length, char value)
        {
            ChildNodes = new List<Node>(ChildNodesSize);
            Parent = parent;
            Length = length;
            Value = char.ToUpper(value);
        }

        public Node Parent { get; private set; }

        public char Value { get; set; }

        public List<Node> ChildNodes { get; private set; }

        public int Length { get; private set; }

        public bool IsWholeWord { get; set; }

        public Node FindChildNode(char c)
        {
            char toUpper = char.ToUpper(c);

            foreach (Node childNode in ChildNodes)
            {
                if (childNode?.Value == toUpper)
                {
                    return childNode;
                }
            }

            return null;
        }
    }
}