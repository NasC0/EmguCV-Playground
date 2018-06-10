using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using WordBrain.ImageProcessing;
using WordBrain.ImageProcessing.Contracts;
using WordBrain.ImageProcessing.Matchers;
using WordbrainPwnr.ImageProcessing.Core;
using WordbrainPwnr.ImageProcessing.Core.Models;
using WordBrainPwnr.DataStructures;
using WordBrainPwnr.DataStructures.Core;
using WordBrainPwnr.DataStructures.Core.Structures;

namespace WordBrainPwnr.ConsoleTests
{
    public class PoC
    {
        public static void Main()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                IMatcher matcher = new FlannMatcher(8, 1);
                IPlayingFieldDetector surfDetector = new SurfPlayingFieldDetector("../../SURF_Resources", matcher);
                Image image = Image.FromFile("../../Rat_18.jpg");
                image.Save(ms, ImageFormat.Png);
                byte[] byteArray = ms.ToArray();

                byte[] detectedPlayingField = surfDetector.DetectPlayingField(byteArray);
                IBoundaryDetector boundaryDetector = new BoundaryDetector();
                PlayingFieldData boundaries = boundaryDetector.GetBoundaries(detectedPlayingField);

                IOcrProcessor processor = new TesseractOcrProcessor("C:\\Program Files\\Tesseract-OCR\\tessdata", "eng-wordbrain");

                IEnumerable<string> allWords = File.ReadLines("../../../Resources/EnglishWords/words_alpha.txt");

                ITrie trie = Trie.BuildTrie(allWords);
                Node root = trie.RootNode;

                Console.WriteLine("Character matrix");
                List<string> characters = processor.GetCharactersFromImage(boundaries.Characters.CharacterMatrix).ToList();
                int matrixSize = (int)Math.Sqrt(boundaries.Characters.CharacterCount);

                int currentIndex = 0;
                char[,] characterMatrix = new char[matrixSize, matrixSize];
                for (int i = 0; i < matrixSize; i++)
                {
                    for (int j = 0; j < matrixSize; j++)
                    {
                        characterMatrix[i, j] = char.Parse(characters[currentIndex]);
                        currentIndex++;
                    }
                }

                Console.WriteLine("Hints");
                List<string> hintCharacters = new List<string>();
                foreach (Hint boundariesHint in boundaries.Hints)
                {
                    if (boundariesHint.IsOcrCandidate)
                    {
                        IEnumerable<string> currentHintCharacters =
                            processor.GetCharactersFromImage(boundariesHint.OcrCandidate)
                                .ToList();

                        string hint = string.Empty;
                        if (currentHintCharacters.Any())
                        {
                            hint = string.Join("", currentHintCharacters);
                        }

                        bool possibleSolution = true;
                        while (possibleSolution)
                        {

                        }
                    }
                }
            }
        }

        private static IEnumerable<Solution> GetPossibleSolutionsForHint(ITrie wordTrie, char[,] characterMatrix,
            Hint hint, IOcrProcessor ocrProcessor)
        {
            List<Solution> possibleSolutions = new List<Solution>();
            string hintString = string.Empty;
            if (hint.IsOcrCandidate)
            {
                IEnumerable<string> hintCharacters = ocrProcessor.GetCharactersFromImage(hint.OcrCandidate);
                hintString = string.Join("", hintCharacters);
            }

            Node currentNode = wordTrie.Prefix(hintString);
            HintLocation startingPoint = GetStartingPoint(characterMatrix, hintString);

            if (startingPoint.IsFoundInMatrix)
            {

            }

            return possibleSolutions;
        }

        private static HintLocation GetStartingPoint(char[,] characterMatrix, string hint)
        {
            if (string.IsNullOrWhiteSpace(hint))
            {
                return new HintLocation();
            }

            char lastHintCharacter = hint[hint.Length - 1];

            for (int row = 0; row < characterMatrix.Length; row++)
            {
                for (int col = 0; col < characterMatrix.Length; col++)
                {
                    if (characterMatrix[row, col] == lastHintCharacter)
                    {
                        return new HintLocation(row, col, true);
                    }
                }
            }

            return new HintLocation();
        }
    }
}
