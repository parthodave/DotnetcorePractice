namespace DotNet8WebAPI.Model
{
    public class AuthenticateRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}
