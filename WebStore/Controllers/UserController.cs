using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using WebStore.Contexts;
using WebStore.Models;
using WebStore.Models.DTOs;
using WebStore.Services;

namespace WebStore.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly AppSettings _appSettings;

        public UserController(ApplicationDbContext context, IMapper mapper, IOptions<AppSettings> appSettings)
        {
            _context = context;
            _mapper = mapper;
            _appSettings = appSettings.Value;
        }

        /// <summary>
        /// Registers a new account.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult Register(UserRegisterDTO model)
        {
            var user = _context.Users.SingleOrDefault(x => x.Username == model.Username);
            if (user != null)
            {
                return Ok(new { message = "Username already exists" });
            }
            bool passwordsMatch = model.Password == model.ConfirmPassword ? true : false;

            if (!passwordsMatch)
            {
                return Ok(new { message = "Passwords do not match" });
            }

            User userObj = new User();
            userObj.Username = model.Username; // Get the username
            userObj.Salt = Convert.ToBase64String(RandomSalt.GetRandomSalt(16)); // Get random salt
            userObj.Password = Convert.ToBase64String(RandomSalt.SaltHashPassword(
                Encoding.ASCII.GetBytes(model.Password),
                Convert.FromBase64String(userObj.Salt)));

            userObj.CreatedBy = model.Username;
            userObj.ModifiedBy = model.Username;

            _context.Users.Add(userObj);
            _context.SaveChanges();
            userObj.Password = "";

            return Ok();
        }

        /// <summary>
        /// Authenticates a user account.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("authenticate")]
        public IActionResult Authenticate(UserDTO model)
        {

            if (_context.Users.Any(user => user.Username.Equals(model.Username)))
            {
                User user = _context.Users.Where(u => u.Username.Equals(model.Username)).First();
                // Calculate hash password from data of client and compare with hash in server with salt
                var client_post_hash_password = Convert.ToBase64String(RandomSalt.SaltHashPassword(
                    Encoding.ASCII.GetBytes(model.Password),
                    Convert.FromBase64String(user.Salt)));
                if (client_post_hash_password.Equals(user.Password))
                {
                    // If the user was found, generate a JWT Token
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
                    var tokenDescriptor = new SecurityTokenDescriptor()
                    {
                        Subject = new ClaimsIdentity(new Claim[]
                        {
                            new Claim(ClaimTypes.Name, user.Id.ToString())
                        }),
                        Expires = DateTime.UtcNow.AddHours(12),
                        SigningCredentials = new SigningCredentials
                            (new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
                    };
                    var token = tokenHandler.CreateToken(tokenDescriptor);
                    user.Token = tokenHandler.WriteToken(token);
                    user.Password = "";
                }
                return Ok(user);
            }
            else
            {
                return BadRequest(new { message = "Username or password is incorrect" });
            }
        }
    }
}