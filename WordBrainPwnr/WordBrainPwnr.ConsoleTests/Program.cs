using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using WordBrain.ImageProcessing;
using WordBrain.ImageProcessing.Matchers;

namespace WordBrainPwnr.ConsoleTests
{
    class Program
    {
        static void Main()
        {
            using (var ms = new MemoryStream())
            {
                FlannMatcher matcher = new FlannMatcher(8, 1);
                SurfPlayingFieldDetector surfDetector = new SurfPlayingFieldDetector("../../SURF_Resources", matcher);
                Image image = Image.FromFile("../../Screenshot_20180530-100638_WordBrain.jpg");
                image.Save(ms, ImageFormat.Png);
                byte[] byteArray = ms.ToArray();

                using (var @is = new MemoryStream(byteArray))
                {
                    Image newImage = Image.FromStream(@is);
                    newImage.Save("result.png");
                }

                surfDetector.DetectPlayingField(byteArray);
            }
        }
    }
}
