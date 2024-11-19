using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FPTU_ELibrary.Domain.Entities;

public class BookCategory
{
    // Key
    public int CategoryId { get; set; }

    // Category detail
    public string EnglishName { get; set; } = null!;
    public string VietnameseName { get; set; } = null!;
    public string? Description { get; set; }

    // Mapping entity
    [JsonIgnore]
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
