using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class Category
{
    // Key
    public int CategoryId { get; set; }

    // Category detail
    public string EnglishName { get; set; } = null!;
    public string VietnameseName { get; set; } = null!;
    public string? Description { get; set; }

    // Mapping entity
    [JsonIgnore]
    public ICollection<BookCategory> BookCategories { get; set; } = new List<BookCategory>();
}
