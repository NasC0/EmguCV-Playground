using Emgu.CV;
using Emgu.CV.Util;

namespace WordBrain.ImageProcessing.Models
{
    public class SurfData
    {
        public VectorOfKeyPoint KeyPoints { get; set; }

        public Mat Descriptors { get; set; }
    }
}