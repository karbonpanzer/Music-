using System;
using RimWorld;
using Verse;

namespace MusicAlbums
{
    public class AlbumOutcomeProperties_MoodBuff : AlbumOutcomeProperties
    {
        public ThoughtDef thought;

        // This is accumulated factor (ReadingSpeed * roomBonus * delta), not raw ticks. 2000 works out to roughly 33 seconds of listening at baseline stats.
        public float minListenFactor = 2000f;

        public AlbumOutcomeProperties_MoodBuff()
        {
            doerClass = typeof(AlbumOutcomeDoer_MoodBuff);
        }

        public override Type DoerClass => typeof(AlbumOutcomeDoer_MoodBuff);
    }
}
