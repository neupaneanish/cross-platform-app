using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace TuinFounder.Validators;

public enum CodeType
{
    Totp = 6,
    Email = 8,
    Recovery = 10
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public partial class CodeValidator(CodeType codeType) : ValidationAttribute
{
    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DigitsOnly();

    [GeneratedRegex("^[A-Z0-9]+$")]
    private static partial Regex AlphanumericOnly();

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string code || string.IsNullOrWhiteSpace(code)) return ValidationResult.Success;

        var isValid = codeType switch
        {
            CodeType.Totp => code.Length == (int)codeType && DigitsOnly().IsMatch(code),
            CodeType.Email or CodeType.Recovery => code.Length == (int)codeType && AlphanumericOnly().IsMatch(code),
            _ => false
        };

        return isValid
            ? ValidationResult.Success
            : new ValidationResult("Enter a valid code.", [validationContext.MemberName!]);
    }
}