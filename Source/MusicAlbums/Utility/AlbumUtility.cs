using System.Collections.Generic;
using MusicAlbums.Comps;
using RimWorld;
using Verse;
using Verse.AI;

namespace MusicAlbums.Utility
{
    public static class AlbumUtility
    {
        private static readonly List<Thing> TmpCandidates = new List<Thing>();
        private static readonly List<Thing> TmpOutcomeCandidates = new List<Thing>();

        // I'm matching BookUtility.QualityJoyFactor exactly. CDs hit the same quality tiers as novels and there's no reason to make music feel mechanically weaker or stronger.
        private static readonly SimpleCurve QualityJoyFactor = new SimpleCurve
        {
            new CurvePoint(0f, 1.2f),
            new CurvePoint(1f, 1.4f),
            new CurvePoint(2f, 1.6f),
            new CurvePoint(3f, 1.8f),
            new CurvePoint(4f, 2.0f),
            new CurvePoint(5f, 2.25f),
            new CurvePoint(6f, 2.5f),
        };

        public static float GetAlbumJoyFactorForQuality(QualityCategory quality) =>
            QualityJoyFactor.Evaluate((int)quality);

        // Reusing ReadingBonus here rather than adding a new room stat. A nice room improves listening for the same reasons it improves reading, and one fewer stat to define is better.
        public static float GetListeningBonus(Thing album)
        {
            Room room = album.GetRoom();
            if (room != null && room.ProperRoom && !room.PsychologicallyOutdoors)
                return room.GetStat(RoomStatDefOf.ReadingBonus);
            return 1f;
        }

        // Gating on Hearing so deaf pawns won't seek albums on their own. A player can still force a deaf pawn to listen manually if they want to, this just stops autonomous seeking.
        public static bool CanListenEver(Pawn listener)
        {
            if (listener.DevelopmentalStage == DevelopmentalStage.Baby)
                return false;
            if (!listener.health.capacities.CapableOf(PawnCapacityDefOf.Hearing))
                return false;
            return true;
        }

        // ReadingSpeed doubles as a media engagement stat here. I didn't want to define a new ListeningSpeed stat just to mirror what ReadingSpeed already does for books.
        public static bool CanListenNow(Pawn listener)
        {
            if (!CanListenEver(listener)) return false;
            if (listener.GetStatValue(StatDefOf.ReadingSpeed) <= 0f) return false;
            return true;
        }

        public static bool CanListenToAlbum(MusicAlbum album, Pawn listener, out string reason)
        {
            DevelopmentalStage stageFilter = album.AlbumComp.Props.developmentalStageFilter;
            if (!stageFilter.HasAny(listener.DevelopmentalStage))
            {
                reason = "BookCantBeStage".Translate(
                    listener.Named("PAWN"),
                    stageFilter.ToCommaList().Named("STAGES"));
                return false;
            }

            if (!CanListenEver(listener))
            {
                reason = "AlbumCantListen".Translate(listener.Named("PAWN"));
                return false;
            }

            reason = null;
            return true;
        }

        // ThingRequestGroup.Book is tied to the vanilla Book ThingClass so I can't use it here. HaulableAlways covers everything that can be picked up, and I filter down to MusicAlbum instances manually. It's a broader search than I'd like but there's no better group.
        public static bool TryGetRandomAlbumToListen(Pawn pawn, out MusicAlbum album)
        {
            album = null;
            TmpCandidates.Clear();
            TmpOutcomeCandidates.Clear();

            foreach (Thing t in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableAlways))
                if (t is MusicAlbum && IsValidAlbum(t, pawn))
                    TmpCandidates.Add(t);

            if (TmpCandidates.Count == 0)
            {
                TmpCandidates.Clear();
                return false;
            }

            // Prefer albums that still have an outcome to give this pawn. If nothing qualifies I fall back to any valid album so listening still happens even if benefits are spent.
            foreach (Thing t in TmpCandidates)
                if (t is MusicAlbum a && a.ProvidesOutcome(pawn))
                    TmpOutcomeCandidates.Add(t);

            album = (MusicAlbum)(TmpOutcomeCandidates.Count > 0
                ? TmpOutcomeCandidates.RandomElement()
                : TmpCandidates.RandomElement());

            TmpCandidates.Clear();
            TmpOutcomeCandidates.Clear();
            return true;
        }

        // I'm skipping pawn.reading policy filters deliberately. Albums aren't books and I don't want players to need to configure a separate reading policy just to let pawns listen.
        private static bool IsValidAlbum(Thing thing, Pawn pawn)
        {
            if (thing is not MusicAlbum) return false;
            if (thing.IsForbiddenHeld(pawn)) return false;
            if (!pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.None)) return false;
            if (thing.Fogged()) return false;
            if (!thing.IsPoliticallyProper(pawn)) return false;
            if (thing.VacuumConcernTo(pawn)) return false;
            return true;
        }

        public static MusicAlbum MakeAlbum(ArtGenerationContext context,
            QualityGenerator? qualityGenerator = null)
        {
            ThingDef def = GetAlbumDefs().RandomElementByWeight(
                x => x.GetCompProperties<CompProperties_MusicAlbum>().pickWeight);
            return MakeAlbum(def, context, qualityGenerator);
        }

        public static MusicAlbum MakeAlbum(ThingDef def, ArtGenerationContext context,
            QualityGenerator? qualityGenerator = null)
        {
            Thing thing = ThingMaker.MakeThing(def, GenStuff.RandomStuffFor(def));
            CompQuality cq = thing.TryGetComp<CompQuality>();
            if (cq != null)
            {
                QualityCategory q = qualityGenerator.HasValue
                    ? QualityUtility.GenerateQuality(qualityGenerator.Value)
                    : QualityUtility.GenerateQualityRandomEqualChance();
                cq.SetQuality(q, context);
            }
            return thing as MusicAlbum;
        }

        public static List<ThingDef> GetAlbumDefs()
        {
            List<ThingDef> result = new List<ThingDef>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                if (def.HasComp<CompMusicAlbum>())
                    result.Add(def);
            return result;
        }
    }
}
