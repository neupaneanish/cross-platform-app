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
        serviceCollection.AddTransient<LoginService>();

        // ViewModels
        serviceCollection.AddTransient<MainViewModel>();

        // Views
        serviceCollection.AddTransient<MainView>(sp =>
        {
            var viewModel = sp.GetRequiredService<MainViewModel>();
            return new MainView { DataContext = viewModel };
        });
    }
}