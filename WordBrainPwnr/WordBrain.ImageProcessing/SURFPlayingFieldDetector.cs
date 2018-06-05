using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;
using Emgu.CV.XFeatures2D;
using WordBrain.ImageProcessing.Contracts;
using WordBrain.ImageProcessing.Models;
using WordbrainPwnr.ImageProcessing.Core;

namespace WordBrain.ImageProcessing
{
    public class SurfPlayingFieldDetector : IPlayingFieldDetector
    {
        private const int HessianTreshold = 300;
        private const int Octaves = 4;
        private const int OctaveLayers = 2;

        private IEnumerable<Mat> _surfResources;
        private IEnumerable<SurfData> _surfResourcesData;

        public SurfPlayingFieldDetector(string surfResourcesPath, IMatcher matcher)
        {
            Matcher = matcher;
            SurfResourcesPath = surfResourcesPath;
            SurfResources = LoadSurfResources(surfResourcesPath);
            SurfResourcesData = LoadSurfResourcesData(_surfResources);
        }

        public string SurfResourcesPath { get; }

        public IEnumerable<Mat> SurfResources
        {
            get => new List<Mat>(_surfResources);
            private set => _surfResources = value ?? throw new ArgumentNullException(nameof(value));
        }

        public IEnumerable<SurfData> SurfResourcesData
        {
            get => new List<SurfData>(_surfResourcesData);
            private set => _surfResourcesData = value ?? throw new ArgumentNullException(nameof(value));
        }

        public IMatcher Matcher { get; set; }

        public byte[] DetectPlayingField(byte[] imageArray)
        {
            using (MemoryStream stream = new MemoryStream(imageArray))
            {
                Bitmap image = (Bitmap)Image.FromStream(stream);
                Image<Gray, byte> convertedToGrayscale = new Image<Gray, byte>(image);
                Mat sceneMat = convertedToGrayscale.Mat;

                SurfData sceneResources = ExecuteSurfDetection(sceneMat);
                MKeyPoint[] sceneKeypoints = sceneResources.KeyPoints.ToArray();

                List<EdgeData> viableKeyPoints = new List<EdgeData>();
                foreach (SurfData modelSurfData in SurfResourcesData)
                {
                    VectorOfVectorOfDMatch currentMatchResult = Matcher.GetMatchesForModel(sceneResources, modelSurfData);
                    IEnumerable<Point> keyPoints = currentMatchResult.ToArrayOfArray()
                        .Select(m => Point.Round(sceneKeypoints[m[0].QueryIdx].Point))
                        .OrderByDescending(p => p.Y);

                    EdgeData currentEdge = GetEdgeDataForMatchingPoints(keyPoints);
                    if (currentEdge != null)
                    {
                        viableKeyPoints.Add(currentEdge);
                    }
                }

                viableKeyPoints = viableKeyPoints
                    .OrderBy(e => e.HeightSum)
                    .ToList();

                Point topMostPoint = viableKeyPoints
                    .FirstOrDefault()?
                    .BottomMostPoint ?? default(Point);

                Point bottomMostPoint = viableKeyPoints
                    .LastOrDefault()?
                    .UpperMostPoint ?? default(Point);

                int rectangleHeight = bottomMostPoint.Y - topMostPoint.Y;
                Rectangle rectangle = new Rectangle(0, topMostPoint.Y, convertedToGrayscale.Width, rectangleHeight);
                Image<Bgr, byte> limitedImage = new Image<Bgr, byte>(image) {ROI = rectangle};
                limitedImage.Draw(rectangle, new Bgr(Color.DarkOrange), 2);

                byte[] imageByteArray;
                using (MemoryStream limitedImageStream = new MemoryStream())
                {
                    //TODO: Crop bitmap
                    limitedImage.ToBitmap().Save(limitedImageStream, ImageFormat.Png);
                    imageByteArray = limitedImageStream.ToArray();
                }

                return imageByteArray;
            }
        }

        private SurfData ExecuteSurfDetection(Mat scene)
        {
            using (SURF surfDetector = new SURF(HessianTreshold, Octaves, OctaveLayers, false, true))
            {
                Mat sceneDescriptors = new Mat();
                VectorOfKeyPoint sceneKeyPoints = new VectorOfKeyPoint();
                surfDetector.DetectAndCompute(scene, null, sceneKeyPoints, sceneDescriptors, false);

                return new SurfData
                {
                    KeyPoints = sceneKeyPoints,
                    Descriptors = sceneDescriptors
                };
            }
        }

        private List<Mat> LoadSurfResources(string path)
        {
            string[] directoryFiles = Directory.GetFiles(path);
            List<Mat> surfResources = new List<Mat>();
            foreach (var directoryFile in directoryFiles)
            {
                Bitmap imageBitmap = (Bitmap)Image.FromFile(directoryFile);
                surfResources.Add((new Image<Gray, byte>(imageBitmap).Mat));
            }

            return surfResources;
        }

        private List<SurfData> LoadSurfResourcesData(IEnumerable<Mat> surfResources)
        {
            List<SurfData> surfResourcesData = new List<SurfData>();

            foreach (Mat surfResource in surfResources)
            {
                SurfData currentSurfData = ExecuteSurfDetection(surfResource);
                surfResourcesData.Add(currentSurfData);
            }

            return surfResourcesData;
        }

        public EdgeData GetEdgeDataForMatchingPoints(IEnumerable<Point> points)
        {
            var keyPoints = points.ToList();
            int keyPointHeightSum = (keyPoints.Sum(p => p.Y) / keyPoints.Count);
            if (keyPointHeightSum > 0)
            {
                Point bottomMostPoint = keyPoints.FirstOrDefault();
                Point upperMostPoint = keyPoints.LastOrDefault();

                if (upperMostPoint == default(Point))
                {
                    upperMostPoint = bottomMostPoint;
                }

                return new EdgeData
                {
                    HeightSum = keyPointHeightSum,
                    BottomMostPoint = bottomMostPoint,
                    UpperMostPoint = upperMostPoint
                };
            }

            return null;
        }
    }
}
