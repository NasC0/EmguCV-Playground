using System.Drawing;

namespace WordBrain.ImageProcessing.Models
{
    public class EdgeData
    {
        public int HeightSum { get; set; }

        public Point UpperMostPoint { get; set; }

        public Point BottomMostPoint { get; set; }
    }
}
