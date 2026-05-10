using System.ComponentModel.DataAnnotations;

namespace Core.Services.Crm.Contracts;

public record UpsertNoteDto
{
    public long? Id { get; set; }

    [Required, MaxLength(500)]
    public string Text { get; set; } = null!;
}
