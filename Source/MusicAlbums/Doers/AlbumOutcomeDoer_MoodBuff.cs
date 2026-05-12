using MusicAlbums;
using RimWorld;
using Verse;

namespace MusicAlbums.Doers
{
    // ThoughtDef needs exactly 7 stages matching QualityCategory order. Buff fires once per session, Reset clears it so the next listen works.
    public class AlbumOutcomeDoer_MoodBuff : AlbumOutcomeDoer
    {
        private float accumulatedFactor;
        private bool buffGranted;

        private AlbumOutcomeProperties_MoodBuff MoodProps =>
            (AlbumOutcomeProperties_MoodBuff)props;

        public override void OnReadingTick(Pawn reader, float factor)
        {
            if (buffGranted) return;
            accumulatedFactor += factor;
            if (accumulatedFactor >= MoodProps.minListenFactor)
                GrantBuff(reader);
        }

        private void GrantBuff(Pawn listener)
        {
            if (MoodProps.thought == null) return;
            if (listener?.needs?.mood == null) return;

            listener.needs.mood.thoughts.memories.TryGainMemory(
                ThoughtMaker.MakeThought(MoodProps.thought, (int)Quality));

            buffGranted = true;
        }

        public override void Reset()
        {
            accumulatedFactor = 0f;
            buffGranted = false;
        }

        public override bool DoesProvideOutcome(Pawn listener) =>
            MoodProps.thought != null && listener?.needs?.mood != null;

        public override string GetBenefitsString(Pawn listener = null)
        {
            if (MoodProps.thought == null) return "";
            ThoughtStage stageDef = MoodProps.thought.stages?[(int)Quality];
            if (stageDef == null) return "";
            return string.Format(" - {0}: {1}", stageDef.label.CapitalizeFirst(), stageDef.baseMoodEffect.ToStringWithSign());
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref accumulatedFactor, "accumulatedFactor", 0f);
            Scribe_Values.Look(ref buffGranted, "buffGranted", false);
        }
    }
}
