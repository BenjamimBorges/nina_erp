using System.ComponentModel;
using ERP.Desktop.Services;

namespace ERP.Desktop.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private string _userGreeting = string.Empty;
        private string _token = string.Empty;
        private readonly ITokenService _tokenService;

        public string UserGreeting
        {
            get => _userGreeting;
            set
            {
                _userGreeting = value;
                OnPropertyChanged(nameof(UserGreeting));
            }
        }

        public string Token
        {
            get => _token;
            set => _token = value;
        }

        public DashboardViewModel(string userFullName, string token)
        {
            _tokenService = new TokenService();
            UserGreeting = $"Bem-vindo, {userFullName}! 👋";
            Token = token;
        }

        public void Logout()
        {
            _tokenService.ClearToken();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
