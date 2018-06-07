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

            foreach (Rectangle rectangle in characterBoxes)
            {
                grayscalePlayingField.Draw(rectangle, new Gray(50));
            }

            ImageViewer.Show(grayscalePlayingField);

            return new PlayingFieldData
            {
                CharacterBoundaries = characterBoxes,
                HintBoundaries = hintBoxes
            };
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
