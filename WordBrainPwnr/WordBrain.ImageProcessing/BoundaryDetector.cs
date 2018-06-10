using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;
using WordBrain.ImageProcessing.Models.Local;
using WordbrainPwnr.ImageProcessing.Core;
using WordbrainPwnr.ImageProcessing.Core.Models;

namespace WordBrain.ImageProcessing
{
    public class BoundaryDetector : IBoundaryDetector
    {
        public const int Treshold = 50;
        public const int TresholdRation = 3;

        public PlayingFieldData GetBoundaries(byte[] playingField)
        {
            Image<Gray, byte> grayscalePlayingField;
            using (MemoryStream imageStream = new MemoryStream(playingField))
            {
                Bitmap convertedImage = (Bitmap) Image.FromStream(imageStream);
                grayscalePlayingField = new Image<Gray, byte>(convertedImage);
            }
            
            Mat scaledImage = ScaleImage(grayscalePlayingField);

            Mat edges = ApplyPreProcessing(scaledImage);
            List<Rectangle> boxList = GetBoundariesForProcessedImage(edges)
                .OrderBy(r => r.Y)
                .ToList();

            int boxListCount = boxList.Count;
            int splitBoxesCount = (boxListCount / 2);

            List<Rectangle> characterBoxes = boxList
                .Take(splitBoxesCount)
                .ToList();

            IEnumerable<Rectangle> hintBoxes = boxList
                .Skip(splitBoxesCount)
                .Take(splitBoxesCount)
                .ToList();

            Image<Gray, byte> binarizedPlayingField =
                new Image<Gray, byte>(scaledImage.Width, scaledImage.Height, new Gray(0));
            CvInvoke.Threshold(scaledImage, binarizedPlayingField, 150, 255, ThresholdType.Binary);

            byte[] charactersImage = GetCharacters(binarizedPlayingField, characterBoxes)
                .ToJpegData();
            IEnumerable<Hint> hints = GetHintCandidates(binarizedPlayingField, hintBoxes);

            return new PlayingFieldData
            {
                Characters = new Characters
                {
                    CharacterMatrix = charactersImage,
                    CharacterCount = characterBoxes.Count
                },
                Hints = hints
            };
        }

        private Image<Gray, byte> GetCharacters(Image<Gray, byte> playingField,
            IEnumerable<Rectangle> characterBoundaries)
        {
            List<MatrixRow> characterRows = SplitBoxesToRows(characterBoundaries, 10).ToList();
            List<Rectangle> characterBoxes = new List<Rectangle>();

            foreach (MatrixRow characterRow in characterRows)
            {
                characterBoxes.AddRange(characterRow.Row.OrderBy(r => r.X));
            }

            return CombineBoxes(playingField, characterBoxes);
        }

        private Image<Gray, byte> CombineBoxes(Image<Gray, byte> playingField, IEnumerable<Rectangle> characterBoundaries)
        {
            List<Rectangle> characterBoundariesList = characterBoundaries.ToList();
            int minRowSize = characterBoundariesList.Min(c => c.Height);

            Image<Gray, byte> combine =
                GetEnlargedBoundingRoiImage(playingField, characterBoundariesList.FirstOrDefault(), minRowSize);

            for (int i = 1; i < characterBoundariesList.Count; i++)
            {
                Rectangle currentRectangle = characterBoundariesList[i];
                Image<Gray, byte> enlargedBoundingImage = GetEnlargedBoundingRoiImage(playingField, currentRectangle, minRowSize);
                CvInvoke.HConcat(combine, enlargedBoundingImage, combine);
            }

            return combine;
        }

        private IEnumerable<Hint> GetHintCandidates(Image<Gray, byte> playingField, IEnumerable<Rectangle> hintsBoundaries)
        {
            List<Hint> hints = GetHints(hintsBoundaries).ToList();

            string directoryName = Guid.NewGuid().ToString();
            Directory.CreateDirectory($".\\{directoryName}");
            foreach (Hint hint in hints)
            {
                List<Rectangle> ocrCandidates = new List<Rectangle>();

                foreach (Rectangle hintBoundingBox in hint.BoundingBoxes)
                {
                    if (CheckForWhitePixelsInBinarizedImage(playingField, hintBoundingBox))
                    {
                        ocrCandidates.Add(hintBoundingBox);
                    }
                }

                if (ocrCandidates.Count > 0)
                {
                    hint.IsOcrCandidate = true;
                    Image<Gray, byte> hintBoxes = CombineBoxes(playingField, ocrCandidates);
                    hint.OcrCandidate = hintBoxes.ToJpegData();

                    string fileName = $"{Guid.NewGuid().ToString()}.png";
                    hintBoxes.Save($"{directoryName}\\{fileName}");
                }
            }

            return hints;
        }

