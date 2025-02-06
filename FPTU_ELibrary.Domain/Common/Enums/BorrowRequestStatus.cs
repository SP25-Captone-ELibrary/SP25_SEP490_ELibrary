namespace FPTU_ELibrary.Domain.Common.Enums;

public enum BorrowRequestStatus
{
    Created, // The request is created and waiting for the user to pick up the item
    Expired, // The user didn't pick up the item before ExpirationDate
    Borrowed, // The user picked up the item, and a BorrowRecord has been created
    Cancelled // The user cancels the request
}