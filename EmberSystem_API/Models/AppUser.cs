using Microsoft.AspNetCore.Identity;

namespace ApplicationSecurity_Backend.Models
{
    public class AppUser: IdentityUser
    {
        public string AccountRole { get; set; }
        public string? EmailVerificationToken { get; set; }
    }
}
