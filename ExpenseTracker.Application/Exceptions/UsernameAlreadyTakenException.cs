namespace ExpenseTracker.Application.Exceptions
{
    public class UsernameAlreadyTakenException : Exception
    {
        public UsernameAlreadyTakenException(string username, Exception? innerException = null)
            : base($"Username '{username}' is already taken.", innerException)
        {
        }
    }
}
