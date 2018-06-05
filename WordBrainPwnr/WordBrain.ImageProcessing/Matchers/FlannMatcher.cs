using System.Linq;
using Emgu.CV.Features2D;
using Emgu.CV.Flann;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using WordBrain.ImageProcessing.Contracts;
using WordBrain.ImageProcessing.Models;

namespace WordBrain.ImageProcessing.Matchers
{
    public class FlannMatcher : IMatcher
    {
        private readonly int _matchLimit;
        private readonly int _kConstant;
        private readonly HierarchicalClusteringIndexParams _hierarchicalParams;
        private readonly SearchParams _searchParams;

        public FlannMatcher(int matchLimit, int kConstant)
            : this(matchLimit, kConstant, new HierarchicalClusteringIndexParams(), new SearchParams())
        {
        }

        public FlannMatcher(int matchLimit, int kConstant, HierarchicalClusteringIndexParams hierarchicalParams, SearchParams searchParams)
        {
            _matchLimit = matchLimit;
            _kConstant = kConstant;
            _hierarchicalParams = hierarchicalParams;
            _searchParams = searchParams;
        }

        public VectorOfVectorOfDMatch GetMatchesForModel(SurfData scene, SurfData model)
        {
            using (FlannBasedMatcher matcher =
                new FlannBasedMatcher(_hierarchicalParams, _searchParams))
            {

                matcher.Add(model.Descriptors);

                VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch();
                matcher.KnnMatch(scene.Descriptors, matches, _kConstant, null);

                MDMatch[][] newMatches = matches
                    .ToArrayOfArray()
                    .OrderBy(m => m[0].Distance)
                    .Take(_matchLimit)
                    .ToArray();

                VectorOfVectorOfDMatch limitMatches = new VectorOfVectorOfDMatch(newMatches);
                matches.Dispose();
                return limitMatches;
            }
        }
    }
}
