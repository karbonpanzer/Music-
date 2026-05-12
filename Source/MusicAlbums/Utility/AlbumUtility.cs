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

        // Same curve values as BookUtility.QualityJoyFactor — CDs hit the same artistic quality tiers as novels so there's no reason to differ.
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

        // Reusing the room's ReadingBonus stat — a nice room improves listening for the same reasons it improves reading, so adding a new stat would be redundant.
        public static float GetListeningBonus(Thing album)
        {
            Room room = album.GetRoom();
            if (room != null && room.ProperRoom && !room.PsychologicallyOutdoors)
                return room.GetStat(RoomStatDefOf.ReadingBonus);
            return 1f;
        }

        // Gating on Hearing so deaf pawns won't seek albums autonomously, though a player can still force them to listen if they want.
        public static bool CanListenEver(Pawn listener)
        {
            if (listener.DevelopmentalStage == DevelopmentalStage.Baby)
                return false;
            if (!listener.health.capacities.CapableOf(PawnCapacityDefOf.Hearing))
                return false;
            return true;
        }

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

        // Can't use ThingRequestGroup.Book since that's tied to the vanilla Book ThingClass, so I search HaulableAlways and filter to MusicAlbum instances instead.
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

        // Skipping pawn.reading policy filters — albums aren't part of that system.
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
