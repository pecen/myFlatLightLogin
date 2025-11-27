using FlatLightLogin;
using Microsoft.Extensions.DependencyInjection;
using myFlatLightLogin.UI.Wpf.MVVM.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace myFlatLightLogin.UI.Wpf.MVVM.View
{
    /// <summary>
    /// Interaction logic for RoleManagement.xaml
    /// </summary>
    public partial class RoleManagement : UserControl
    {
        public RoleManagement()
        {
            InitializeComponent();

            // Explicitly set DataContext from DI container
            // This ensures the ViewModel is properly bound even when using DataTemplates
            if (Application.Current is App app && app.ServiceProvider != null)
            {
                DataContext = app.ServiceProvider.GetRequiredService<RoleManagementViewModel>();
            }
        }
    }
}
