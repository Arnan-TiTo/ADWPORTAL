public sealed class PaginationState
{
    public int Page { get; private set; } = 1;
    public int PageSize { get; private set; } = 10;
    public int TotalCount { get; private set; }

    // เดิม: Math.Ceiling(TotalCount / (double)PageSize)
    public int TotalPages => Math.Max(1, (TotalCount + PageSize - 1) / PageSize);

    public int WindowSize { get; set; } = 5;

    public void SetTotal(int total) => TotalCount = Math.Max(0, total);
    public void SetPageSize(int size)
    {
        PageSize = Math.Max(1, size);
        GoTo(1);
    }
    public void GoTo(int p) => Page = Math.Clamp(p, 1, TotalPages);
    public (int Skip, int Take) Range() => ((Page - 1) * PageSize, PageSize);

    public int WindowStart
    {
        get
        {
            if (TotalPages <= 2) return 2;
            var maxStart = Math.Max(2, TotalPages - 1 - (WindowSize - 1));
            return Math.Max(2, Math.Min(Page - WindowSize / 2, maxStart));
        }
    }
    public int WindowEnd => (TotalPages <= 2) ? 1 : Math.Min(TotalPages - 1, WindowStart + WindowSize - 1);
}
