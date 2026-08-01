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

public abstract record LoginTwoFactorReq
{
    public sealed record Totp(string Code) : LoginTwoFactorReq;

    public sealed record Recovery(string Code) : LoginTwoFactorReq;
}

public class LoginService(
    ExternalAuthenticationService.ExternalAuthenticationServiceClient client,
    ITokenService tokenService)
{
    private static readonly RpcException InvalidResponseException =
        new(new Status(StatusCode.Internal, "Invalid response"));

    private static readonly RpcException InvalidRequestException =
        new(new Status(StatusCode.InvalidArgument, "Invalid method"));

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
                return SaveToken(response.Token);
            case LoginResponse.ResponseOneofCase.Totp:
                return new LoginResult.Totp(response.Totp);
            case LoginResponse.ResponseOneofCase.Verification:
                return new LoginResult.Verification(response.Verification);
            case LoginResponse.ResponseOneofCase.None:
            default:
                throw InvalidResponseException;
        }
    }

    public async Task<LoginResult.Success> LoginTwoFactorAsync(string session, LoginTwoFactorReq req,
        CancellationToken cancellationToken = default)
    {
        var request = new LoginTwoFactorRequest { Session = session };

        switch (req)
        {
            case LoginTwoFactorReq.Totp totp:
                request.Totp = totp.Code;
                break;
            case LoginTwoFactorReq.Recovery recovery:
                request.Recovery = recovery.Code;
                break;
            default:
                throw InvalidRequestException;
        }

        var response = await client.LoginTwoFactorAsync(request, cancellationToken: cancellationToken);
        return SaveToken(response.Token);
    }

    private LoginResult.Success SaveToken(Token token)
    {
        tokenService.Save(token);
        return new LoginResult.Success();
    }
}