using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using WordBrain.ImageProcessing;
using WordBrain.ImageProcessing.Contracts;
using WordBrain.ImageProcessing.Matchers;
using WordbrainPwnr.ImageProcessing.Core;
using WordbrainPwnr.ImageProcessing.Core.Models;

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
                Image image = Image.FromFile("../../Screenshot_20180530-100638_WordBrain.jpg");
                image.Save(ms, ImageFormat.Png);
                byte[] byteArray = ms.ToArray();

                byte[] detectedPlayingField = surfDetector.DetectPlayingField(byteArray);
                IBoundaryDetector boundaryDetector = new BoundaryDetector();
                PlayingFieldData boundaries = boundaryDetector.GetBoundaries(detectedPlayingField);
            }
        }
    }
}
