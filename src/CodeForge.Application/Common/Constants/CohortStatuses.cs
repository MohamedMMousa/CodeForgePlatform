namespace CodeForge.Application.Common.Constants
{
    /// <summary>
    /// Admin-controlled lifecycle only. "Full" and "cutoff passed" are computed at
    /// read/write time from capacity and dates, never stored — see docs/DATABASE.md §4.
    /// </summary>
    public static class CohortStatuses
    {
        public const string Draft = "draft";
        public const string Open = "open";
        public const string Cancelled = "cancelled";
        public const string Completed = "completed";
    }
}
