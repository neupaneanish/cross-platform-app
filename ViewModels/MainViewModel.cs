using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using TuinFounder.Services;

namespace TuinFounder.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private ViewModelBase _currentViewModel;

    public MainViewModel(
        ITokenService tokenService,
        PublicViewModel publicViewModel,
        PrivateViewModel privateViewModel
    )
    {
        _currentViewModel = tokenService.IsAuthenticated() ? privateViewModel : publicViewModel;

        tokenService.AuthChanged += isAuthenticated =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _currentViewModel = isAuthenticated ? privateViewModel : publicViewModel;
            });
        };
    }
}