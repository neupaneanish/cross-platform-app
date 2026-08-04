using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grpc.Core;
using TuinFounder.Services;
using TuinFounder.Validators;

namespace TuinFounder.ViewModels;

public partial class LoginViewModel(
    LoginService service,
    Action onNavigateToRegister,
    Action onNavigateToForgetPassword,
    Action<string, SessionType> onNavigateToVerification
) : ViewModelBase
{
    private readonly string _errMessage = "Something went wrong, try again";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email")]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Password is required")]
    [PasswordValidator(ErrorMessage = "Enter a valid password")]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty] public partial string? ErrorMessage { get; private set; } = null;

    [ObservableProperty] public partial bool IsLoading { get; private set; } = false;

    [RelayCommand]
    private void NavigateToRegister()
    {
        onNavigateToRegister();
    }

    [RelayCommand]
    private void NavigateToForgetPassword()
    {
        onNavigateToForgetPassword();
    }


    [RelayCommand]
    private async Task Submit()
    {
        ValidateAllProperties();

        if (HasErrors) return;

        ErrorMessage = null;
        IsLoading = true;

        try
        {
            var response = await service.LoginAsync(Email, Password);
            switch (response)
            {
                case LoginResult.Success:
                    break;
                case LoginResult.Totp totp:
                    onNavigateToVerification(totp.Session, SessionType.Totp);
                    break;
                case LoginResult.Verification verification:
                    onNavigateToVerification(verification.Session, SessionType.Verification);
                    break;
                default:
                    ErrorMessage = _errMessage;
                    break;
            }
        }
        catch (RpcException e)
        {
            ErrorMessage = e.Status.Detail;
        }
        catch (Exception)
        {
            ErrorMessage = _errMessage;
        }
        finally
        {
            IsLoading = false;
            Password = string.Empty;
            ClearErrors(Password);
        }
    }
}