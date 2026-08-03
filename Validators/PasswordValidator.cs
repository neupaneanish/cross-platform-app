using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace TuinFounder.Validators;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public partial class PasswordValidator : ValidationAttribute
{
    private const int MinLength = 8;
    private const int MaxLength = 64;

    [GeneratedRegex(@"\d")]
    private static partial Regex Digit();

    [GeneratedRegex("[A-Z]")]
    private static partial Regex UpperCase();

    [GeneratedRegex("[a-z]")]
    private static partial Regex LowerCase();

    [GeneratedRegex(@"[!@#$%^&*(),.?""{}|<>\-_]")]
    private static partial Regex Special();

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string password || string.IsNullOrWhiteSpace(password)) return ValidationResult.Success;

        var errors = GetErrors(password);
        if (errors.Length <= 0) return ValidationResult.Success;
        var memberNames = validationContext.MemberName is { } name ? new[] { name } : null;
        return new ValidationResult(ErrorMessage ?? string.Join("\n", errors), memberNames);
    }

    public static string[] GetErrors(string? password)
    {
        if (string.IsNullOrWhiteSpace(password)) return [];

        var errors = new List<string>();

        if (password.Length < MinLength) errors.Add($"Must be at least {MinLength} characters");

        if (password.Length > MaxLength) errors.Add($"Cannot exceed {MaxLength} characters");

        if (!Digit().IsMatch(password)) errors.Add("At least one digit");

        if (!UpperCase().IsMatch(password)) errors.Add("At least one uppercase");

        if (!LowerCase().IsMatch(password)) errors.Add("At least one lowercase");

        if (!Special().IsMatch(password)) errors.Add("At least one special");

        return [.. errors];
    }
}