namespace ExpenseTracker.API.Dtos
{
    public class PreferencesDto
    {
        public string Currency { get; set; }
        public string Locale { get; set; }

        public PreferencesDto(string currency, string locale)
        {
            Currency = currency;
            Locale = locale;
        }
    }
}
