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
