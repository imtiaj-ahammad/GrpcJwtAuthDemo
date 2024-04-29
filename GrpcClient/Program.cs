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