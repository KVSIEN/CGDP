namespace CGD.Map
{
    // Min / average / max of one number over the maps of a MapStyleReport.
    public class MapReportStat
    {
        public int   Min   { get; private set; } = int.MaxValue;
        public int   Max   { get; private set; } = int.MinValue;
        public int   Count { get; private set; }
        public float Average => Count > 0 ? (float)_sum / Count : 0f;

        private long _sum;

        public void Add(int value)
        {
            if (value < Min) Min = value;
            if (value > Max) Max = value;
            _sum += value;
            Count++;
        }

        public override string ToString() => Count == 0 ? "—" : $"{Average:0.0}  ({Min}–{Max})";
    }
}
