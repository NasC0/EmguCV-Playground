using WordbrainPwnr.ImageProcessing.Core.Models;

namespace WordbrainPwnr.ImageProcessing.Core
{
    public interface IBoundaryDetector
    {
        PlayingFieldData GetBoundaries(byte[] playingField);
    }
}
