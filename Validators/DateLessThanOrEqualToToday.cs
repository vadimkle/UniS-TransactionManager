using System.ComponentModel.DataAnnotations;

namespace TransactionManager.Validators;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class DateTimeLessThanOrEqualToNow : ValidationAttribute
{
    public override string FormatErrorMessage(string name)
    {
        return $"{name} cannot be in the future.";
    }

    protected override ValidationResult? IsValid(object? objValue, ValidationContext validationContext)
    {
        var dateValue = objValue as DateTime? ?? DateTime.MaxValue;

        // suppoze that unspecified date time kind is for UTC time
        var dateOffset = dateValue.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(dateValue, TimeSpan.Zero)
            : new DateTimeOffset(dateValue);

        return dateOffset <= DateTimeOffset.UtcNow
            ? ValidationResult.Success
            : new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
    }
}