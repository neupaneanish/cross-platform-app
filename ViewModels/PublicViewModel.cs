using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TuinFounder.ViewModels;

public partial class PublicViewModel : ViewModelBase
{
    private readonly Func<Action<string, SessionType>, Action, ForgetPasswordViewModel> _forgetPasswordFactory;
    private readonly Func<Action, Action, Action<string, SessionType>, LoginViewModel> _loginFactory;
    private readonly Func<Action<string, SessionType>, Action, RegisterViewModel> _registerFactory;
    private readonly Func<string, Action, ResetPasswordViewModel> _resetPasswordFactory;
    private readonly Func<string, SessionType, Action<string>, Action, VerificationViewModel> _verificationFactory;

    [ObservableProperty] private ViewModelBase _currentViewModel;

    public PublicViewModel(
        Func<Action, Action, Action<string, SessionType>, LoginViewModel> loginFactory,
        Func<Action<string, SessionType>, Action, RegisterViewModel> registerFactory,
        Func<Action<string, SessionType>, Action, ForgetPasswordViewModel> forgetPasswordFactory,
        Func<string, SessionType, Action<string>, Action, VerificationViewModel> verificationFactory,
        Func<string, Action, ResetPasswordViewModel> resetPasswordFactory
    )
    {
        _loginFactory = loginFactory;
        _registerFactory = registerFactory;
        _forgetPasswordFactory = forgetPasswordFactory;
        _verificationFactory = verificationFactory;
        _resetPasswordFactory = resetPasswordFactory;

        _currentViewModel = _loginFactory(NavigateToRegister, NavigateToForgetPassword, NavigateToVerification);
    }

    private void NavigateToLogin()
    {
        CurrentViewModel = _loginFactory(NavigateToRegister, NavigateToForgetPassword, NavigateToVerification);
    }


    private void NavigateToRegister()
    {
        CurrentViewModel = _registerFactory(NavigateToVerification, NavigateToLogin);
    }


    private void NavigateToForgetPassword()
    {
        CurrentViewModel = _forgetPasswordFactory(NavigateToVerification, NavigateToLogin);
    }


    private void NavigateToVerification(string session, SessionType type)
    {
        CurrentViewModel = _verificationFactory(session, type, NavigateToResetPassword, NavigateToLogin);
    }

    private void NavigateToResetPassword(string token)
    {
        CurrentViewModel = _resetPasswordFactory(token, NavigateToLogin);
    }
}