using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace GrpcServer
{
    public static class JwtAuthenticationManager
    {
        public const string JWT_TOKEN_KEY = "CodingDroplets@2022CodingDroplets@2022CodingDroplets@2022";
        public const int JWT_TOKEN_VALIDITY = 30;

        public static AuthenticaitonResponse Authenticate(AuthenticaitonRequest authenticaitonRequest)
        {
            // -- Implement User Credentials Validaiton
            var userRole = string.Empty;
            if (authenticaitonRequest.UserName == "admin" && authenticaitonRequest.Password == "admin")
            {
                userRole = "Administrator";
            }
            else if (authenticaitonRequest.UserName == "user" && authenticaitonRequest.Password == "user")
            {
                userRole = "User";
            }
            else
            {
                return null;
            }
            // -- 
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var tokenKey = Encoding.ASCII.GetBytes(JWT_TOKEN_KEY);
            var tokenExpireDateTime = DateTime.Now.AddMinutes(JWT_TOKEN_VALIDITY);
            var securityTokenDiscriptor = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new List<Claim>
                {
                    new Claim("username", authenticaitonRequest.UserName),
                    new Claim(ClaimTypes.Role, userRole)
                }),
                Expires = tokenExpireDateTime,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256Signature/*SecurityAlgorithms.HmacSha256Signature*/)
            };

            var securityToken = jwtSecurityTokenHandler.CreateToken(securityTokenDiscriptor);
            var token = jwtSecurityTokenHandler.WriteToken(securityToken);

            return new AuthenticaitonResponse
            {
                AccessToken = token,
                ExpiresIn = (int)tokenExpireDateTime.Subtract(DateTime.Now).TotalSeconds
            };


        }
    }
}
