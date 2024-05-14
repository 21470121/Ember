using ApplicationSecurity_Backend.Models;
using ApplicationSecurity_Backend.ViewModels;
using Google.Apis.Auth;
using Humanizer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace ApplicationSecurity_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IRepository _repository;
        private readonly IUserClaimsPrincipalFactory<AppUser> _claimsPrincipalFactory;
        private readonly IConfiguration _configuration;
        private AppDbContext _context;
        private readonly IEmailSender _emailSender;
      

        public AccountController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, IUserClaimsPrincipalFactory<AppUser> claimsPrincipalFactory, IConfiguration configuration, IRepository repository, AppDbContext appDbContext, IEmailSender emailSender)
        {
            _userManager = userManager;
            _roleManager= roleManager;
            _claimsPrincipalFactory = claimsPrincipalFactory;
            _configuration = configuration;
            _repository = repository;
            _context = appDbContext;
            _emailSender = emailSender;
           
        }

        //[HttpGet]
        //[Route("GetAllCourses")]
        //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        ////[Authorize]
        //public async Task<IActionResult> GetAllCoursesAsync()
        //{

        //    try
        //    {
        //        var results = await _repository.GetAllCoursesAsync();
        //        return Ok(results);
        //    }
        //    catch (Exception)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError, "Internal Server Error, please contact support");
        //    }
        //}

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register(UserViewModel uvm)
        {
            var user = await _userManager.FindByIdAsync(uvm.UserName);

            if (user == null)
            {
                user = new AppUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = uvm.UserName,
                    Email = uvm.UserName,
                    AccountRole = "Customer"
                };

                var result = await _userManager.CreateAsync(user, uvm.password);

                if (result.Succeeded)
                {
                    var cust = new Customer
                    {
                        AppUser = user,
                        CustomerName = uvm.UserName
                    };
                    _context.Set<Customer>().Add(cust);
                    await _context.SaveChangesAsync();
                }

                if (result.Errors.Count() > 0) return StatusCode(StatusCodes.Status500InternalServerError, "Internal Server Error. Please contact support."); //error message
            }
            else
            {
                return Forbid("Account already exists.");
            }

            return Ok();
        }

        [HttpPost]
        [Route("Login")]
        public async Task<ActionResult> Login(UserViewModel uvm)
        {
            var user = await _userManager.FindByNameAsync(uvm.UserName);

            if (user != null && await _userManager.CheckPasswordAsync(user, uvm.password))
            {
       
                try
                {
                    if(user.TwoFactorEnabled)
                    {

                        var randomBytes = new byte[4];
                        using (var rng = RandomNumberGenerator.Create())
                        {
                            rng.GetBytes(randomBytes);
                        };

                        var verificationToken = Convert.ToBase64String(randomBytes);
                        user.EmailVerificationToken = verificationToken;
                        await  _userManager.UpdateAsync(user);

                       
                       // var emailBody= GenerateEmailBody(verificationToken);

                        await _emailSender.SendEmailAsync(user.Email, "RAFA Distributors: Two Factor Verification", $"Your two-factor verification code is: {verificationToken}");
                        return Ok(new { requiresTwoFactorAuth = true });
                    }
                    else
                    {
                        return GenerateJWTToken(user);
                    }

                }
                catch (Exception)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Internal Server Error. Please contact support.");
                }
            }
            else
            {
                return NotFound("Does not exist");
            }

        }

        private string GenerateEmailBody(string verificationToken)
        {
            return $@"
        <html>
        <head>
            <style>
                /* Styles */
            </style>
        </head>
        <body>
            <div class='container'>
                <h1>Two-Factor Authentication</h1>
                <p>Please use the following verification code to complete your login:</p>
                <p style='font-size: 20px;'>{verificationToken}</p>
                <p>If you didn't request this code, you can safely ignore this email.</p>
            </div>
        </body>
        </html>";
        }




        [HttpPost("registerAdmin")]
        public async Task<IActionResult> RegisterAdmin(RegisterAdminViewModel rvm)
        {
            var user = await _userManager.FindByIdAsync(rvm.emailaddress);

            if (user == null)
            {
                user = new AppUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = rvm.emailaddress,
                    Email = rvm.emailaddress,
                    AccountRole = "Admin"
                };


                var result = await _userManager.CreateAsync(user, rvm.password);

                //string AccountID = user.Id;
                //var admin = new Admin
                //{
                //    AdminName= rvm.Name,
                //    AdminSurname = rvm.Surname,
                    
                //}

                if (result.Errors.Count() > 0) return StatusCode(StatusCodes.Status500InternalServerError, "Internal Server Error. Please contact support.");
            }
            else
            {
                return Forbid("Account already exists.");
            }

            return Ok();


        }

        [HttpPost("EnableTwoFactorAuthentication")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> EnableTwoFactorAuthentication(string username)
        {

            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
            {
                Console.WriteLine($"User with UserName {user} not found");
                return NotFound(new { message = "User not found" });
            }

            user.TwoFactorEnabled = true;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Two-factor authentication enabled" });
        }

        [HttpPost("SendEmailVerification2FA")]
      //  [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> SendTwoFactorVerificationEmail(string username)
        {
            
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

           
            var verificationCode = 55556;

            
            await _emailSender.SendEmailAsync(user.Email, "Two-Factor Verification Code", $"Your two-factor verification code is: {verificationCode}");

           
            return Ok(new { message = "Two-factor verification email sent" });
        }

        [HttpPost("SendEmailVerification")]
        public async Task<IActionResult> SendEmailVerification(string username)
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }


            var randomBytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            };
            

            var verificationToken = Convert.ToBase64String(randomBytes); 
            user.EmailVerificationToken = verificationToken;
            await _userManager.UpdateAsync(user);


          

            // Send verification email using your existing email sender service
           // await _emailSender.SendEmailAsync(user.Email, "Verify Your Email Address",);
            Console.WriteLine($"Generated Verification Token: {verificationToken}");
            return Ok(new { message = "Email verification email sent" });
        }

        [HttpGet("Verify2FAToken")]
        public async Task<IActionResult> Verify2FAToken(string verifytoken, string username)
        {
            var user = await _userManager.FindByEmailAsync(username);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            } else if (user.EmailVerificationToken != verifytoken)
            {
                return BadRequest(new { message = "Invalid verification token" });
            } else
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
                return GenerateJWTToken(user);
            }
        }



        [HttpGet("Check2FA")]
        public async Task<IActionResult> Check2FA(string username)
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            bool isEnabled = user.TwoFactorEnabled;

            return Ok(new { isEnabled });
        }


        [HttpGet]
        private ActionResult GenerateJWTToken(AppUser user)
        {
            // Create JWT Token
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new Claim(ClaimTypes.Role, user.AccountRole)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Tokens:Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _configuration["Tokens:Issuer"],
                _configuration["Tokens:Audience"],
                claims,
                signingCredentials: credentials,
                expires: DateTime.UtcNow.AddHours(3)
            );

            return Created("", new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                user = user.UserName
            });
        }

       


        [HttpPost]
        [Route("LoginExternal")]
        public async Task<ActionResult> LoginExternal([FromBody]string IDToken)
        {

            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new List<string>() { _configuration.GetSection("Authentication:Google:ClientId").Value }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(IDToken, settings);
            var user = await _userManager.FindByNameAsync(payload.Email);

            if (user != null)
            {

                try
                {
                    return GenerateJWTToken(user);

                }
                catch (Exception)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Internal Server Error. Please contact support.");
                }
            }
            else
            {
                return NotFound("Does not exist");
            }

        }

        [HttpPost]
        [Route("CreateRole")]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                role = new IdentityRole
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = roleName
                };

                var result = await _roleManager.CreateAsync(role);

                if (result.Errors.Count() > 0) return BadRequest(result.Errors);
            }
            else
            {
                return Forbid("Role already exists.");
            }

            return Ok();
        }
    }
}
