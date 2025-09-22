using System.Collections.Generic;
using adwportal.Models;

namespace adwportal.States
{
    public class IdwListState
    {
        public string? BatchNo { get; set; }
        public string? OrderNo { get; set; }
        public string? Sku { get; set; }
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 20;
        public PagedResult<IdwOrderDto> Data { get; set; } = new() { Page = 1, Size = 20 };
        public List<IdwOrderDto> Source { get; set; } = new();
        public bool Dirty { get; set; }
        public long? RefreshRowId { get; set; }
        public bool HasData => Data?.Items != null && Data.Items.Count > 0;
    }
}
