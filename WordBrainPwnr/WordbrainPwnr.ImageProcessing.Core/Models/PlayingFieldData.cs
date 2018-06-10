using System.Collections.Generic;
using System.Drawing;

namespace WordbrainPwnr.ImageProcessing.Core.Models
{
    public class PlayingFieldData
    {
        public Characters Characters { get; set; }

        public IEnumerable<Hint> Hints { get; set; }
    }
}
