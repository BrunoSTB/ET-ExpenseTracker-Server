using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.API.Configuration
{
    public class ConnectionStringsOptions
    {
        public const string SectionName = "ConnectionStrings";

        [Required(ErrorMessage = "ConnectionStrings:Default is required (environment variable ConnectionStrings__Default).")]
        public string Default { get; set; } = string.Empty;
    }
}
