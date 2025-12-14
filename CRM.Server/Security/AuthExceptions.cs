namespace CRM.Server.Security
{
    /// <summary>
    /// Thrown when a refresh/access token is invalid or reused
    /// </summary>
    public class AuthInvalidTokenException : Exception
    {
        public AuthInvalidTokenException() { }
    }

    /// <summary>
    /// Thrown when a refresh token is expired
    /// </summary>
    public class AuthTokenExpiredException : Exception
    {
        public AuthTokenExpiredException() { }
    }

    /// <summary>
    /// Thrown when user password is expired
    /// </summary>
    public class AuthPasswordExpiredException : Exception
    {
        public AuthPasswordExpiredException() { }
    }

    /// <summary>
    /// Thrown when MFA verification is required
    /// </summary>
    public class AuthMfaRequiredException : Exception
    {
        public string Email { get; }

        public AuthMfaRequiredException(string email)
        {
            Email = email;
        }
    }
}
