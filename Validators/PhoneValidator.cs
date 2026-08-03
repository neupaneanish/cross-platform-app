using System;
using System.ComponentModel.DataAnnotations;
using PhoneNumbers;

namespace TuinFounder.Validators;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class PhoneValidator : ValidationAttribute
{
    private const string Message = "Enter a valid phone number";

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string number || string.IsNullOrWhiteSpace(number)) return ValidationResult.Success;

        var phoneUtils = PhoneNumberUtil.GetInstance();

        try
        {
            var phone = phoneUtils.Parse(number, null);
            var isValid = phoneUtils.IsValidNumber(phone) && phoneUtils.GetNumberType(phone) != PhoneNumberType.MOBILE;
            return isValid ? ValidationResult.Success : new ValidationResult(Message);
        }
        catch (NumberParseException)
        {
            return new ValidationResult(Message);
        }
    }
}