using System.Collections.Generic;
using WordBrainPwnr.DataStructures.Core;
using WordBrainPwnr.DataStructures.Core.Structures;

namespace WordBrainPwnr.DataStructures
{
    public class Trie : ITrie
    {
        public static Trie BuildTrie(IEnumerable<string> dictionary)
        {
            Node rootNode = new Node(null, 0, default(char));
            Trie result = new Trie(rootNode);

            foreach (string word in dictionary)
            {
                Node prefix = result.Prefix(word);
                for (int i = prefix.Length; i < word.Length; i++)
                {
                    Node newNode = new Node(prefix, prefix.Length + 1, word[i]);
                    prefix.ChildNodes.Add(newNode);
                    prefix = newNode;
                }
            }

            return result;
        }

        public Trie(Node rootNode)
        {
            RootNode = rootNode;
        }

        public Node RootNode { get; private set; }

        public bool WordExists(string word)
        {
            Node currentNode = RootNode;

            foreach (char character in word)
            {
                if (currentNode.FindChildNode(character) == null)
                {
                    return false;
                }
            }

            return true;
        }

        public Node Prefix(string word)
        {
            Node currentNode = RootNode;
            Node result = currentNode;

            foreach (char letter in word)
            {
                currentNode = currentNode.FindChildNode(letter);
                if (currentNode == null)
                    break;
                result = currentNode;
            }

            return result;
        }
    }
}
