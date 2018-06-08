using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;
using Emgu.CV.XFeatures2D;

namespace EmguCV.OCRTesting
{
    public class Program
    {
        static void Main()
        {
            //using (Mat image = CvInvoke.Imread("../../characters/higher-res.jpg"))
            //{
            //    var scaledImage = new Mat();
            //    CvInvoke.Resize(image, scaledImage, Size.Empty, 1.5, 1.5);

            //    DetectLinesInImage(scaledImage);
            //}

            //using (Mat toolbarObject = CvInvoke.Imread("../../../SURF_Resources/SURF_Toolbar_Mask.png", ImreadModes.Grayscale))
            //using (Mat wordbrainObject = CvInvoke.Imread("../../../SURF_Resources/SURF_Wordbrain_Mask.png", ImreadModes.Grayscale))
            //using (Mat scene = CvInvoke.Imread("../../../characters/Screenshot_20180530-100638_WordBrain.jpg", ImreadModes.Grayscale))
            //{
            //    CompareImages(scene, toolbarObject, wordbrainObject);
            //    //DetectKeyPoints(scene);
            //}

            Console.WriteLine("Starting image recognition");

            Task.Factory.StartNew(() =>
            {
                using (Image<Bgr, byte> image =
                    new Image<Bgr, byte>(Path.GetFullPath("../../../characters/result.png")))
                {
                    using (Tesseract tesseractOcrProvider =
                        new Tesseract("C:\\Program Files (x86)\\Tesseract-OCR\\tessdata", "eng", OcrEngineMode.TesseractOnly))
                    {
                        tesseractOcrProvider.SetImage(image);
                        tesseractOcrProvider.Recognize();
                        Tesseract.Character[] characters = tesseractOcrProvider.GetCharacters();
                        string text = tesseractOcrProvider.GetBoxText();
                        Console.WriteLine(text);
                    }
                }
            }).Wait();

            Console.WriteLine("Called async image recognition");
        }

        private static void CompareImages(Mat scene, Mat toolbarObject, Mat wordbrainObject)
        {
            SURFData toolbarSurfResults = ExecuteSurfDetection(toolbarObject);
            SURFData wordbrainSurfResults = ExecuteSurfDetection(wordbrainObject);
            SURFData sceneSurfResults = ExecuteSurfDetection(scene);

            Mat drawnMatches = new Mat();

            VectorOfVectorOfDMatch toolbarMatchResults = GetSceneMatchesForModel(sceneSurfResults, toolbarSurfResults);
            VectorOfVectorOfDMatch wordbrainMatchResults = GetSceneMatchesForModel(sceneSurfResults, wordbrainSurfResults);
            MKeyPoint[] sceneKeyPoints = sceneSurfResults.KeyPoints.ToArray();

            Point highestKeyPoint = toolbarMatchResults.ToArrayOfArray()
                .Select(m => Point.Round(sceneKeyPoints[m[0].QueryIdx].Point))
                .OrderBy(kp => kp.Y)
                .FirstOrDefault();

            Point lowestKeyPoint = wordbrainMatchResults.ToArrayOfArray()
                .Select(m => Point.Round(sceneKeyPoints[m[0].QueryIdx].Point))
                .OrderByDescending(kp => kp.Y)
                .FirstOrDefault();

            int rectangleHeight = highestKeyPoint.Y - lowestKeyPoint.Y;
            
            Image<Gray, byte> sceneImage = scene.ToImage<Gray, Byte>();
            Console.WriteLine(sceneImage.Width);
            Console.WriteLine(sceneImage.Height);
            
            Rectangle rectangle = new Rectangle(0, lowestKeyPoint.Y, scene.Width, rectangleHeight);

            //sceneImage.Draw("X", highestKeyPoint, FontFace.HersheyPlain, 5, new Gray(255), thickness: 2);
            //sceneImage.Draw("X", lowestKeyPoint, FontFace.HersheyPlain, 5, new Gray(255), thickness: 2);
            //sceneImage.Draw(rectangle, new Gray(10), 5);
            //ImageViewer.Show(sceneImage);
            //Features2DToolbox.DrawMatches(toolbarObject, toolbarSurfResults.KeyPoints, scene,
            //    sceneSurfResults.KeyPoints, limitMatches, drawnMatches, new MCvScalar(255), new MCvScalar(255), null, Features2DToolbox.KeypointDrawType.NotDrawSinglePoints);

            Image<Gray, byte> sliced = sceneImage.Copy(rectangle);
            sliced.Save("../../../characters/characters-and-clues-result.jpg");
            ImageViewer.Show(sliced);
        }

