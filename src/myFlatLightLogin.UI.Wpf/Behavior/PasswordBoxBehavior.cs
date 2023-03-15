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
    public sealed class PasswordBoxBehavior : Behavior<UIElement>
    {
        private static int pwdLength = 0;

        protected override void OnAttached()
        {
            base.OnAttached();
            //AssociatedObject.LostKeyboardFocus += AssociatedObjectLostKeyboardFocus;
            AssociatedObject.KeyDown += AssociatedObjectKeyDown;
            AssociatedObject.KeyUp += AssociatedObjectKeyUp;
        }

        private void AssociatedObjectKeyUp(object sender, KeyEventArgs e)
        {
            var associatedPasswordBox = AssociatedObject as PasswordBox;
            if (associatedPasswordBox != null)
            {
                if (e.Key == Key.Back && pwdLength > 0) //Keyboard.IsKeyDown(Key.Back))
                {
                    pwdLength--;
                }
                else
                {
                    return;
                }

                var vm = (MainWindowViewModel)associatedPasswordBox.DataContext;
                if (pwdLength == 0 && associatedPasswordBox.Password.Length == 0) 
                {
                    vm.PwdIsEmpty = true;
                }
                else if(pwdLength != associatedPasswordBox.Password.Length)
                {
                    pwdLength = associatedPasswordBox.Password.Length;
                }
            }
        }

        private void AssociatedObjectKeyDown(object sender, KeyEventArgs e)
        {
            var associatedPasswordBox = AssociatedObject as PasswordBox;
            if (associatedPasswordBox != null)
            {
                pwdLength++;

                var vm = (MainWindowViewModel)associatedPasswordBox.DataContext;
                if (pwdLength > 0) 
                {
                    vm.PwdIsEmpty = false;
                }
            }
        }

        protected override void OnDetaching()
        {
            //AssociatedObject.LostKeyboardFocus -= AssociatedObjectLostKeyboardFocus;
            AssociatedObject.KeyDown -= AssociatedObjectKeyDown;
            base.OnDetaching();
        }

        //void AssociatedObjectLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        //{
        //    var associatedPasswordBox = AssociatedObject as PasswordBox;
        //    if (associatedPasswordBox != null)
        //    {
        //        // Set your view-model's Password property here

        //    }
        //}
    }
}
