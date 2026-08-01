using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using TuinFounder.External.Authentication.V1;

namespace TuinFounder.Services;

public class VerificationService(
    ExternalAuthenticationService.ExternalAuthenticationServiceClient client,
    ITokenService tokenService
)
{
    private static readonly RpcException InvalidRequestException =
        new(new Status(StatusCode.InvalidArgument, "Invalid method"));

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
        tokenService.Save(response.Token);
        return new LoginResult.Success();
    }
}