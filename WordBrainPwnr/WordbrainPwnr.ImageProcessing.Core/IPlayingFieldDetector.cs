namespace WordbrainPwnr.ImageProcessing.Core
{
    public interface IPlayingFieldDetector
    {
        byte[] DetectPlayingField(byte[] imageArray);
    }
}
