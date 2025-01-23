namespace FPTU_ELibrary.API.Payloads.Requests;

public class CheckItemWithImagesRequest : BaseCheckItem
{
    public List<IFormFile> Images { get; set; } = null!;
}

public class BaseCheckItem
{
    public string Title { get; set; } = null!; // marc21 code 245a
    public string? SubTitle { get; set; } // marc21 code 245b
    public string? GeneralNote { get; set; } // marc21 code 500
    public string Publisher { get; set; } = null!; // marc21 code 260
    public List<string> Authors { get; set; } = new(); // marc21 code[700a]/[700a + 700e]
}