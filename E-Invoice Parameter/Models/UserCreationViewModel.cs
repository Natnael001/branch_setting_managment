using System.ComponentModel.DataAnnotations;

public class UserCreationViewModel
{
    // Employee Information
    [Required(ErrorMessage = "First name is required")]
    [Display(Name = "First Name")]
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public string FirstName { get; set; }

    [Display(Name = "Second Name")]
    [StringLength(100, ErrorMessage = "Second name cannot exceed 100 characters")]
    public string? SecondName { get; set; }

    [Required(ErrorMessage = "Please select a role")]
    [Display(Name = "Position/Role")]
    public int RoleId { get; set; }

    // User Account Information
    [Required(ErrorMessage = "Username is required")]
    [Display(Name = "Username")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
    [MaxLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
    public string Username { get; set; }

    // Password fields are optional for editing – no [Required] attributes
    [Display(Name = "Password")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Display(Name = "Confirm Password")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Remark")]
    [StringLength(500, ErrorMessage = "Remark cannot exceed 500 characters")]
    public string? Remark { get; set; }
}