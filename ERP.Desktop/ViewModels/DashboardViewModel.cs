using ERP.Desktop.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ERP.Desktop.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _api;
        private string _userGreeting = string.Empty;

        public string UserGreeting
        {
            get => _userGreeting;
            set => SetField(ref _userGreeting, value);
        }

        public DashboardViewModel(ApiService api)
        {
            _api = api;
            UserGreeting = $"Olá, {api.CurrentUser?.FullName} ({api.CurrentUser?.Role}) 👋";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
