using System.Windows;
using System.Windows.Controls;

namespace ERP.Desktop.Behaviors
{
    public static class PasswordBoxBehavior
    {
        public static bool GetIsLoading(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsLoadingProperty);
        }

        public static void SetIsLoading(DependencyObject obj, bool value)
        {
            obj.SetValue(IsLoadingProperty, value);
        }

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.RegisterAttached(
                "IsLoading",
                typeof(bool),
                typeof(PasswordBoxBehavior),
                new PropertyMetadata(false, OnIsLoadingChanged));

        private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox passwordBox)
            {
                passwordBox.IsEnabled = !(bool)e.NewValue;
            }
        }
    }
}
