using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                Image image = Image.FromFile("../../IMG-1ca764be45f572eab4a1f5a40a0bffa5-V.jpg");
                image.Save(ms, ImageFormat.Png);
                byte[] byteArray = ms.ToArray();

                byte[] detectedPlayingField = surfDetector.DetectPlayingField(byteArray);
                IBoundaryDetector boundaryDetector = new BoundaryDetector();
                PlayingFieldData boundaries = boundaryDetector.GetBoundaries(detectedPlayingField);

                IOcrProcessor processor = new TesseractOcrProcessor("C:\\Program Files\\Tesseract-OCR\\tessdata", "eng-wordbrain");

                IEnumerable<string> allWords = File.ReadLines("../../../Resources/EnglishWords/words_alpha.txt");

                ITrie trie = Trie.BuildTrie(allWords);

                Console.WriteLine("Character matrix");
                List<string> characters = processor.GetCharactersFromImage(boundaries.Characters.CharacterMatrix).ToList();
                int matrixSize = (int)Math.Sqrt(boundaries.Characters.CharacterCount);

                int currentIndex = 0;
                char?[,] characterMatrix = new char?[matrixSize, matrixSize];
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
                Hint hint = boundaries.Hints.FirstOrDefault();

                Stopwatch sw = new Stopwatch();
                sw.Start();
                IEnumerable<string> possibleSolutions = GetPossibleSolutionsForHint(trie, characterMatrix, hint);
                sw.Stop();

                Console.WriteLine($"Elapsed while finding first words and sqeezing: {sw.Elapsed}");
            }
        }

        private static IEnumerable<string> GetPossibleSolutionsForHint(ITrie wordTrie, char?[,] characterMatrix,
            Hint hint)
        {
            List<string> possibleSolutions = new List<string>();
            //string hintString = string.Empty;
            //if (hint.IsOcrCandidate)
            //{
            //    IEnumerable<string> hintCharacters = ocrProcessor.GetCharactersFromImage(hint.OcrCandidate);
            //    hintString = string.Join("", hintCharacters);
            //}

            //Node currentNode = wordTrie.Prefix(hintString);
            //HintLocation startingPoint = GetStartingPoint(characterMatrix, hintString);

            //if (startingPoint.IsFoundInMatrix)
            //{

            //}

            int matrixRows = characterMatrix.GetLength(0);
            int matrixCols = characterMatrix.GetLength(1);

            for (int rows = 0; rows < matrixRows; rows++)
            {
                for (int cols = 0; cols < matrixCols; cols++)
                {
                    string currentValue = characterMatrix[rows, cols].ToString();
                    Move initialMove = new Move(currentValue, 1, new Location(rows, cols));
                    Queue<Move> moveQueue = new Queue<Move>();
                    moveQueue.Enqueue(initialMove);

                    while (moveQueue.Count > 0)
                    {
                        Move currentMove = moveQueue.Dequeue();
                        currentMove.Parent?.VisitChild(currentMove);

                        if (wordTrie.WordExists(currentMove.Value))
                        {
                            if (currentMove.Depth == hint.HintSize && wordTrie.IsFullWord(currentMove.Value))
                            {
                                possibleSolutions.Add(currentMove.Value);
                                SqueezeMatrix(characterMatrix, currentMove.GetTraversedLocations());
                                continue;
                            }

                            IEnumerable<Move> availableMoves = GetAvailableMoves(currentMove, characterMatrix);
                            foreach (Move availableMove in availableMoves)
                            {
                                currentMove.AddChild(availableMove);
                                moveQueue.Enqueue(availableMove);
                            }
                        }
                    }
                }
            }

            return possibleSolutions;
        }

        public static HintLocation GetStartingPoint(char[,] characterMatrix, string hint)
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

        public static char?[,] SqueezeMatrix(char?[,] matrix, IEnumerable<Location> usedLocations)
        {
            HashSet<Location> locations = new HashSet<Location>(usedLocations);

            if (locations.Count == 0)
            {
                return matrix;
            }

            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            List<List<char?>> invertedSqueezedMatrix = new List<List<char?>>();

            for (int col = 0; col < cols; col++)
            {
                List<char?> currentSqueezedColumn = SqueezeColumn(matrix, col, locations);
                invertedSqueezedMatrix.Add(currentSqueezedColumn);
            }

            char?[,] outputMatrix = new char?[rows, cols];

            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    char? currentValue = invertedSqueezedMatrix[col][row];
                    outputMatrix[row, col] = currentValue;
                }
            }

            return outputMatrix;
        }

        private static List<char?> SqueezeColumn(char?[,] matrix, int column, IEnumerable<Location> locations)
        {
            IEnumerable<Location> orderedLocations = locations
                .Where(l => l.Col == column);

            HashSet<Location> hashedLocations = new HashSet<Location>(orderedLocations);

            int rows = matrix.GetLength(0);

            List<char?> charList = new List<char?>();
            List<char?> nullList = new List<char?>();
            for (int row = 0; row < rows; row++)
            {
                Location currentLocation = new Location(row, column);
                if (hashedLocations.Contains(currentLocation))
                {
                    hashedLocations.Remove(currentLocation);
                    nullList.Add(null);
                    continue;
                }

                charList.Add(matrix[row, column]);
            }

            List<char?> columnList = new List<char?>(nullList);
            columnList.AddRange(charList);

            return columnList;
        }

        public static IEnumerable<Move> GetAvailableMoves(Move currentMove, char?[,] matrix)
        {
            IEnumerable<Location> previousLocations = currentMove.GetTraversedLocations();
            IEnumerable<Location> possibleLocations = GetPossibleLocations(previousLocations, currentMove.Location,
                matrix.GetLength(0), matrix.GetLength(1));

            List<Move> availableMoves = new List<Move>();
            foreach (Location possibleLocation in possibleLocations)
            {
                char? matrixCharacter = matrix[possibleLocation.Row, possibleLocation.Col];
                if (!matrixCharacter.HasValue)
                {
                    continue;
                }

                string value = currentMove.Value + matrixCharacter;
                Move availableMove = new Move(currentMove, value, currentMove.Depth + 1, possibleLocation);
                availableMoves.Add(availableMove);
            }

            return availableMoves;
        }

        public static IEnumerable<Location> GetPossibleLocations(IEnumerable<Location> previousLocations,
            Location currentLocation, int rows, int cols)
        {
            List<Location> allAvailableLocations = new List<Location>
            {
                Move(MoveDirection.Up, currentLocation),
                Move(MoveDirection.Down, currentLocation),
                Move(MoveDirection.Left, currentLocation),
                Move(MoveDirection.Right, currentLocation),
                Move(MoveDirection.UpLeft, currentLocation),
                Move(MoveDirection.UpRight, currentLocation),
                Move(MoveDirection.DownLeft, currentLocation),
                Move(MoveDirection.DownRight, currentLocation)
            };

            IEnumerable<Location> viableLocations = allAvailableLocations.Where(l =>
                (l.Row >= 0 && l.Row < rows) && (l.Col >= 0 && l.Col < cols) && !previousLocations.Any(pl => pl.Equals(l)));

            return viableLocations;
        }

        public static Location Move(MoveDirection direction, Location currentLocation)
        {
            switch (direction)
            {
                case MoveDirection.Up:
                    return new Location(currentLocation.Row - 1, currentLocation.Col);
                case MoveDirection.Down:
                    return new Location(currentLocation.Row + 1, currentLocation.Col);
                case MoveDirection.Left:
                    return new Location(currentLocation.Row, currentLocation.Col - 1);
                case MoveDirection.Right:
                    return new Location(currentLocation.Row, currentLocation.Col + 1);
                case MoveDirection.UpLeft:
                    return new Location(currentLocation.Row - 1, currentLocation.Col - 1);
                case MoveDirection.UpRight:
                    return new Location(currentLocation.Row - 1, currentLocation.Col + 1);
                case MoveDirection.DownLeft:
                    return new Location(currentLocation.Row + 1, currentLocation.Col - 1);
                case MoveDirection.DownRight:
                    return new Location(currentLocation.Row + 1, currentLocation.Col + 1);
                default:
                    throw new ArgumentException("Invalid enumeration supplied", nameof(direction));
            }
        }
    }
}
