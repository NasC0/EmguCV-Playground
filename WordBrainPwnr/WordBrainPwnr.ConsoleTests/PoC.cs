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

                List<string> strings = new List<string>
                {
                    "Peter",
                    "Piper",
                    "picked",
                    "peck",
                    "pickled",
                    "peppers"
                };

                ITrie trie = Trie.BuildTrie(strings);
                Node root = trie.RootNode;

                Console.WriteLine("Character matrix");
                List<string> characters = processor.GetCharactersFromImage(boundaries.Characters.CharacterMatrix).ToList();
                int matrixSize = (int)Math.Sqrt(boundaries.Characters.CharacterCount);

                int currentIndex = 0;
                for (int i = 0; i < matrixSize; i++)
                {
                    for (int j = 0; j < matrixSize; j++)
                    {
                        Console.Write($"{characters[currentIndex]} ");
                        currentIndex++;
                    }

                    Console.WriteLine();
                }

                Console.WriteLine("Hints");
                foreach (Hint boundariesHint in boundaries.Hints)
                {
                    if (boundariesHint.IsOcrCandidate)
                    {
                        IEnumerable<string> hintCharacters =
                            processor.GetCharactersFromImage(boundariesHint.OcrCandidate);

                        foreach (string hintCharacter in hintCharacters)
                        {
                            Console.Write($"{hintCharacter} ");
                        }

                        Console.WriteLine();
                    }
                }
            }
        }
    }
}
