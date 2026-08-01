using System.Threading;
using System.Threading.Tasks;
using TuinFounder.External.Authentication.V1;

namespace TuinFounder.Services;

public class ForgetPasswordService(ExternalAuthenticationService.ExternalAuthenticationServiceClient client)
{
    public async Task<ForgetPasswordResponse> ForgetPasswordAsync(
        string email,
        CancellationToken cancellationToken = default
    )
    {
        var request = new ForgetPasswordRequest { Email = email };

        return await client.ForgetPasswordAsync(request, cancellationToken: cancellationToken);
    }
}