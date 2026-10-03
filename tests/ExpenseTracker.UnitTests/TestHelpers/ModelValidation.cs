using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ExpenseTracker.UnitTests.TestHelpers;

public static class ModelValidation
{
    public static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    public static bool IsValidParameter(MethodInfo method, string parameterName, object? value)
    {
        var parameter = method.GetParameters().Single(p => p.Name == parameterName);
        return parameter.GetCustomAttributes<ValidationAttribute>().All(attribute => attribute.IsValid(value));
    }
}
