using System.Threading;
using System.Threading.Tasks;
using TuinFounder.Common.Password.V1;
using TuinFounder.External.Authentication.V1;

namespace TuinFounder.Services;

public class ResetPasswordService(ExternalAuthenticationService.ExternalAuthenticationServiceClient client)
{
    public async Task<ResetPasswordResponse> ResetPasswordAsync(
        string session,
        string password,
        string confirmPassword,
        CancellationToken cancellationToken = default
    )
    {
        var request = new ResetPasswordRequest
        {
            Session = session,
            Password = new Password { Value = password },
            ConfirmPassword = new Password { Value = confirmPassword }
        };

        return await client.ResetPasswordAsync(request, cancellationToken: cancellationToken);
    }
}