using System.Collections.Generic;

namespace WordbrainPwnr.ImageProcessing.Core
{
    public interface IOcrProcessor
    {
        IEnumerable<string> GetCharactersFromImage(byte[] imageData);
    }
}
