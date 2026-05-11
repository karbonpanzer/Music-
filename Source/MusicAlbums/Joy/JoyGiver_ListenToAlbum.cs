using RimWorld;
using Verse;
using Verse.AI;

namespace MusicAlbums
{
    // Using def.jobDef rather than hardcoding so the XML stays in control of which job runs.
    public class JoyGiver_ListenToAlbum : JoyGiver
    {
        public override bool CanBeGivenTo(Pawn pawn)
        {
            if (!AlbumUtility.CanListenNow(pawn))
                return false;
            if (PawnUtility.WillSoonHaveBasicNeed(pawn))
                return false;
            return base.CanBeGivenTo(pawn);
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            if (AlbumUtility.TryGetRandomAlbumToListen(pawn, out MusicAlbum album))
                return JobMaker.MakeJob(def.jobDef, album);
            return null;
        }
    }
}
