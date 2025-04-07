namespace FPTU_ELibrary.Domain.Specifications.Params;

public class SupplementRequestDetailSpecParams : BaseSpecParams
{
    public int?[]? PageCountRange { get; set; }
    public int?[]? AverageRatingRange { get; set; }
    public int?[]? RatingsCountRange { get; set; }
}