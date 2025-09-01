namespace ShiftCompliance.Web.Models.Vm
{
    public class PagedProductionListVm
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
        public List<ProductionListItemVm> Items { get; set; } = new();
    }
}
