using FPTU_ELibrary.Application.Dtos.BookEditions;

namespace FPTU_ELibrary.Application.Dtos.AIServices.Speech;

public class RecognisedItemDto<T>
{
    public T MatchedItem { get; set; }
    public List<RelatedItemDto<T>> RelatedItemsDetails { get; set; }
}

public class RelatedItemDto<T>
{
    public string RelatedProperty { get; set; }
    public List<T> RelatedItems { get; set; } 
}