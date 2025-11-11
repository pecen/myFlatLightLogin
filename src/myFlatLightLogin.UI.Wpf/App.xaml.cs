using Microsoft.Extensions.DependencyInjection;
using myFlatLightLogin.Core.MVVM;
using myFlatLightLogin.Core.Services;
using myFlatLightLogin.UI.Wpf.MVVM.View;
using myFlatLightLogin.UI.Wpf.MVVM.ViewModel;
using Serilog;
using System;
using System.IO;
using System.Windows;

namespace FlatLightLogin
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;

        public App()
        {
            // Configure Serilog
            ConfigureSerilog();

            IServiceCollection services = new ServiceCollection();

            services.AddSingleton(provider => new MainWindow
            {
                DataContext = provider.GetRequiredService<MainWindowViewModel>()
            });

            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<LoginViewModel>();
            services.AddSingleton<RegisterUserViewModel>();
            services.AddSingleton<HomeViewModel>();

            services.AddSingleton<INavigationService, NavigationService>();

            services.AddSingleton<Func<Type, ViewModelBase>>(serviceProvider =>
                viewModelType => (ViewModelBase)serviceProvider.GetRequiredService(viewModelType));

            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureSerilog()
        {
            // Create logs directory if it doesn't exist
            var logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logsPath);

            // Configure Serilog
            // SECURITY NOTE: Exception details can contain sensitive data (passwords, tokens, etc.)
            // Always log ex.Message only, never pass full exception objects to logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: Path.Combine(logsPath, "myFlatLightLogin-.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                    retainedFileCountLimit: 30)
                .CreateLogger();

            Log.Information("========================================");
            Log.Information("Application Starting");
            Log.Information("========================================");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Log.Information("OnStartup called");

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("========================================");
            Log.Information("Application Shutting Down");
            Log.Information("========================================");

            // Ensure all logs are flushed before exit
            Log.CloseAndFlush();

            base.OnExit(e);
        }
    }
}
