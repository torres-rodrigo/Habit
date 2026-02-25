namespace Tracker.Models
{
    public class YearUntrackedCount
    {
        public int Year { get; set; }
        public int UntrackedCount { get; set; }
        public int TotalCount { get; set; }
        public double Percentage => TotalCount > 0 ? (UntrackedCount * 100.0 / TotalCount) : 0;
    }
}
