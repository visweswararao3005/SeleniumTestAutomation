namespace LoginAutomation.Tests.Utils
{
    public class LoginTestCase
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Expected { get; set; } // "Success" | "Error" | "Validation"
        public bool RememberMe { get; set; } = false;
    }
}
