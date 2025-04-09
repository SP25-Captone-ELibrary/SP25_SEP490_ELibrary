using System.ComponentModel;

namespace FPTU_ELibrary.Domain.Common.Enums;

public enum LibraryItemCategory
{
    [Description("Sách đơn")]
    SingleBook,
    [Description("Sách bộ")]
    BookSeries,
    [Description("Sách tham khảo")]
    ReferenceBook,
    [Description("Báo chí")]
    Newspaper,
    [Description("Tạp chí")]
    Magazine,
    [Description("Sách thiếu nhi")]
    ChildrenBook,
    [Description("Sách khác")]
    Other
    
    #region Archived
    // [Description("Sách nghiệp vụ")]
    // ProfessionalBook,
    // [Description("Sách văn học")]
    // Literature,
    // [Description("Tài liệu đa phương tiện")]
    // Multimedia,
    // [Description("Báo cáo nghiên cứu")]
    // ResearchPaper,
    // [Description("Tài liệu hỗ trợ học tập")]
    // LearningSupportMaterial,
    // [Description("Luận văn, luận án")]
    // AcademicThesis
    // [Description("Sách chuyên ngành")]
    // SpecializedBook,
    #endregion
}