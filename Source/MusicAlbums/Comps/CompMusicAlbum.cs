using System.Collections.Generic;
using MusicAlbums.Doers;
using RimWorld;
using Verse;

namespace MusicAlbums.Comps
{
    public class CompMusicAlbum : CompReadable
    {
        public new CompProperties_MusicAlbum Props =>
            (CompProperties_MusicAlbum)props;

        // I need this typed as AlbumOutcomeDoer rather than the base ReadingOutcomeDoer so callers don't have to cast everywhere. Filtering with is instead of OfType keeps Linq out.
        public new IEnumerable<AlbumOutcomeDoer> Doers
        {
            get
            {
                foreach (ReadingOutcomeDoer doer in base.Doers)
                    if (doer is AlbumOutcomeDoer albumDoer)
                        yield return albumDoer;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (AlbumOutcomeDoer doer in Doers)
                foreach (Gizmo gizmo in doer.GetGizmos())
                    yield return gizmo;
        }
    }
}
