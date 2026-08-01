using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using TuinFounder.Common.Password.V1;
using TuinFounder.External.Authentication.V1;

namespace TuinFounder.Services;

public abstract record LoginResult
{
    public sealed record Success : LoginResult;

    public sealed record Totp(string Session) : LoginResult;

    public sealed record Verification(string Session) : LoginResult;
}

public class LoginService(
    ExternalAuthenticationService.ExternalAuthenticationServiceClient client,
    ITokenService tokenService)
{
    private static readonly RpcException InvalidResponseException =
        new(new Status(StatusCode.Internal, "Invalid response"));

    public async Task<LoginResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default
    )
    {
        var request = new LoginRequest
        {
            Email = email,
            Password = new Password { Value = password }
        };

        var response = await client.LoginAsync(request, cancellationToken: cancellationToken);

        switch (response.ResponseCase)
        {
            case LoginResponse.ResponseOneofCase.Token:
                tokenService.Save(response.Token);
                return new LoginResult.Success();
            case LoginResponse.ResponseOneofCase.Totp:
                return new LoginResult.Totp(response.Totp);
            case LoginResponse.ResponseOneofCase.Verification:
                return new LoginResult.Verification(response.Verification);
            case LoginResponse.ResponseOneofCase.None:
            default:
                throw InvalidResponseException;
        }
    }
}