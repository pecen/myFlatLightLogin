using MahApps.Metro.IconPacks;
using System.Windows;
using System.Windows.Controls;

namespace myFlatLightLogin.Core.Controls
{
    /// <summary>
    /// A reusable password input control with show/hide toggle functionality.
    /// </summary>
    public partial class TogglePwdBox : UserControl
    {
        public TogglePwdBox()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        /// <summary>
        /// The password value.
        /// </summary>
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(
                nameof(Password),
                typeof(string),
                typeof(TogglePwdBox),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnPasswordChanged));

        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TogglePwdBox)d;
            var newPassword = e.NewValue as string ?? string.Empty;

            // Update IsPasswordEmpty
            control.IsPasswordEmpty = string.IsNullOrEmpty(newPassword);

            // Sync with internal controls only if they don't already have the same value
            if (control.IsPasswordVisible)
            {
                if (control.textBox.Text != newPassword)
                    control.textBox.Text = newPassword;
            }
            else
            {
                if (control.passwordBox.Password != newPassword)
                    control.passwordBox.Password = newPassword;
            }
        }

        /// <summary>
        /// The placeholder text to display when the password is empty.
        /// </summary>
        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(
                nameof(Placeholder),
                typeof(string),
                typeof(TogglePwdBox),
                new PropertyMetadata("Enter Password"));

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        /// <summary>
        /// Whether to show a left icon (e.g., lock icon).
        /// </summary>
        public static readonly DependencyProperty ShowLeftIconProperty =
            DependencyProperty.Register(
                nameof(ShowLeftIcon),
                typeof(bool),
                typeof(TogglePwdBox),
                new PropertyMetadata(false));

        public bool ShowLeftIcon
        {
            get => (bool)GetValue(ShowLeftIconProperty);
            set => SetValue(ShowLeftIconProperty, value);
        }

        /// <summary>
        /// The icon to display on the left side (requires ShowLeftIcon=true).
        /// </summary>
        public static readonly DependencyProperty LeftIconKindProperty =
            DependencyProperty.Register(
                nameof(LeftIconKind),
                typeof(PackIconMaterialKind),
                typeof(TogglePwdBox),
                new PropertyMetadata(PackIconMaterialKind.LockOutline));

        public PackIconMaterialKind LeftIconKind
        {
            get => (PackIconMaterialKind)GetValue(LeftIconKindProperty);
            set => SetValue(LeftIconKindProperty, value);
        }

        /// <summary>
        /// Whether the password is currently visible (internal state).
        /// </summary>
        public static readonly DependencyProperty IsPasswordVisibleProperty =
            DependencyProperty.Register(
                nameof(IsPasswordVisible),
                typeof(bool),
                typeof(TogglePwdBox),
                new PropertyMetadata(false, OnIsPasswordVisibleChanged));

        public bool IsPasswordVisible
        {
            get => (bool)GetValue(IsPasswordVisibleProperty);
            private set => SetValue(IsPasswordVisibleProperty, value);
        }

        private static void OnIsPasswordVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TogglePwdBox)d;
            var isVisible = (bool)e.NewValue;

            if (isVisible)
            {
                // Switch to TextBox
                control.textBox.Text = control.Password;
                control.textBox.Visibility = Visibility.Visible;
                control.passwordBox.Visibility = Visibility.Collapsed;
                control.textBox.Focus();
                control.textBox.CaretIndex = control.textBox.Text.Length;
            }
            else
            {
                // Switch to PasswordBox
                control.passwordBox.Password = control.Password;
                control.passwordBox.Visibility = Visibility.Visible;
                control.textBox.Visibility = Visibility.Collapsed;
                control.passwordBox.Focus();
            }
        }

        /// <summary>
        /// Whether the password is empty (for placeholder visibility).
        /// </summary>
        public static readonly DependencyProperty IsPasswordEmptyProperty =
            DependencyProperty.Register(
                nameof(IsPasswordEmpty),
                typeof(bool),
                typeof(TogglePwdBox),
                new PropertyMetadata(true));

        public bool IsPasswordEmpty
        {
            get => (bool)GetValue(IsPasswordEmptyProperty);
            private set => SetValue(IsPasswordEmptyProperty, value);
        }

        #endregion

        #region Event Handlers

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!IsPasswordVisible)
            {
                Password = passwordBox.Password;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsPasswordVisible)
            {
                Password = textBox.Text;
            }
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        #endregion
    }
}
