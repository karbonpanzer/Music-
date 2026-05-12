using RimWorld;
using Verse;

namespace MusicAlbums
{
    [DefOf]
    public static class MusicAlbumsDefOf
    {
        public static JobDef ListenToAlbum;

        static MusicAlbumsDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MusicAlbumsDefOf));
        }
    }
}
