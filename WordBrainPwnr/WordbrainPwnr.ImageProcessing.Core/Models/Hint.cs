using System.Collections.Generic;
using System.Drawing;

namespace WordbrainPwnr.ImageProcessing.Core.Models
{
    public class Hint
    {
        public int HintSize { get; set; }

        public bool IsOcrCandidate { get; set; }

        public byte[] OcrCandidate { get; set; }

        public IEnumerable<Rectangle> BoundingBoxes { get; set; }
    }
}
