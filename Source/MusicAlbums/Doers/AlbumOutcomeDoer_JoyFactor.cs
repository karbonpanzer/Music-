using RimWorld;
using Verse;

namespace MusicAlbums
{
    // Displays the quality-scaled joy multiplier in the ITab benefits section. The actual joyFactor is set on MusicAlbum during GenerateAlbum from the quality curve and this doer only handles the UI string, same as how book doers expose their effect via GetBenefitsString for BookUIUtility to show.
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
