using System.ComponentModel.DataAnnotations;

namespace LabManagementAPI.Models;

public sealed class SsoLoginRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}
