using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MusicAlbums
{
    public class CompMusicAlbum : CompReadable
    {
        public new CompProperties_MusicAlbum Props =>
            (CompProperties_MusicAlbum)props;

        public new IEnumerable<AlbumOutcomeDoer> Doers =>
            base.Doers.OfType<AlbumOutcomeDoer>();

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (AlbumOutcomeDoer doer in Doers)
                foreach (Gizmo gizmo in doer.GetGizmos())
                    yield return gizmo;
        }
    }
}
