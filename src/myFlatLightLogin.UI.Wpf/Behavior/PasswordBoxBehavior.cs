using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using myFlatLightLogin.UI.Wpf.MVVM.ViewModel;

namespace myFlatLightLogin.UI.Wpf.Behavior
{
    public sealed class PasswordBoxBehavior : Behavior<PasswordBox>
    {
        private static int pwdLength = 0;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PasswordChanged += AssociatedObjectPasswordChanged;
        }

        private void AssociatedObjectPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (AssociatedObject is PasswordBox associatedPasswordBox)
            {
                var vm = (MainWindowViewModel)associatedPasswordBox.DataContext;
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
