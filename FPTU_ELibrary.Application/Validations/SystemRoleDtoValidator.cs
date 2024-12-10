using FluentValidation;
using FPTU_ELibrary.Application.Dtos.Roles;

namespace FPTU_ELibrary.Application.Validations;

public class SystemRoleDtoValidator : AbstractValidator<SystemRoleDto>
{
    public SystemRoleDtoValidator()
    {
    }   
}