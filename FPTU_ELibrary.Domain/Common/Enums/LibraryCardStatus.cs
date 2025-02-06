namespace FPTU_ELibrary.Domain.Common.Enums;

public enum LibraryCardStatus
{
    Pending, // Register but not yet paid  
    Active, // Payment successfully, card is active
    Expired, // Card expired
    Suspended, // Disabled the card due to rule violations or deactivated by admin or librarian
}