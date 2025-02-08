using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class LibraryCardController : ControllerBase
{
    private readonly ILibraryCardService<LibraryCardDto> _cardSvc;

    public LibraryCardController(ILibraryCardService<LibraryCardDto> cardSvc)
    {
        _cardSvc = cardSvc;
    }

    #region Management
    #endregion
}