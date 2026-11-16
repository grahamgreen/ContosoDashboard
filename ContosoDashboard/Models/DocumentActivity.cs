using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

// Audit record; DocumentId has no FK so history survives document deletion.
public class DocumentActivity
{
    [Key]
    public int DocumentActivityId { get; set; }

    public int? DocumentId { get; set; }

    [Required]
    [MaxLength(255)]
    public string DocumentTitle { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string FileType { get; set; } = string.Empty;

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Action { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}

public static class DocumentActions
{
    public const string Upload = "Upload";
    public const string Download = "Download";
    public const string Preview = "Preview";
    public const string Delete = "Delete";
    public const string Share = "Share";
    public const string Update = "Update";
    public const string Replace = "Replace";
}
