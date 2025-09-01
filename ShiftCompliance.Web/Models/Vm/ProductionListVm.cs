namespace ShiftCompliance.Web.Models.Vm
{
    public class ProductionListQueryVm
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Shift { get; set; }             // Morning/Afternoon/Night
        public string? Supervisor { get; set; }        // free text search
        public string? Compliance { get; set; }        // "All" | "Yes" | "No"

        // paging
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    

    public class ProductionListPageVm
    {
        public ProductionListQueryVm Query { get; set; } = new();
        public List<ProductionListItemVm> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
    }
}
