using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grpc.Core;
using TuinFounder.Services;
using TuinFounder.Validators;

namespace TuinFounder.ViewModels;

public partial class ResetPasswordViewModel : ViewModelBase
{
    private readonly ResetPasswordService _service;

    public ResetPasswordViewModel(
        ResetPasswordService service,
        string session
    )
    {
        _service = service;
        Session = session;
    }

    private string Session { get; }
    [ObservableProperty] private partial string? ErrorMessage { get; set; } = null;

    [ObservableProperty] private partial bool IsLoading { get; set; } = false;

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
    private async Task Submit()
    {
        if (IsLoading) return;

        ValidateAllProperties();

        if (HasErrors) return;

        ErrorMessage = null;
        IsLoading = true;

        try
        {
            await _service.ResetPasswordAsync(Session, Password, ConfirmPassword);
            // TODO: Navigate to Login
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