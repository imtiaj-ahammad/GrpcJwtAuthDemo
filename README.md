


1. Let's create a blank solution with name **GrpcJwtAuthDemo**
2. Add gitignore with following command
    ```
    dotnet new gitignore
    ```
3. Add README.md file
4. Add a new console app against the solution named **GrpcClient**
5. Add new project with **Asp.Net Core Grpc Services** framework called **GrpcServer**
6. Add following packages into GrpcServer
    ```
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.5.1" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="6.0.29" />

    ```
7. Add following packages into GrpcClient
    ```
    <PackageReference Include="Google.Protobuf" Version="3.26.1" />
    <PackageReference Include="Grpc.Net.Client" Version="2.62.0" />
    <PackageReference Include="Grpc.Tools" Version="2.62.0">
    ```
8. Go to GrpcServer and copy the auto-generated proto file and paste it in the same directory and rename it as **authentication.proto** and update the cs inside
    ```
    syntax = "proto3";

    option csharp_namespace = "GrpcServer";

    package authentication;

    service Authentication {
    rpc Authenticate (AuthenticaitonRequest) returns (AuthenticaitonResponse);
    }

    message AuthenticaitonRequest{
        string UserName = 1;
        string Password = 2;
    }

    message AuthenticaitonResponse{
        stirng AccessToken = 1;
        int32 ExpiresIn = 2;
    }
    ```
9. Now let's go to Services folder and add a new class called **AuthenticationService** 
    ```
    namespace GrpcServer.Services
    {
        public class AuthenticationService : Authentication.AuthenticationBase
        {
            public override async Task<AuthenticaitonResponse> Authenticate(AuthenticaitonRequest request, ServerCallContext context)
            {
                var authenticationResponse = JwtAuthenticationManager.Authenticate(request);
                if(authenticationResponse == null)
                {
                    throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid User Credentials"));
                }
                return authenticationResponse;
            }
        }
    }
    ```
10. Add a new class into Grpc.Server named **JwtAuthenticationManager** 
    ```
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
    ```
11. Go to program.cs and add the injection for Authentication 
    ```
    using GrpcServer;
    using GrpcServer.Services;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.IdentityModel.Tokens;
    using System.Text;

    var builder = WebApplication.CreateBuilder(args);

    // Additional configuration is required to successfully run gRPC on macOS.
    // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

    // Add services to the container.
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(JwtAuthenticationManager.JWT_TOKEN_KEY)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });
    builder.Services.AddAuthorization();
    builder.Services.AddGrpc();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.UseAuthentication();
    app.UseAuthorization();
    //app.MapGrpcService<GreeterService>();
    //app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
    app.MapGrpcService<AuthenticationService>();
    app.MapGrpcService<CalculationService>();


    app.Run();

    ```
12. Go to GrpcServer and copy the auto-generated proto file and paste it in the same directory and rename it as **calculation.proto** and update the cs inside
    ```
    syntax = "proto3";

    option csharp_namespace = "GrpcServer";

    package calculation;

    service Calculation {
    rpc Add (InputNumbers) returns (CalculationResult);
    rpc Substract (InputNumbers) returns (CalculationResult);
    rpc Multiply (InputNumbers) returns (CalculationResult);
    }

    message InputNumbers{
        int32 Number1 = 1;
        int32 Number2 = 2;
    }

    message CalculationResult{
        int32 Result = 1;
    }
    ```
13. Add a new service **CalculationService** for calculation proto.
    ```
    namespace GrpcServer.Services
    {
        //[Authorize]
        public class CalculationService : Calculation.CalculationBase
        {
            [Authorize(Roles = "Administrator")]
            public override Task<CalculationResult> Add(InputNumbers request, ServerCallContext context)
            {
                return Task.FromResult(new CalculationResult { Result = request.Number1 + request.Number2 });
            }
            [Authorize(Roles = "Administrator,User")]
            public override Task<CalculationResult> Substract(InputNumbers request, ServerCallContext context)
            {
                return Task.FromResult(new CalculationResult { Result = request.Number1 - request.Number2 });
            }
            [AllowAnonymous]
            public override Task<CalculationResult> Multiply(InputNumbers request, ServerCallContext context)
            {
                return Task.FromResult(new CalculationResult { Result = request.Number1 * request.Number2 });
            }
        }
    }
    ```
14. Go to **GrpcClient** and make a new folder named **Protos** and copy the **authentication.proto** and **calculation.proto** from **GrpcServer**
15. Now edit the **GrpcClient.csproj** and remove the **ItemGroup** for removing the protos and update the protobuf type as **Client**
    ```
    <ItemGroup>
        <PackageReference Include="Google.Protobuf" Version="3.26.1" />
        <PackageReference Include="Grpc.Net.Client" Version="2.62.0" />
        <PackageReference Include="Grpc.Tools" Version="2.62.0">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>

        <!--ItemGroup>
        <None Update="Protos\authentication.proto">
        <GrpcServices>Client</GrpcServices>
        </None>
        <None Update="Protos\calculation.proto">
        <GrpcServices>Client</GrpcServices>
        </None>
    </ItemGroup-->
        <ItemGroup>
            <Protobuf Include="Protos\authentication.proto" GrpcServices="Client" />
            <Protobuf Include="Protos\calculation.proto" GrpcServices="Client" />
        </ItemGroup>
    </Project>

    ```
16. Now build the **Grpc.Client** that will generate the proto automated codes
17. Now go to **Grpc.Client.Program.cs** and update accordingly to call the services
    ```
    using Grpc.Core;
    using Grpc.Net.Client;
    Console.WriteLine("Hello, World!");

    var channel = GrpcChannel.ForAddress("http://localhost:5285");
    try
    {
        var authenticationClient = new GrpcClient.Authentication.AuthenticationClient(channel);
        var authenticationResponse = authenticationClient.Authenticate(new GrpcClient.AuthenticaitonRequest
        {
            UserName = "admin",
            Password = "admin"
        });
        Console.WriteLine($"Received Auth Response | Token: {authenticationResponse.AccessToken} | Expires In: {authenticationResponse.ExpiresIn}");


        var calculationClient = new GrpcClient.Calculation.CalculationClient(channel);
        var headers = new Metadata();
        headers.Add("Authorization", $"Bearer {authenticationResponse.AccessToken}");

        var sumResult = calculationClient.Add(new GrpcClient.InputNumbers { Number1 = 5, Number2 = 10 }, headers);
        Console.WriteLine($"Sum Result: 5+10={sumResult.Result}");

        var subtractResult = calculationClient.Substract(new GrpcClient.InputNumbers { Number1 = 20, Number2 = 5 }, headers);
        Console.WriteLine($"Subtract result: 20-5={subtractResult.Result}");

        var multiplyResult = calculationClient.Multiply(new GrpcClient.InputNumbers { Number1 = 5, Number2 = 6 });
        Console.WriteLine($"Multiply Result: 5*6={multiplyResult.Result}");

    }
    catch (RpcException ex)
    {
        Console.WriteLine($"Status Code: {ex.StatusCode} | Error: {ex.Message}");
        return;
    }

    await channel.ShutdownAsync();
    ```
18. Now let's run both the project and check if everything works

#### References
- https://www.youtube.com/watch?v=4-GTX6vW2Z4&list=PLzewa6pjbr3IOa6POjAMM0xiPZ-shjoem&index=4