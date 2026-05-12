using System;

namespace MusicAlbums.Doers
{
    public class AlbumOutcomeProperties_JoyFactor : AlbumOutcomeProperties
    {
        public AlbumOutcomeProperties_JoyFactor()
        {
            doerClass = typeof(AlbumOutcomeDoer_JoyFactor);
        }

        public override Type DoerClass => typeof(AlbumOutcomeDoer_JoyFactor);
    }
}
