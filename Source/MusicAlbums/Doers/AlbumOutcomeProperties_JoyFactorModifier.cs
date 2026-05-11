using System;

namespace MusicAlbums
{
    public class AlbumOutcomeProperties_JoyFactorModifier : AlbumOutcomeProperties
    {
        public AlbumOutcomeProperties_JoyFactorModifier()
        {
            doerClass = typeof(AlbumOutcomeDoer_JoyFactorModifier);
        }

        public override Type DoerClass => typeof(AlbumOutcomeDoer_JoyFactorModifier);
    }
}
