namespace FPTU_ELibrary.Domain.Specifications.Params
{
    public class BaseSpecParams
    {
        public int? PageIndex { get; set; } = 1;
        public int? PageSize { get; set; }
        public string? Search { get; set; }
        public string? Sort { get; set; }
    }
}
