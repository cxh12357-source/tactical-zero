using System;

namespace TacticalZero
{
    [Serializable]
    public sealed class MatchStats
    {
        public int kills, shots, hits, headshots, supplies;
        public float damageTaken, seconds;
        public bool won;
        public float Accuracy => shots > 0 ? 100f * hits / shots : 0;
        public int Score => kills * 100 + supplies + (won ? 500 : 0);
    }
}