        private static VectorOfVectorOfDMatch GetSceneMatchesForModel(SURFData sceneData, SURFData modelData)
        {
            FlannBasedMatcher matcher =
                new FlannBasedMatcher(new HierarchicalClusteringIndexParams(), new SearchParams());

            matcher.Add(modelData.Descriptors);

            VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch();
            matcher.KnnMatch(sceneData.Descriptors, matches, 1, null);

            MDMatch[][] newMatches = matches
                .ToArrayOfArray()
                .OrderBy(m => m[0].Distance)
                .Take(8)
                .ToArray();

            VectorOfVectorOfDMatch limitMatches = new VectorOfVectorOfDMatch(newMatches);
            matches.Dispose();
            return limitMatches;
        }

        private static SURFData ExecuteSurfDetection(Mat scene)
        {
            using (SURF surfDetector = new SURF(300, 4, 2, false, true))
            {
                Mat sceneDescriptors = new Mat();
                VectorOfKeyPoint sceneKeyPoints = new VectorOfKeyPoint();
                surfDetector.DetectAndCompute(scene, null, sceneKeyPoints, sceneDescriptors, false);

                return new SURFData
                {
                    KeyPoints = sceneKeyPoints,
                    Descriptors = sceneDescriptors
                };
            }
        }

        private static void DetectKeyPointsCuda(Image<Bgr, byte> image)
        {
            Console.WriteLine(CudaInvoke.HasCuda);
            using (GpuMat<byte> gpuImage = new GpuMat<byte>(image.Mat))
            {
                CudaSURF surfDetector = new CudaSURF(400, 4, 2, false);
                MKeyPoint[] features = surfDetector.DetectKeyPoints(gpuImage, null);

                using (Image<Bgr, byte> imageWithFeatures = new Image<Bgr, byte>(image.Bitmap))
                {
                    foreach (MKeyPoint mKeyPoint in features)
                    {
                        imageWithFeatures.Draw(".", new Point((int)mKeyPoint.Point.X, (int)mKeyPoint.Point.Y),
                            FontFace.HersheyComplex, 1, new Bgr(Color.Gray));
                    }

                    ImageViewer.Show(imageWithFeatures);
                }
            }
        }

        private static void DetectRectangles(Image<Bgr, byte> image)
        {
            UMat uImage = new UMat();
            CvInvoke.CvtColor(image, uImage, ColorConversion.Bgr2Gray);

            UMat pyrDown = new UMat();
            CvInvoke.PyrDown(uImage, pyrDown);
            CvInvoke.PyrUp(pyrDown, uImage);

            double cannyThresholdLinking = 3.0;
            double cannyThreshold = 100.0;
            UMat cannyEdges = new UMat();
            CvInvoke.Canny(uImage, cannyEdges, cannyThreshold, cannyThresholdLinking);
            ImageViewer.Show(cannyEdges);

            LineSegment2D[] lines = CvInvoke.HoughLinesP(
                cannyEdges,
                1, //Distance resolution in pixel-related units
                Math.PI / 45.0, //Angle resolution measured in radians.
                20, //threshold
                30, //min Line width
                10); //gap between lines

            List<Rectangle> boxList = new List<Rectangle>();

            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
                CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                int count = contours.Size;
                for (int i = 0; i < count; i++)
                {
                    using (VectorOfPoint contour = contours[i])
                    using (VectorOfPoint approxContour = new VectorOfPoint())
                    {
                        CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.05, true);
                        if (CvInvoke.ContourArea(approxContour, true) > 150)
                        {
                            if (approxContour.Size == 4)
                            {
                                bool isRectangle = true;
                                Point[] pts = approxContour.ToArray();
                                LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

                                for (int j = 0; j < edges.Length; j++)
                                {
                                    double angle = Math.Abs(edges[(j + 1) % edges.Length]
                                        .GetExteriorAngleDegree(edges[j]));

                                    if (angle < 80 || angle > 100)
                                    {
                                        isRectangle = false;
                                        break;
                                    }
                                }

                                if (isRectangle)
                                {
                                    RotatedRect currentRectangle = CvInvoke.MinAreaRect(approxContour);
                                    Rectangle minRectangle = currentRectangle.MinAreaRect();
                                    int ninetyPercentWidth = minRectangle.Width - (int)(minRectangle.Width * 0.1);
                                    int ninetyPercentHeight = minRectangle.Height - (int)(minRectangle.Height * 0.1);
                                    minRectangle.Size = new Size(ninetyPercentWidth, ninetyPercentHeight);
                                    minRectangle.Offset(5, 5);
                                    boxList.Add(minRectangle);
                                }
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < boxList.Count; i++)
            {
                var rotatedRect = boxList[i];

                image.Draw(rotatedRect, new Bgr(Color.DarkOrange), 2);
            }
            //foreach (RotatedRect rotatedRect in boxList)
            //{
            //    if (rotatedRect.Angle > -90 && rotatedRect.Angle < -85)
            //    {
            //        rotatedRect.Angle = -90;
            //    }

            //    image.Draw(rotatedRect, new Bgr(Color.DarkOrange), 2);
            //}
            
            ImageViewer.Show(image);
        }
    }
}
