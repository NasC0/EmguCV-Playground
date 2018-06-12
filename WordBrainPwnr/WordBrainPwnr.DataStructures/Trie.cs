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

                prefix.IsWholeWord = true;
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
            Node node = GetWordNode(word);
            return node != null;
        }

        public bool IsFullWord(string word)
        {
            Node node = GetWordNode(word);
            return node.IsWholeWord;
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

        private Node GetWordNode(string word)
        {
            Node currentNode = RootNode;

            foreach (char character in word)
            {
                Node childNode = currentNode.FindChildNode(character);
                if (childNode == null)
                {
                    return null;
                }

                currentNode = childNode;
            }

            return currentNode;
        }
    }
}
