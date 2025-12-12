using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CharonDataProcessor.Models;

[Table("Metrics")]
public class Metric
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(max)")]
    public string PayloadJson { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

