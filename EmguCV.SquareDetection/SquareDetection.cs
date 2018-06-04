using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;

namespace EmguCV.SquareDetection
{
    public static class SquareDetection
    {
        // Adapted from https://github.com/alyssaq/opencv

        public const int Treshold = 50;
        public const int TresholdRatio = 3;

        public static void Main()
        {
            using (Mat sourceImage = CvInvoke.Imread("../../../characters/characters-and-clues-result.jpg"))
            {
                //Mat scaledImage = new Mat();
                //CvInvoke.Resize(sourceImage, scaledImage, Size.Empty, 1.5, 1.5);

                List<Rectangle> detectedRectangles = DetectSquares(sourceImage)
                    .ToList();

                Image<Bgr, byte> destinationImage = sourceImage.ToImage<Bgr, byte>();

                //destinationImage.Draw(detectedRectangles[15], new Bgr(Color.DarkOrange), 2);
                //destinationImage.Draw(detectedRectangles[16], new Bgr(Color.DarkOrange), 2);

                foreach (Rectangle rectangle in detectedRectangles)
                {
                    destinationImage.Draw(rectangle, new Bgr(Color.DarkOrange), 1);
                }

                ImageViewer.Show(destinationImage);
                destinationImage.Save("../../../characters/characters-and-clues-result-two.jpg");
            }
        }

        public static IEnumerable<Rectangle> DetectSquares(Mat sourceImage)
        {
            Mat destinationImage = new Mat();
            destinationImage.Create(sourceImage.Rows, sourceImage.Cols, sourceImage.Depth, 1);
            Mat greyscaleImage = new Mat();
            CvInvoke.CvtColor(sourceImage, greyscaleImage, ColorConversion.Bgr2Gray);

            Mat detectedEdges = new Mat();
            CvInvoke.GaussianBlur(greyscaleImage, detectedEdges, new Size(1, 1), 1);
            CvInvoke.Canny(detectedEdges, detectedEdges, Treshold, Treshold * 3);
            CvInvoke.Dilate(detectedEdges, detectedEdges, new Mat(), new Point(-1, -1), 3, BorderType.Default, new MCvScalar(255, 255, 255));

            //ImageViewer.Show(detectedEdges);

            List<Rectangle> boxList = new List<Rectangle>();
            List<LineSegment2D> lines = new List<LineSegment2D>();

            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(detectedEdges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                int count = contours.Size;
                for (int i = 0; i < count; i++)
                {
                    using (VectorOfPoint approxContour = new VectorOfPoint())
                    using (VectorOfPoint approx = contours[i])
                    {
                        CvInvoke.ApproxPolyDP(approx, approxContour, CvInvoke.ArcLength(approx, true) * 0.035, true);
                        Point[] pts = approxContour.ToArray();
                        LineSegment2D[] edges = PointCollection.PolyLine(pts, true);
                        lines.AddRange(edges);

                        if (CvInvoke.ContourArea(approxContour, true) > 500)
                        {
                            if (approxContour.Size >= 2)
                            {
                                bool isRectangle = true;

                                for (int j = 0; j < edges.Length; j++)
                                {
                                    double angle = Math.Abs(edges[(j + 1) % edges.Length]
                                        .GetExteriorAngleDegree(edges[j]));

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
                                    //int ninetyPercentWidth = minRectangle.Width - (int)(minRectangle.Width * 0.05);
                                    //int ninetyPercentHeight = minRectangle.Height - (int)(minRectangle.Height * 0.05);
                                    //minRectangle.Size = new Size(ninetyPercentWidth, ninetyPercentHeight);
                                    //minRectangle.Offset(5, 5);
                                    boxList.Add(minRectangle);
                                }
                            }
                        }
                    }
                }
            }

            return boxList;
        }
    }
}
