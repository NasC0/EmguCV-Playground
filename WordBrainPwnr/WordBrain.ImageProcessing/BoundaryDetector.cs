using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using WordbrainPwnr.ImageProcessing.Core;
using WordbrainPwnr.ImageProcessing.Core.Models;

namespace WordBrain.ImageProcessing
{
    public class BoundaryDetector : IBoundaryDetector
    {
        public const int Treshold = 50;
        public const int TresholdRatio = 3;

        public PlayingFieldData GetBoundaries(byte[] playingField)
        {
            Image<Gray, byte> grayscalePlayingField;
            using (MemoryStream imageStream = new MemoryStream(playingField))
            {
                Bitmap convertedImage = (Bitmap) Image.FromStream(imageStream);
                grayscalePlayingField = new Image<Gray, byte>(convertedImage);
            }

            Mat edges = ApplyPreProcessing(grayscalePlayingField);
            List<Rectangle> boxList = GetBoundariesForProcessedImage(edges).ToList();
            int boxListCount = boxList.Count;
            int splitBoxesCount = (boxListCount / 2);

            IEnumerable<Rectangle> hintBoxes = boxList
                .Take(splitBoxesCount)
                .ToList();

            IEnumerable<Rectangle> characterBoxes = boxList
                .Skip(splitBoxesCount)
                .Take(splitBoxesCount)
                .ToList();

            GetCharacters(grayscalePlayingField, characterBoxes);
            IEnumerable<Hint> hints = GetHintCandidates(grayscalePlayingField, hintBoxes);

            return new PlayingFieldData
            {
                CharacterBoundaries = characterBoxes,
                HintBoundaries = hintBoxes
            };
        }

        private void GetCharacters(Image<Gray, byte> playingField, IEnumerable<Rectangle> characterBoundaries)
        {
            List<Rectangle> characterBoundariesList = characterBoundaries.Reverse().ToList();
            int maxRowSize = characterBoundariesList.Max(c => c.Height);

            Image<Gray, byte> combine =
                GetEnlargedBoundingRoiImage(playingField, characterBoundariesList.FirstOrDefault(), maxRowSize);

            for (int i = 1; i < characterBoundariesList.Count; i++)
            {
                Rectangle currentRectangle = characterBoundariesList[i];
                Image<Gray, byte> enlargedBoundingImage = GetEnlargedBoundingRoiImage(playingField, currentRectangle, maxRowSize);
                CvInvoke.HConcat(combine, enlargedBoundingImage, combine);
            }
            
            combine.Save("../../result.png");
        }

        private IEnumerable<Hint> GetHintCandidates(Image<Gray, byte> playingField, IEnumerable<Rectangle> hintsBoundaries)
        {
            IEnumerable<Hint> hints = GetHints(hintsBoundaries);
            return hints;
        }

        private IEnumerable<Hint> GetHints(IEnumerable<Rectangle> hintsBoundaries)
        {
            List<Rectangle> hintsBoundariesList = hintsBoundaries.Reverse().ToList();
            int maxHintBoxWidth = hintsBoundariesList.Max(h => h.Width);
            int approximateMaxDistanceBetweenHints = maxHintBoxWidth / 2;

            List<Hint> hints = new List<Hint>();
            List<Rectangle> maxCurrentCandidate = new List<Rectangle>();
            for (int i = 0; i < hintsBoundariesList.Count; i++)
            {
                Rectangle currentRectangle = hintsBoundariesList[i];
                maxCurrentCandidate.Add(currentRectangle);

                if (i == hintsBoundariesList.Count - 1)
                {
                    Hint hint = new Hint()
                    {
                        HintSize = maxCurrentCandidate.Count,
                        BoundingBoxes = maxCurrentCandidate
                    };

                    hints.Add(hint);
                    break;
                }

                Rectangle nextRectangle = hintsBoundariesList[i + 1];
                int distanceBetweenPreviousXEndPointAndCurrentStartingPoint = nextRectangle.X - (currentRectangle.X + currentRectangle.Width);
                int widthDifference = Math.Abs(nextRectangle.Width - currentRectangle.Width);

                int distanceBetweenPreviousYEndPointAndCurrentStartingPoint = nextRectangle.Y - (currentRectangle.Y + currentRectangle.Height);
                int heightDifference = Math.Abs(nextRectangle.Height - currentRectangle.Height);

                if (distanceBetweenPreviousXEndPointAndCurrentStartingPoint >= widthDifference + approximateMaxDistanceBetweenHints ||
                    distanceBetweenPreviousYEndPointAndCurrentStartingPoint > heightDifference)
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

            return hints;
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

        private Mat ApplyPreProcessing(CvArray<byte> image)
        {
            Mat imageMat = image.Mat;
            
            Mat detectedEdges = new Mat();
            CvInvoke.GaussianBlur(imageMat, detectedEdges, new Size(1, 1), 1);
            CvInvoke.Canny(detectedEdges, detectedEdges, Treshold, Treshold * 3);
            CvInvoke.Dilate(detectedEdges, detectedEdges, new Mat(), new Point(-1, -1), 3, BorderType.Default, new MCvScalar(255, 255, 255));

            return detectedEdges;
        }
    }
}
