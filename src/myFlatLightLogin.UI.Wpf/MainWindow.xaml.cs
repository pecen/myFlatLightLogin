using System;
using System.Windows;
using System.Windows.Input;

namespace FlatLightLogin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private bool IsMaximize = false;
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (IsMaximize)
                {
                    WindowState = WindowState.Normal;
                    Width = 1024;
                    Height = 720;

                    IsMaximize = false;
                }
                else
                {
                    WindowState = WindowState.Maximized;
                    IsMaximize = true;
                }
            }
        }
    }
}
