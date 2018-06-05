using Emgu.CV.Util;
using WordBrain.ImageProcessing.Models;

namespace WordBrain.ImageProcessing.Contracts
{
    public interface IMatcher
    {
        VectorOfVectorOfDMatch GetMatchesForModel(SurfData scene, SurfData model);
    }
}
