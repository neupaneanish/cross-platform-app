using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using TuinFounder.External.Authentication.V1;

namespace TuinFounder.Services;

public abstract record LoginTwoFactorReq
{
    public sealed record Totp(string Code) : LoginTwoFactorReq;

    public sealed record Recovery(string Code) : LoginTwoFactorReq;
}

public abstract record AccountVerificationResult
{
    public sealed record Success : AccountVerificationResult;

    public sealed record Totp(string Session) : AccountVerificationResult;

    public sealed record Reset(string Session) : AccountVerificationResult;
}

public class VerificationService(
    ExternalAuthenticationService.ExternalAuthenticationServiceClient client,
    ITokenService tokenService
)
{
    private static readonly RpcException InvalidRequestException =
        new(new Status(StatusCode.InvalidArgument, "Invalid method"));

    private static readonly RpcException InvalidResponseException =
        new(new Status(StatusCode.Internal, "Invalid response"));

    public async Task<AccountVerificationResult.Success> LoginTwoFactorAsync(
        string session,
        LoginTwoFactorReq req,
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

    public async Task<VerificationResponse> VerificationAsync(
        string session,
        string code,
        CancellationToken cancellationToken = default)
    {
        var request = new VerificationRequest
        {
            Session = session,
            Code = code
        };

        return await client.VerificationAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<AccountVerificationResult> AccountVerificationAsync(
        string session,
        string code,
        CancellationToken cancellationToken = default
    )
    {
        var request = new AccountVerificationRequest
        {
            Session = session,
            Code = code
        };
        var response = await client.AccountVerificationAsync(request, cancellationToken: cancellationToken);

        switch (response.ResponseCase)
        {
            case AccountVerificationResponse.ResponseOneofCase.Token:
                return SaveToken(response.Token);
            case AccountVerificationResponse.ResponseOneofCase.TotpSession:
                return new AccountVerificationResult.Totp(response.TotpSession);
            case AccountVerificationResponse.ResponseOneofCase.ResetSession:
                return new AccountVerificationResult.Reset(response.ResetSession);
            case AccountVerificationResponse.ResponseOneofCase.None:
            default: throw InvalidResponseException;
        }
    }

    public async Task<ResendResponse> ResendAsync(
        string session,
        CancellationToken cancellationToken = default
    )
    {
        var request = new ResendRequest
        {
            Session = session
        };

        return await client.ResendAsync(request, cancellationToken: cancellationToken);
    }

    private AccountVerificationResult.Success SaveToken(Token token)
    {
        tokenService.Save(token);
        return new AccountVerificationResult.Success();
    }
}