using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.EntityFrameworkCore;

namespace FPTU_ELibrary.Domain.Specifications;

public class RoleSpecification : BaseSpecification<RolePermission>
{
    public RoleSpecification(RoleSpecParams roleSpecParams)
    {
    }
}