using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using TuinFounder.External.Authentication.V1;
using TuinFounder.Gateway.Authentication.V1;
using TuinFounder.Services;
using TuinFounder.ViewModels;
using TuinFounder.Views;

namespace TuinFounder;

public class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var serviceCollection = new ServiceCollection();
        RegisterServices(serviceCollection);
        _serviceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _serviceProvider.GetRequiredService<MainView>();
            desktop.ShutdownRequested += (_, _) => _serviceProvider.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void RegisterServices(IServiceCollection serviceCollection)
    {
        var url = new Uri("https://founder.tuin.dev");

        // gRPC Clients
        serviceCollection.AddGrpcClient<ExternalAuthenticationService.ExternalAuthenticationServiceClient>(client =>
            client.Address = url);
        serviceCollection.AddGrpcClient<GatewayAuthenticationService.GatewayAuthenticationServiceClient>(client =>
            client.Address = url);

        // Services
        serviceCollection.AddSingleton<ITokenService, TokenService>();
        serviceCollection.AddSingleton<AuthInterceptorService>();
        serviceCollection.AddTransient<LoginService>();
        serviceCollection.AddTransient<VerificationService>();
        serviceCollection.AddTransient<ForgetPasswordService>();
        serviceCollection.AddTransient<RegisterService>();
        serviceCollection.AddTransient<ResetPasswordService>();

        // ViewModels
        serviceCollection.AddTransient<MainViewModel>();
        serviceCollection.AddTransient<PublicViewModel>();
        serviceCollection.AddTransient<PrivateViewModel>();

        serviceCollection.AddTransient<Func<Action, Action, Action<string, SessionType>, LoginViewModel>>(sp =>
            (onRegister, onForgetPassword, onVerification) =>
                ActivatorUtilities.CreateInstance<LoginViewModel>(
                    sp,
                    onRegister,
                    onForgetPassword,
                    onVerification
                ));

        serviceCollection.AddTransient<Func<Action<string, SessionType>, Action, RegisterViewModel>>(sp =>
            (onVerification, onLogin) =>
                ActivatorUtilities.CreateInstance<RegisterViewModel>(sp, onVerification, onLogin));

        serviceCollection.AddTransient<Func<Action<string, SessionType>, Action, ForgetPasswordViewModel>>(sp =>
            (onVerification, onLogin) =>
                ActivatorUtilities.CreateInstance<ForgetPasswordViewModel>(sp, onVerification, onLogin));

        serviceCollection.AddTransient<Func<string, SessionType, Action<string>, Action, VerificationViewModel>>(sp =>
            (session, sessionType, onResetPassword, onLogin) =>
                ActivatorUtilities.CreateInstance<VerificationViewModel>(sp, session, sessionType, onResetPassword,
                    onLogin));

        serviceCollection.AddTransient<Func<string, Action, ResetPasswordViewModel>>(sp =>
            (token, onLogin) =>
                ActivatorUtilities.CreateInstance<ResetPasswordViewModel>(sp, token, onLogin));

        // Views
        serviceCollection.AddTransient<MainView>(sp =>
        {
            var viewModel = sp.GetRequiredService<MainViewModel>();
            return new MainView { DataContext = viewModel };
        });
    }
}