using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace myFlatLightLogin.UI.Wpf.Extension
{
    public static class PasswordBoxExtensions
    {
        private static bool _isUpdating = false;

        #region BoundPassword Attached Property

        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BoundPassword",
                typeof(string),
                typeof(PasswordBoxExtensions),
                new FrameworkPropertyMetadata(string.Empty, OnBoundPasswordChanged));

        public static string GetBoundPassword(DependencyObject d)
        {
            return (string)d.GetValue(BoundPasswordProperty);
        }

        public static void SetBoundPassword(DependencyObject d, string value)
        {
            d.SetValue(BoundPasswordProperty, value);
        }

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox passwordBox)
            {
                passwordBox.PasswordChanged -= OnPasswordBoxPasswordChanged;

                if (!_isUpdating)
                {
                    passwordBox.Password = (string)e.NewValue;
                }

                passwordBox.PasswordChanged += OnPasswordBoxPasswordChanged;
            }
        }

        private static void OnPasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                _isUpdating = true;
                SetBoundPassword(passwordBox, passwordBox.Password);
                _isUpdating = false;
            }
        }

        #endregion

        #region IsActive Property

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.RegisterAttached(
                "IsActive", typeof(bool), typeof(PasswordBoxExtensions),
                new FrameworkPropertyMetadata(OnIsActiveChanged));

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PasswordBox passwordBox) return;

            passwordBox.PasswordChanged -= OnPasswordChanged;
            if ((bool)e.NewValue)
            {
                SetIsPasswordEmpty(passwordBox);
                passwordBox.PasswordChanged += OnPasswordChanged;
            }
        }

        private static void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            SetIsPasswordEmpty((PasswordBox)sender);
        }

        public static void SetIsActive(PasswordBox element, bool value)
        {
            element.SetValue(IsActiveProperty, value);
        }

        public static bool GetIsActive(PasswordBox element)
        {
            return (bool)element.GetValue(IsActiveProperty);
        }

        #endregion

        #region IsPasswordEmpty Property

        public static readonly DependencyPropertyKey IsPasswordEmptyPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "IsPasswordEmpty", typeof(bool), typeof(PasswordBoxExtensions),
                new FrameworkPropertyMetadata());

        public static readonly DependencyProperty IsPasswordEmptyProperty =
            IsPasswordEmptyPropertyKey.DependencyProperty;

        private static void SetIsPasswordEmpty(PasswordBox element)
        {
            element.SetValue(IsPasswordEmptyPropertyKey, element.SecurePassword.Length == 0);
        }

        public static bool GetIsPasswordEmpty(PasswordBox element)
        {
            return (bool)element.GetValue(IsPasswordEmptyProperty);
        }

        #endregion
    }
}
