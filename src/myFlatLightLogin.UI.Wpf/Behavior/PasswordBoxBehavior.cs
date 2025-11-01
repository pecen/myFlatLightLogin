using Microsoft.Xaml.Behaviors;
using myFlatLightLogin.Core.Enums;
using myFlatLightLogin.Core.Extensions;
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
                if (associatedPasswordBox.Name == TextBoxNames.txtPassword.ToString())
                {
                    var vm = (IAuthenticateUser)associatedPasswordBox.DataContext;

                    if (vm != null)
                    {
                        vm.Password = associatedPasswordBox.Password;
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
                else if (associatedPasswordBox.Name == TextBoxNames.txtConfirmPassword.ToString())
                {
                    var vm = (IAuthenticateConfirmUser)associatedPasswordBox.DataContext;

                    if (vm != null)
                    {
                        vm.ConfirmPassword = associatedPasswordBox.Password;
                        if (associatedPasswordBox.SecurePassword.Length > 0)
                        {
                            vm.ConfirmPwdIsEmpty = false;
                        }
                        else
                        {
                            vm.ConfirmPwdIsEmpty = true;
                        }
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
