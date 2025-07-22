using System.ComponentModel.DataAnnotations;

namespace Forma.Examples.Web.AspNetCore.Data.Entities;

/// <summary>
/// User entity for Entity Framework
/// </summary>
public class User
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
}