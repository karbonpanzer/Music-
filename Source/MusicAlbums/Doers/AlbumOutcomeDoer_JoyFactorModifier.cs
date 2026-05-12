using MusicAlbums.Utility;
using RimWorld;
using Verse;

namespace MusicAlbums.Doers
{
    public class AlbumOutcomeDoer_JoyFactorModifier : AlbumOutcomeDoer
    {
        public override bool DoesProvideOutcome(Pawn listener) => true;

        public override void OnAlbumGenerated(Pawn artist = null)
        {
            Album.SetJoyFactor(AlbumUtility.GetAlbumJoyFactorForQuality(Quality));
        }

        public override string GetBenefitsString(Pawn listener = null)
        {
            return string.Format(" - {0}: x{1}", "AlbumJoyFactor".Translate(), Album.JoyFactor.ToString("F2"));
        }
    }
}
