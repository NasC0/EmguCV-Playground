using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Emgu.CV;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using WordbrainPwnr.ImageProcessing.Core;

namespace WordBrain.ImageProcessing
{
    public class TesseractOcrProcessor : IOcrProcessor
    {
        private readonly string _tesseractPath;
        private readonly string _language;

        public TesseractOcrProcessor(string tesseractPath, string language)
        {
            _tesseractPath = tesseractPath;
            _language = language;
        }

        public IEnumerable<string> GetCharactersFromImage(byte[] imageData)
        {
            List<string> recognizedCharacters = new List<string>();

            using (MemoryStream memoryStream = new MemoryStream(imageData))
            {
                Bitmap bitmapImage = (Bitmap) Image.FromStream(memoryStream);
                Image<Gray, byte> image = new Image<Gray, byte>(bitmapImage);

                using (Tesseract tesseract = new Tesseract(_tesseractPath, _language, OcrEngineMode.TesseractOnly))
                {
                    tesseract.SetVariable("tessedit_char_whitelist", "QWERTYUIOPASDFGHJKLZXCVBNM");
                    tesseract.SetImage(image);
                    tesseract.Recognize();
                    IEnumerable<Tesseract.Character> characters = DiscardUncenteredCharacters(tesseract.GetCharacters());

                    foreach (Tesseract.Character character in characters)
                    {
                        if (!string.IsNullOrWhiteSpace(character.Text))
                        {
                            recognizedCharacters.Add(character.Text);
                        }
                    }
                }
            }

            return recognizedCharacters;
        }

        private IEnumerable<Tesseract.Character> DiscardUncenteredCharacters(
            IEnumerable<Tesseract.Character> characters)
        {
            List<Tesseract.Character> leftoverCharacters = new List<Tesseract.Character>();
            double minBottom = characters.Average(c => c.Region.Bottom) / 2;

            foreach (Tesseract.Character character in characters)
            {
                if (character.Region.Bottom < minBottom)
                {
                    continue;
                }

                leftoverCharacters.Add(character);
            }

            return leftoverCharacters;
        }
    }
}
