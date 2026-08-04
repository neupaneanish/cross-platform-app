using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grpc.Core;
using TuinFounder.Services;
using TuinFounder.Validators;

namespace TuinFounder.ViewModels;

public partial class RegisterViewModel(
    RegisterService service,
    Action<string, SessionType> onNavigateToVerification,
    Action onNavigateToLogin
) : ViewModelBase
{
    [ObservableProperty] public partial string? ErrorMessage { get; private set; } = null;

    [ObservableProperty] public partial bool IsLoading { get; private set; } = false;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email")]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Phone is required")]
    [PhoneValidator]
    public partial string Phone { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Password is required")]
    [PasswordValidator]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Confirm Password is required")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public partial string ConfirmPassword { get; set; } = string.Empty;

    partial void OnPasswordChanged(string value)
    {
        if (!string.IsNullOrEmpty(ConfirmPassword)) ValidateProperty(ConfirmPassword, nameof(ConfirmPassword));
    }

    [RelayCommand]
    private void NavigateToLogin()
    {
        onNavigateToLogin();
    }


    [RelayCommand]
    private async Task Submit()
    {
        if (IsLoading) return;

        ValidateAllProperties();

        if (HasErrors) return;

        ErrorMessage = null;
        IsLoading = true;

        try
        {
            var response = await service.RegisterAsync(Email, Password, ConfirmPassword, Phone);
            onNavigateToVerification(response.Session, SessionType.Account);
        }
        catch (RpcException e)
        {
            ErrorMessage = e.Status.Detail;
        }
        catch (Exception)
        {
            ErrorMessage = "Something went wrong, try again";
        }
        finally
        {
            IsLoading = false;
        }
    }
}