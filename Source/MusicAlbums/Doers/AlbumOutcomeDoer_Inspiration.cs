using MusicAlbums;
using RimWorld;
using Verse;

namespace MusicAlbums.Doers
{
    // The actual roll happens in the job driver's finish action where the pawn is in scope - this doer just holds the chance table and the benefits string.
    public class AlbumOutcomeDoer_Inspiration : AlbumOutcomeDoer
    {
        private static readonly float[] ChanceByQuality = new float[]
        {
            0.00f, // Awful
            0.00f, // Poor
            0.00f, // Normal
            0.04f, // Good
            0.06f, // Excellent
            0.10f, // Masterwork
            0.15f, // Legendary
        };

        public float Chance => ChanceByQuality[(int)Quality];

        public override bool DoesProvideOutcome(Pawn listener)
        {
            if (listener?.mindState?.inspirationHandler == null) return false;
            if (listener.mindState.inspirationHandler.Inspired) return false;
            return Chance > 0f;
        }

        public override string GetBenefitsString(Pawn listener = null)
        {
            float chance = Chance;
            if (chance <= 0f) return "";
            return string.Format(" - {0}: {1}", "AlbumInspirationChance".Translate(), chance.ToStringPercent());
        }
    }
}
