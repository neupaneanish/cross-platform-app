using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Avalonia.Threading;
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
    private readonly Action _onNavigateToLogin;
    private readonly Action<string> _onNavigateToResetPassword;
    private readonly VerificationService _service;
    private readonly DispatcherTimer _timer;

    public VerificationViewModel(
        VerificationService service,
        string session,
        SessionType sessionType,
        Action<string> onNavigateToResetPassword,
        Action onNavigateToLogin
    )
    {
        _service = service;
        Session = session;
        SessionT = sessionType;
        _onNavigateToResetPassword = onNavigateToResetPassword;
        _onNavigateToLogin = onNavigateToLogin;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;

        UpdateCodeType();
        UpdateHeader();
        StartTimer();
    }

    public bool ToggleOrResendEnabled => (SessionT == SessionType.Totp || TimeLeft <= TimeSpan.Zero) && !IsLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedTime))]
    [NotifyPropertyChangedFor(nameof(ToggleOrResendText))]
    [NotifyPropertyChangedFor(nameof(ToggleOrResendEnabled))]
    private partial TimeSpan TimeLeft { get; set; } = TimeSpan.FromMinutes(2);

    public string FormattedTime => TimeLeft > TimeSpan.Zero
        ? $"Resend in {TimeLeft.Minutes:D2}:{TimeLeft.Seconds:D2}"
        : "Resend Code";

    [ObservableProperty] private partial string Session { get; set; }
    [ObservableProperty] private partial SessionType SessionT { get; set; }
    private CodeType CodeT { get; set; }

    [ObservableProperty] public partial string? ErrorMessage { get; set; } = null;

    [ObservableProperty] public partial bool IsLoading { get; private set; } = false;

    [ObservableProperty] public partial string Header { get; set; } = string.Empty;

    public string ToggleOrResendText => SessionT switch
    {
        SessionType.Totp => CodeT == CodeType.Totp ? "Use recovery code" : "Use Authentication app",
        SessionType.Verification or SessionType.Account => FormattedTime,
        _ => string.Empty
    };

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Code is required")]
    [CustomValidation(typeof(VerificationViewModel), nameof(ValidateCode))]
    public partial string Code { get; set; } = string.Empty;

    private void StartTimer(int minutes = 2)
    {
        if (SessionT == SessionType.Totp) return;
        _timer.Stop();
        TimeLeft = TimeSpan.FromMinutes(minutes);
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (TimeLeft > TimeSpan.Zero)
            TimeLeft = TimeLeft.Subtract(TimeSpan.FromSeconds(1));
        else
            _timer.Stop();
    }

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
        OnPropertyChanged(nameof(ToggleOrResendText));
        OnPropertyChanged(nameof(ToggleOrResendEnabled));
        ClearErrors(nameof(Code));
    }

    [RelayCommand]
    private void NavigateToLogin()
    {
        _timer.Stop();
        _onNavigateToLogin();
    }

    [RelayCommand]
    private void ToggleCodeType()
    {
        if (SessionT != SessionType.Totp) return;
        CodeT = CodeT == CodeType.Totp ? CodeType.Recovery : CodeType.Totp;
        OnPropertyChanged(nameof(ToggleOrResendText));
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
        catch (Exception)
        {
            ErrorMessage = _errMessage;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task HandleVerification()
    {
        var response = await _service.VerificationAsync(Session, Code);
        _timer.Stop();
        _onNavigateToResetPassword(response.Session);
    }

    private async Task HandleTwoFactorLogin()
    {
        LoginTwoFactorReq req = CodeT == CodeType.Totp
            ? new LoginTwoFactorReq.Totp(Code)
            : new LoginTwoFactorReq.Recovery(Code);

        await _service.LoginTwoFactorAsync(Session, req);
        _timer.Stop();
        // TODO: Navigate to Home / Dashboard
    }

    private async Task HandleAccountVerification()
    {
        var response = await _service.AccountVerificationAsync(Session, Code);

        switch (response)
        {
            case AccountVerificationResult.Success:
                _timer.Stop();
                break;
            case AccountVerificationResult.Reset reset:
                SessionT = SessionType.Verification;
                Session = reset.Session;
                UpdateCodeType();
                StartTimer();
                break;
            case AccountVerificationResult.Totp totp:
                Session = totp.Session;
                SessionT = SessionType.Totp;
                UpdateCodeType();
                StartTimer();
                break;
        }
    }

    [RelayCommand]
    private async Task Resend()
    {
        if (!ToggleOrResendEnabled) return;

        ErrorMessage = null;
        IsLoading = true;

        try
        {
            var response = await _service.ResendAsync(Session);
            Session = response.Session;
            Code = string.Empty;
            StartTimer();
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

    private void UpdateHeader()
    {
        Header = SessionT switch
        {
            SessionType.Totp => "Two Factor Verification",
            SessionType.Account => "Account Verification",
            SessionType.Verification => "Reset Verification",
            _ => string.Empty
        };
    }

    [RelayCommand]
    private async Task ToggleOrResend()
    {
        if (SessionT == SessionType.Totp)
            ToggleCodeType();

        else
            await Resend();
    }
}