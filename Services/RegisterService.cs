using System.Threading;
using System.Threading.Tasks;
using TuinFounder.Common.Password.V1;
using TuinFounder.External.Authentication.V1;

namespace TuinFounder.Services;

public class RegisterService(ExternalAuthenticationService.ExternalAuthenticationServiceClient client)
{
    public async Task<RegisterResponse> RegisterAsync(
        string email,
        string password,
        string confirmPassword,
        string phone,
        CancellationToken cancellationToken = default
    )
    {
        var request = new RegisterRequest
        {
            Email = email,
            Password = new Password { Value = password },
            ConfirmPassword = new Password { Value = confirmPassword },
            Phone = phone
        };

        return await client.RegisterAsync(request, cancellationToken: cancellationToken);
    }
}