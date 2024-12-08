namespace FPTU_ELibrary.Application.Dtos.Roles;

public class SystemMessageDto
{
    public string MsgId { get; set; } = null!;
    public string MsgContent { get; set; } = null!;
    public string? Vi { get; set; } // Vietnamese
    public string? En { get; set; } // English
    public string? Ru { get; set; } // Russian
    public string? Ja { get; set; } // Japanese
    public string? Ko { get; set; } // Korean
    public DateTime CreateDate { get; set; }
    public string CreateBy { get; set; } = null!;
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; } 
}