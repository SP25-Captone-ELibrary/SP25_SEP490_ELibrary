namespace FPTU_ELibrary.Application.Elastic.Params
{
    public record SearchUserParameters(
        string? SearchText,
        string? Sort,
        bool? IsDecendingSort,
        int? PublicationYear,
        string? Languages,
        int? MaxPageCount,
        int? IsDeleted,
        int? IsDraft,
        int Skip,
        int Take
    );
}