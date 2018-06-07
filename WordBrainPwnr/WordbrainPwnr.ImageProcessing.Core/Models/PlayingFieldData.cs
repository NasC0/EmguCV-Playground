using System.Collections.Generic;
using System.Drawing;

namespace WordbrainPwnr.ImageProcessing.Core.Models
{
    public class PlayingFieldData
    {
        public IEnumerable<Rectangle> CharacterBoundaries { get; set; }

        public IEnumerable<Rectangle> HintBoundaries { get; set; }
    }
}
