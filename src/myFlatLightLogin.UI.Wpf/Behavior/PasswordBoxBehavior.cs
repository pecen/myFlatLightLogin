using Microsoft.Xaml.Behaviors;
using myFlatLightLogin.UI.Wpf.MVVM.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace myFlatLightLogin.UI.Wpf.Behavior
{
    public sealed class PasswordBoxBehavior : Behavior<PasswordBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PasswordChanged += AssociatedObjectPasswordChanged;
        }

        private void AssociatedObjectPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (AssociatedObject is PasswordBox associatedPasswordBox)
            {
                var vm = (LoginViewModel)associatedPasswordBox.DataContext;
                if (vm != null)
                {
                    if (associatedPasswordBox.SecurePassword.Length > 0)
                    {
                        vm.PwdIsEmpty = false;
                    }
                    else
                    {
                        vm.PwdIsEmpty = true;
                    }
                }
            }
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PasswordChanged -= AssociatedObjectPasswordChanged;
            base.OnDetaching();
        }
    }
}
