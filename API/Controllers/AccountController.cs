using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        public ITokenService _tokenService { get; set; }
        public AccountController(DataContext context, ITokenService tokenService)
        {
            _tokenService = tokenService;
            _context = context;
        }

        [HttpPost("register")] // POST: api/account/register
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (await UserExists(registerDto.UserName)) return BadRequest("Username is taken");

            using var hmac = new HMACSHA512();

            var user = new AppUser
            {
                UserName = registerDto.UserName.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.password)),
                PasswordSalt = hmac.Key
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new UserDto 
            {
                UserName = user.UserName,
                Token = _tokenService.CreateToken(user)
            };
        }

        [HttpPost("Login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _context.Users
                                     .SingleOrDefaultAsync(u => u.UserName == loginDto.UserName);

            if (user == null) return Unauthorized("Invalid Username");

            using var hmac = new HMACSHA512(user.PasswordSalt);

            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password");
            }

            return new UserDto
            {
                UserName = user.UserName,
                Token = _tokenService.CreateToken(user)
            };
        }

        private async Task<bool> UserExists(string username)
        {
            return await _context.Users.AnyAsync(u => u.UserName == username.ToLower());
        }
    }
}