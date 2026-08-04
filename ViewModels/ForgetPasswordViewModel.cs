using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grpc.Core;
using TuinFounder.External.Authentication.V1;
using TuinFounder.Services;

namespace TuinFounder.ViewModels;

public partial class ForgetPasswordViewModel(
    ForgetPasswordService service,
    Action onNavigateToLogin,
    Action<string, SessionType> onNavigateToVerification
) : ViewModelBase
{
    private readonly string _errMessage = "Something went wrong, try again";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email")]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty] public partial string? ErrorMessage { get; private set; } = null;

    [ObservableProperty] public partial bool IsLoading { get; private set; } = false;

    [RelayCommand]
    private void NavigateToLogin()
    {
        onNavigateToLogin();
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
            var response = await service.ForgetPasswordAsync(Email);
            switch (response.ResponseCase)
            {
                case ForgetPasswordResponse.ResponseOneofCase.Session:
                    onNavigateToVerification(response.Session, SessionType.Verification);
                    break;
                case ForgetPasswordResponse.ResponseOneofCase.Verification:
                    onNavigateToVerification(response.Verification, SessionType.Verification);
                    break;
                case ForgetPasswordResponse.ResponseOneofCase.None:
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
        }
    }
}