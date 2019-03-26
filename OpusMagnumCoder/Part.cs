using System.Collections.Generic;

namespace OpusMagnumCoder
{
    public class Part
    {
        public int Index;
        public string Name;
        public int Number;
        public Position Position;
        public int Rotation;
        public int Size;
        public List<Step> Steps;
        public List<Position> TrackPositions;
        public int TrackLoopLength = 1;
    }
}