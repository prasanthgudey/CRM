using System;
using System.ComponentModel.DataAnnotations;

namespace CRM.Client.DTOs.Shared
{
    public class DateNotInPastAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            if (value is DateTime dt)
            {
                if (dt.Date < DateTime.Today)
                    return new ValidationResult("Past dates are not allowed.");
            }

            return ValidationResult.Success;
        }
    }
}