        private bool CheckForWhitePixelsInBinarizedImage(Image<Gray, byte> playingField, Rectangle boundingBox)
        {
            Point upperLeftPixelLocation = new Point(boundingBox.X + 1, boundingBox.Y + 1);
            Point upperRightPixelLocation = new Point(boundingBox.X + boundingBox.Width - 1, boundingBox.Y + 1);
            Point bottomLeftPixelLocation = new Point(boundingBox.X + 1, boundingBox.Y + boundingBox.Height - 1);
            Point bottomRightPixelLocation = new Point(boundingBox.X + boundingBox.Width - 1, boundingBox.Y + boundingBox.Height - 1);

            List<byte> colorValueFromPoints = new List<byte>();
            colorValueFromPoints.Add(playingField.Data[upperLeftPixelLocation.Y, upperLeftPixelLocation.X, 0]);
            colorValueFromPoints.Add(playingField.Data[upperRightPixelLocation.Y, upperRightPixelLocation.X, 0]);
            colorValueFromPoints.Add(playingField.Data[bottomLeftPixelLocation.Y, bottomLeftPixelLocation.X, 0]);
            colorValueFromPoints.Add(playingField.Data[bottomRightPixelLocation.Y, bottomRightPixelLocation.X, 0]);

            int whitePixelsCount = colorValueFromPoints
                .Count(v => v == 255);

            if (whitePixelsCount >= 3)
            {
                return true;
            }

            return false;
        }

        private IEnumerable<Hint> GetHints(IEnumerable<Rectangle> hintsBoundaries)
        {
            List<Rectangle> boundariesList = hintsBoundaries.ToList();
            int maxHintBoxWidth = boundariesList.Max(h => h.Width);
            int approximateMaxDistanceBetweenHints = maxHintBoxWidth / 2;
            IEnumerable<MatrixRow> hintRows = SplitBoxesToRows(boundariesList, 4);
            List<MatrixRow> hintRowsList = hintRows.ToList();

            List<Hint> hints = new List<Hint>();
            foreach (MatrixRow hintRow in hintRowsList)
            {
                List<Rectangle> rowList = hintRow.Row
                    .OrderBy(r => r.X)
                    .ToList();

                List<Rectangle> maxCurrentCandidate = new List<Rectangle>();
                int limit = hintRow.Row.Count();

                for (int i = 0; i < limit; i++)
                {
                    Rectangle currentRectangle = rowList[i];
                    maxCurrentCandidate.Add(currentRectangle);

                    if (i == limit - 1)
                    {
                        Hint hint = new Hint()
                        {
                            HintSize = maxCurrentCandidate.Count,
                            BoundingBoxes = maxCurrentCandidate
                        };

                        hints.Add(hint);
                        break;
                    }

                    Rectangle nextRectangle = rowList[i + 1];
                    int distanceBetweenPreviousXEndPointAndCurrentStartingPoint = nextRectangle.X - (currentRectangle.X + currentRectangle.Width);
                    int widthDifference = Math.Abs(nextRectangle.Width - currentRectangle.Width);

                    if (distanceBetweenPreviousXEndPointAndCurrentStartingPoint >= widthDifference + approximateMaxDistanceBetweenHints)
                    {
                        Hint hint = new Hint
                        {
                            HintSize = maxCurrentCandidate.Count,
                            BoundingBoxes = maxCurrentCandidate
                        };

                        hints.Add(hint);
                        maxCurrentCandidate = new List<Rectangle>();
                    }
                }
            }

            return hints;
        }

