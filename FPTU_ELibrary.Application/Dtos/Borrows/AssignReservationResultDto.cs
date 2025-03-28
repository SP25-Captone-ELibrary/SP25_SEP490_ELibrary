using System.Globalization;
using FPTU_ELibrary.Domain.Entities;

namespace FPTU_ELibrary.Application.Dtos.Borrows;

public class AssignReservationResultDto
{
    public string ReservationCode { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string CardBarcode { get; set; } = null!;
    public DateTime AssignedDate { get; set; }
    public ReservationQueueDto ReservationQueue { get; set; } = null!;
}

public static class AssignReservationResultDtoExtension
{
    public static List<AssignReservationResultDto> ToAssignReservationResultListDto(
        this List<ReservationQueueDto> reservations, DateTime assignedDate)
    {
        return reservations.Select(r => new AssignReservationResultDto()
        {
            AssignedDate = assignedDate,
            CardBarcode = r.LibraryCard.Barcode,
            FullName = r.LibraryCard.FullName,
            ReservationCode = r.ReservationCode ?? string.Empty,
            ReservationQueue = r
        }).ToList();
    }

    public static AssignReservationResultDto ToAssignReservationResultDto(this ReservationQueueDto dto)
    {
        return new()
        {
            AssignedDate = dto.AssignedDate ?? DateTime.MinValue,
            CardBarcode = dto.LibraryCard.Barcode,
            FullName = dto.LibraryCard.FullName,
            ReservationCode = dto.ReservationCode ?? string.Empty,
            ReservationQueue = dto
        };
    }
}