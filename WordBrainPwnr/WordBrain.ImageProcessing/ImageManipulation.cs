using System.Drawing;
using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;
using WordbrainPwnr.ImageProcessing.Core;

namespace WordBrain.ImageProcessing
{
    public class ImageManipulation : IImageManipulation
    {
        public byte[] ConvertToGrayscale(byte[] imageArray)
        {
            byte[] grayscaleImage;
            using (MemoryStream memoryStream = new MemoryStream(imageArray))
            {
                Bitmap image = (Bitmap) Image.FromStream(memoryStream);
                Image<Gray, byte> convertedToGrayscale = new Image<Gray, byte>(image);

                grayscaleImage = convertedToGrayscale.ToJpegData();
            }

            return grayscaleImage;
        }
    }
}
