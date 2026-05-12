using MusicAlbums.Utility;
using RimWorld;
using Verse;

namespace MusicAlbums.Doers
{
    // This is the doer that actually writes the joy multiplier onto the album at generation time. AlbumOutcomeDoer_JoyFactor handles displaying it in the UI.
    // They're separate because baking the value in at generation means it's consistent throughout the album's lifetime.
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
