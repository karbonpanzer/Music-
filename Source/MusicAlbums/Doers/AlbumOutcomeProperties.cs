using System;
using RimWorld;

namespace MusicAlbums.Doers
{
    public abstract class AlbumOutcomeProperties : ReadingOutcomeProperties
    {
        public override Type DoerClass => doerClass;

        protected AlbumOutcomeProperties() { }
    }
}