        private IEnumerable<MatrixRow> SplitBoxesToRows(IEnumerable<Rectangle> boxes, int divisor)
        {
            List<Rectangle> boxesList = boxes
                .ToList();

            int minBoxHeight = boxesList.Min(h => h.Height);
            int minApproxDistance = minBoxHeight / divisor;

            List<MatrixRow> rows = new List<MatrixRow>();
            List<Rectangle> currentRow = new List<Rectangle>();

            int limit = boxesList.Count;
            for (int i = 0; i < limit; i++)
            {
                Rectangle currentBox = boxesList[i];
                currentRow.Add(currentBox);

                if (i == limit - 1)
                {
                    MatrixRow currentMatrixRow = new MatrixRow
                    {
                        Row = currentRow
                    };

                    rows.Add(currentMatrixRow);
                    break;
                }

                Rectangle nextBox = boxesList[i + 1];

                int currentBoxBottom = currentBox.Y + currentBox.Height;

                if (nextBox.Y >= minApproxDistance + currentBoxBottom)
                {
                    MatrixRow currentMatrixRow = new MatrixRow
                    {
                        Row = currentRow
                    };

                    rows.Add(currentMatrixRow);
                    currentRow = new List<Rectangle>();
                }
            }

            return rows;
        }

        private Image<Gray, byte> GetEnlargedBoundingRoiImage(Image<Gray, byte> playingField, Rectangle roi,
            int rowSize)
        {
            Rectangle enlargedRectangle = roi;
            enlargedRectangle.Height = rowSize;

            Image<Gray, byte> boundary = playingField.Copy(enlargedRectangle);
            return boundary;
        }

        private IEnumerable<Rectangle> GetBoundariesForProcessedImage(Mat edges)
        {
            List<Rectangle> boxList = new List<Rectangle>();
            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(edges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                int contourCount = contours.Size;
                for (int i = 0; i < contourCount; i++)
                {
                    using (VectorOfPoint approxContour = new VectorOfPoint())
                    using (VectorOfPoint approx = contours[i])
                    {
                        CvInvoke.ApproxPolyDP(approx, approxContour, CvInvoke.ArcLength(approx, true) * 0.035, true);
                        Point[] pts = approxContour.ToArray();
                        LineSegment2D[] edgesList = PointCollection.PolyLine(pts, true);
                        double contourArea = CvInvoke.ContourArea(approxContour, true);
                        if (contourArea >= 500 && contourArea <= edges.Width * edges.Height / 5.0)
                        {
                            Rectangle contourRectangle = GetRectangleFromContour(approxContour, edgesList);
                            if (contourRectangle != default(Rectangle))
                            {
                                boxList.Add(contourRectangle);
                            }
                        }
                    }
                }
            }

            return boxList;
        }

        private Rectangle GetRectangleFromContour(VectorOfPoint approxContour, LineSegment2D[] edgesList)
        {
            if (approxContour.Size > 2)
            {
                bool isRectangle = true;

                for (int j = 0; j < edgesList.Length; j++)
                {
                    double angle = Math.Abs(edgesList[(j + 1) % edgesList.Length]
                        .GetExteriorAngleDegree(edgesList[j]));

                    if (angle < 85 || angle > 95)
                    {
                        isRectangle = false;
                        break;
                    }
                }

                if (isRectangle)
                {
                    RotatedRect currentRectangle = CvInvoke.MinAreaRect(approxContour);
                    Rectangle minRectangle = currentRectangle.MinAreaRect();
                    return minRectangle;
                }
            }

            return default(Rectangle);
        }

        private Mat ScaleImage(CvArray<byte> image)
        {
            if (image.Width * image.Height >= 900_000)
            {
                return image.Mat;
            }

            Mat scaledImage = new Mat();
            CvInvoke.Resize(image, scaledImage, Size.Empty, 1.5, 1.5);

            return scaledImage;
        }

        private Mat ApplyPreProcessing(Mat image)
        {
            Mat detectedEdges = new Mat();
            CvInvoke.GaussianBlur(image, detectedEdges, new Size(1, 1), 1);
            CvInvoke.Canny(detectedEdges, detectedEdges, Treshold, Treshold * TresholdRation);
            CvInvoke.Dilate(detectedEdges, detectedEdges, new Mat(), new Point(-1, -1), 3, BorderType.Default, new MCvScalar(255, 255, 255));

            return detectedEdges;
        }
    }
}
