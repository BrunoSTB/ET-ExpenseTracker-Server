using System.ComponentModel.DataAnnotations;
using Npgsql;

namespace ExpenseTracker.API.Options
{
    public class DatabaseOptions : IValidatableObject
    {
        public const string ConnectionStringName = "DefaultConnection";
        public const string LegacyVariable = "SqlConnectionString";

        [Required(ErrorMessage = "A database connection string is required. Set ConnectionStrings__DefaultConnection (or the legacy SqlConnectionString).")]
        public string ConnectionString { get; set; } = string.Empty;

        public void Load(IConfiguration configuration)
        {
            ConnectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? configuration[LegacyVariable]
                ?? string.Empty;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                yield break;
            }

            NpgsqlConnectionStringBuilder? builder = null;
            try
            {
                builder = new NpgsqlConnectionStringBuilder(ConnectionString);
            }
            catch (ArgumentException)
            {
            }

            if (builder == null)
            {
                yield return new ValidationResult(
                    "The database connection string is not a valid Npgsql connection string.",
                    [nameof(ConnectionString)]);
            }
            else if (string.IsNullOrWhiteSpace(builder.Host))
            {
                yield return new ValidationResult(
                    "The database connection string must specify a Host.",
                    [nameof(ConnectionString)]);
            }
        }
    }
}
