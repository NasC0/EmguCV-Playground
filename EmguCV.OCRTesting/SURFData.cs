using Emgu.CV;
using Emgu.CV.Util;

namespace EmguCV.OCRTesting
{
    public class SURFData
    {
        public VectorOfKeyPoint KeyPoints { get; set; }

        public Mat Descriptors { get; set; }
    }
}
