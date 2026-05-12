using System;

namespace MusicAlbums.Doers
{
    public class AlbumOutcomeProperties_Inspiration : AlbumOutcomeProperties
    {
        public AlbumOutcomeProperties_Inspiration()
        {
            doerClass = typeof(AlbumOutcomeDoer_Inspiration);
        }

        public override Type DoerClass => typeof(AlbumOutcomeDoer_Inspiration);
    }
}
