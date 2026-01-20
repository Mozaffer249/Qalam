using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Helpers;
using Qalam.Data.Results;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Qalam.Service.Implementations
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly UserManager<User> _userManager;
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public AuthenticationService(
            JwtSettings jwtSettings,
            UserManager<User> userManager,
            IRefreshTokenRepository refreshTokenRepository)
        {
            _jwtSettings = jwtSettings;
            _userManager = userManager;
            _refreshTokenRepository = refreshTokenRepository;
        }

        public async Task<JwtAuthResult> GetJWTToken(User user)
        {
            var (jwtToken, accessToken) = await GenerateJWTToken(user);
            var refreshToken = GetRefreshToken(user.UserName!);

            var userRefreshToken = new UserRefreshToken
            {
                AddedTime = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(_jwtSettings.RefreshTokenExpireDate),
                IsUsed = false,
                IsRevoked = false,
                JwtId = jwtToken.Id,
                RefreshToken = refreshToken.TokenString,
                Token = accessToken,
                UserId = user.Id
            };

            await _refreshTokenRepository.AddAsync(userRefreshToken);

            return new JwtAuthResult
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        public async Task<JwtSecurityToken?> ReadJWTToken(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
                return null;

            var handler = new JwtSecurityTokenHandler();

            try
            {
                var response = handler.ReadJwtToken(accessToken);
                return response;
            }
            catch
            {
                return null;
            }
        }

        public async Task<JwtAuthResult?> GetRefreshToken(User user, JwtSecurityToken jwtToken, string refreshToken)
        {
            var userRefreshToken = await _refreshTokenRepository.GetTableNoTracking()
                .FirstOrDefaultAsync(x => x.Token == jwtToken.RawData &&
                                         x.RefreshToken == refreshToken &&
                                         x.UserId == user.Id);

            if (userRefreshToken == null)
                return null;

            // Check if token is not used and not revoked
            if (userRefreshToken.IsUsed || userRefreshToken.IsRevoked)
                return null;

            // Check expiry date
            if (userRefreshToken.ExpiryDate < DateTime.Now)
                return null;

            // Mark old token as used
            userRefreshToken.IsUsed = true;
            await _refreshTokenRepository.UpdateAsync(userRefreshToken);

            // Generate new token
            return await GetJWTToken(user);
        }

        public async Task<string> ValidateToken(string accessToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret))
            };

            try
            {
                handler.ValidateToken(accessToken, parameters, out SecurityToken validatedToken);
                return "NotExpired";
            }
            catch (SecurityTokenExpiredException)
            {
                return "Expired";
            }
            catch
            {
                return "Invalid";
            }
        }

        public async Task<string> ConfirmEmail(int userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return "UserNotFound";

            var result = await _userManager.ConfirmEmailAsync(user, code);

            return result.Succeeded ? "Success" : "Failed";
        }

        public async Task<string> SendResetPasswordCode(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return "UserNotFound";

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            return code;
        }

        public async Task<string> ResetPassword(string email, string code, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return "UserNotFound";

            var result = await _userManager.ResetPasswordAsync(user, code, newPassword);

            return result.Succeeded ? "Success" : "Failed";
        }

        public async Task<bool> RevokeTokenAsync(string accessToken, string? refreshToken, int userId, bool allDevices)
        {
            try
            {
                if (allDevices)
                {
                    // Revoke all tokens for the user
                    var userTokens = await _refreshTokenRepository.GetTableNoTracking()
                        .Where(x => x.UserId == userId && !x.IsRevoked)
                        .ToListAsync();

                    foreach (var token in userTokens)
                    {
                        token.IsRevoked = true;
                        await _refreshTokenRepository.UpdateAsync(token);
                    }
                }
                else
                {
                    // Revoke specific token
                    var userRefreshToken = await _refreshTokenRepository.GetTableNoTracking()
                        .FirstOrDefaultAsync(x => x.Token == accessToken && x.UserId == userId);

                    if (userRefreshToken != null)
                    {
                        userRefreshToken.IsRevoked = true;
                        await _refreshTokenRepository.UpdateAsync(userRefreshToken);
                    }

                    // If refresh token provided, revoke it too
                    if (!string.IsNullOrEmpty(refreshToken))
                    {
                        var refreshTokenEntity = await _refreshTokenRepository.GetTableNoTracking()
                            .FirstOrDefaultAsync(x => x.RefreshToken == refreshToken && x.UserId == userId);

                        if (refreshTokenEntity != null)
                        {
                            refreshTokenEntity.IsRevoked = true;
                            await _refreshTokenRepository.UpdateAsync(refreshTokenEntity);
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        #region Private Methods

        private async Task<(JwtSecurityToken, string)> GenerateJWTToken(User user)
        {
            var claims = await GetClaims(user);
            var jwtToken = new JwtSecurityToken(
                _jwtSettings.Issuer,
                _jwtSettings.Audience,
                claims,
                expires: DateTime.Now.AddDays(_jwtSettings.AccessTokenExpireDate),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
                    SecurityAlgorithms.HmacSha256));

            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            return (jwtToken, accessToken);
        }

        private async Task<List<Claim>> GetClaims(User user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                // Use short claim names for cleaner JWT payload
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),  // "sub" - subject
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),  // "jti" - JWT ID
                new Claim("uid", user.Id.ToString()),
                new Claim("name", user.UserName ?? user.PhoneNumber ?? user.Id.ToString())
            };

            // Add optional claims only if they have values
            if (!string.IsNullOrEmpty(user.Email))
            {
                claims.Add(new Claim("email", user.Email));
            }
            
            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                claims.Add(new Claim("phone", user.PhoneNumber));
            }
            
            if (!string.IsNullOrEmpty(user.FirstName))
            {
                claims.Add(new Claim("firstName", user.FirstName));
            }
            
            if (!string.IsNullOrEmpty(user.LastName))
            {
                claims.Add(new Claim("lastName", user.LastName));
            }

            foreach (var role in roles)
            {
                claims.Add(new Claim("role", role));
            }

            var userClaims = await _userManager.GetClaimsAsync(user);
            claims.AddRange(userClaims);

            return claims;
        }

        private RefreshToken GetRefreshToken(string username)
        {
            var refreshToken = new RefreshToken
            {
                UserName = username,
                TokenString = GenerateRefreshToken(),
                ExpireAt = DateTime.Now.AddDays(_jwtSettings.RefreshTokenExpireDate)
            };

            return refreshToken;
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var generator = RandomNumberGenerator.Create();
            generator.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        #endregion
    }
}
