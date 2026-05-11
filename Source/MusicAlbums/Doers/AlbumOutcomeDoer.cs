using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Grammar;

namespace MusicAlbums
{
    public abstract class AlbumOutcomeDoer : ReadingOutcomeDoer
    {
        public new AlbumOutcomeProperties Props => (AlbumOutcomeProperties)props;
        public MusicAlbum Album => (MusicAlbum)Parent;

        private CompQuality compQualityCached;
        private CompQuality CompQuality =>
            compQualityCached ?? (compQualityCached = Parent.GetComp<CompQuality>());

        public QualityCategory Quality =>
            CompQuality?.Quality ?? QualityCategory.Normal;

        public abstract bool DoesProvideOutcome(Pawn listener);

        public virtual void OnAlbumGenerated(Pawn artist = null) { }
        public virtual string GetBenefitsString(Pawn listener = null) => "";
        public virtual bool BenefitDetailsCanChange(Pawn listener = null) => false;

        // Keeping this hook open so doers can inject named tokens into the grammar if needed.
        public virtual IEnumerable<Rule_String> GetTopicRuleStrings() => null;

        public virtual IEnumerable<Dialog_InfoCard.Hyperlink> GetHyperlinks()
        {
            yield break;
        }

        public virtual IEnumerable<Gizmo> GetGizmos()
        {
            yield break;
        }

        public virtual void Reset() { }
    }
}
