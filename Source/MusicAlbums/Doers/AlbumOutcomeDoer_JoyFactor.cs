using MusicAlbums.Utility;
using RimWorld;
using Verse;

namespace MusicAlbums.Doers
{
    // This doer exists purely to show the joy multiplier in the ITab benefits list. The actual joyFactor value gets set by AlbumOutcomeDoer_JoyFactorModifier at generation time.
    // I split them because the modifier doer needs to run at generation and the display doer needs to run at UI time, and mixing those two responsibilities into one class felt wrong.
    public class AlbumOutcomeDoer_JoyFactor : AlbumOutcomeDoer
    {
        public override bool DoesProvideOutcome(Pawn listener) => false;

        public override string GetBenefitsString(Pawn listener = null)
        {
            float factor = AlbumUtility.GetAlbumJoyFactorForQuality(Quality);
            return "AlbumJoyFactor".Translate() + ": x" + factor.ToString("F2");
        }
    }
}
