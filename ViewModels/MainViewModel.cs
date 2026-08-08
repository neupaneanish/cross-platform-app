using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using TuinFounder.Services;

namespace TuinFounder.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel(
        ITokenService tokenService,
        PublicViewModel publicViewModel,
        PrivateViewModel privateViewModel
    )
    {
        CurrentViewModel = tokenService.IsAuthenticated() ? privateViewModel : publicViewModel;

        tokenService.SessionExpired += isExpired =>
            Dispatcher.UIThread.Post(() => { SessionExpired = isExpired; });

        tokenService.AuthChanged += isAuthenticated =>
            Dispatcher.UIThread.Post(() => CurrentViewModel = isAuthenticated ? privateViewModel : publicViewModel);
    }

    [ObservableProperty] public partial ViewModelBase CurrentViewModel { get; set; }
    [ObservableProperty] public partial bool SessionExpired { get; set; }
}