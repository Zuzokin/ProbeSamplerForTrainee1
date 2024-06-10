using System.Globalization;
using System.Windows.Controls;

namespace ProbeSampler.WPF
{
    public class IntegerValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is string stringValue)
            {
                if (int.TryParse(stringValue, out _))
                {
                    return ValidationResult.ValidResult;
                }
            }

            return new ValidationResult(false, "Введите целое число");
        }
    }
}
