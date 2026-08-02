using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grpc.Core;
using TuinFounder.Services;
using TuinFounder.Validators;

namespace TuinFounder.ViewModels;

public enum SessionType
{
    Undefine,
    Totp,
    Account,
    Verification
}

public partial class VerificationViewModel : ViewModelBase
{
    private readonly string _errMessage = "Something went wrong, try again";
    private readonly VerificationService _service;

    public VerificationViewModel(
        VerificationService service,
        string session,
        SessionType sessionType
    )
    {
        _service = service;
        Session = session;
        SessionT = sessionType;
        UpdateCodeType();
    }

    [ObservableProperty] private partial string Session { get; set; }
    [ObservableProperty] private partial SessionType SessionT { get; set; }
    private CodeType CodeT { get; set; }

    [ObservableProperty] public partial string? ErrorMessage { get; set; } = null;

    [ObservableProperty] public partial bool IsLoading { get; set; } = false;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Code is required")]
    [CustomValidation(typeof(VerificationViewModel), nameof(ValidateCode))]
    public partial string Code { get; set; } = string.Empty;

    public static ValidationResult? ValidateCode(string? code, ValidationContext context)
    {
        if (context.ObjectInstance is not VerificationViewModel vm) return ValidationResult.Success;
        var validator = new CodeValidator(vm.CodeT);
        return validator.GetValidationResult(code, context);
    }

    private void UpdateCodeType()
    {
        CodeT = SessionT is SessionType.Account or SessionType.Verification
            ? CodeType.Email
            : CodeType.Totp;
        ClearErrors(nameof(Code));
    }

    [RelayCommand]
    private void ToggleCodeType()
    {
        if (SessionT != SessionType.Totp) return;
        CodeT = CodeT == CodeType.Totp ? CodeType.Recovery : CodeType.Totp;
        Code = string.Empty;
        ClearErrors(nameof(Code));
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
            switch (SessionT)
            {
                case SessionType.Totp:
                    await HandleTwoFactorLogin();
                    break;
                case SessionType.Account:
                    await HandleAccountVerification();
                    break;
                case SessionType.Verification:
                    await HandleVerification();
                    break;
            }
        }
        catch (RpcException e)
        {
            ShowDialog(e);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task HandleVerification()
    {
        var response = await _service.VerificationAsync(Session, Code);
        // TODO: Navigate to ResetPassword
    }

    private async Task HandleTwoFactorLogin()
    {
        LoginTwoFactorReq req = CodeT == CodeType.Totp
            ? new LoginTwoFactorReq.Totp(Code)
            : new LoginTwoFactorReq.Recovery(Code);

        await _service.LoginTwoFactorAsync(Session, req);
        // TODO: Navigate to Home / Dashboard
    }

    private async Task HandleAccountVerification()
    {
        var response = await _service.AccountVerificationAsync(Session, Code);

        switch (response)
        {
            case AccountVerificationResult.Success:
                // TODO: Navigate to Dashboard / Home
                break;
            case AccountVerificationResult.Reset reset:
                SessionT = SessionType.Verification;
                Session = reset.Session;
                UpdateCodeType();
                break;
            case AccountVerificationResult.Totp totp:
                Session = totp.Session;
                SessionT = SessionType.Totp;
                UpdateCodeType();
                break;
        }
    }

    [RelayCommand]
    private async Task Resend()
    {
        ErrorMessage = null;
        IsLoading = true;

        try
        {
            var response = await _service.ResendAsync(Session);
            Session = response.Session;
            Code = string.Empty;
        }
        catch (RpcException e)
        {
            ShowDialog(e);
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

    private void ShowDialog(RpcException e)
    {
        switch (e.StatusCode)
        {
            case StatusCode.Aborted:
                //TODO: Show Dialog session expired and redirect to Login
                break;
            case StatusCode.ResourceExhausted:
                // TODO: Show Dialog Rate limit and redirect to Login
                break;
            default:
                ErrorMessage = e.Status.Detail;
                break;
        }
    }
}