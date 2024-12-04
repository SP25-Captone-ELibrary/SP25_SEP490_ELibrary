namespace FPTU_ELibrary.Application.Dtos.Cache;

public record GetCachedValueDto<T>(bool Cached, T Value);
