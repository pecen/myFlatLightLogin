using myFlatLightLogin.UI.Common.MVVM;
using myFlatLightLogin.Core.Services;
using myFlatLightLogin.Library;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    /// <summary>
    /// ViewModel for managing roles using BLL's RoleEdit and RoleList (Admin functionality).
    /// </summary>
    public class RoleManagementViewModel : ViewModelBase
    {
        private static readonly ILogger _logger = Log.ForContext<RoleManagementViewModel>();

        #region Properties

        private ObservableCollection<RoleInfo> _roles = new ObservableCollection<RoleInfo>();
        public ObservableCollection<RoleInfo> Roles
        {
            get => _roles;
            set => SetProperty(ref _roles, value);
        }

        private RoleInfo _selectedRole;
        public RoleInfo SelectedRole
        {
            get => _selectedRole;
            set
            {
                SetProperty(ref _selectedRole, value);
                if (value != null)
                {
                    EditRoleId = value.Id;
                    EditRoleName = value.Name;
                    EditRoleDescription = value.Description;
                }
            }
        }

        private int _editRoleId;
        public int EditRoleId
        {
            get => _editRoleId;
            set => SetProperty(ref _editRoleId, value);
        }

        private string _editRoleName;
        public string EditRoleName
        {
            get => _editRoleName;
            set => SetProperty(ref _editRoleName, value);
        }

        private string _editRoleDescription;
        public string EditRoleDescription
        {
            get => _editRoleDescription;
            set => SetProperty(ref _editRoleDescription, value);
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Commands

        public AsyncRelayCommand LoadRolesCommand { get; }
        public AsyncRelayCommand SeedDefaultRolesCommand { get; }
        public AsyncRelayCommand AddRoleCommand { get; }
        public AsyncRelayCommand UpdateRoleCommand { get; }
        public AsyncRelayCommand DeleteRoleCommand { get; }
        public RelayCommand ClearFormCommand { get; }
        public RelayCommand BackCommand { get; }

        #endregion

        public RoleManagementViewModel(INavigationService navigationService)
        {
            Navigation = navigationService;

            LoadRolesCommand = new AsyncRelayCommand(LoadRolesAsync, () => !IsLoading);
            SeedDefaultRolesCommand = new AsyncRelayCommand(SeedDefaultRolesAsync, () => !IsLoading);
            AddRoleCommand = new AsyncRelayCommand(AddRoleAsync, () => !IsLoading);
            UpdateRoleCommand = new AsyncRelayCommand(UpdateRoleAsync, () => !IsLoading && SelectedRole != null);
            DeleteRoleCommand = new AsyncRelayCommand(DeleteRoleAsync, () => !IsLoading && SelectedRole != null);
            ClearFormCommand = new RelayCommand(o => ClearForm(), o => true);
            BackCommand = new RelayCommand(o => Navigation.NavigateTo<HomeViewModel>(), o => true);

            // Load roles on initialization
            _ = LoadRolesAsync();
        }

        #region Methods

        /// <summary>
        /// Loads all roles using BLL's RoleList.
        /// </summary>
        private async Task LoadRolesAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading roles...";
                _logger.Information("Loading roles using BLL...");

                // Use BLL's RoleList to fetch all roles
                var roleList = await RoleList.GetRolesAsync();

                _logger.Information($"Retrieved {roleList?.Count ?? 0} roles from BLL");

                Roles.Clear();
                if (roleList != null && roleList.Count > 0)
                {
                    foreach (var role in roleList)
                    {
                        _logger.Information($"Adding role: ID={role.Id}, Name={role.Name}, Description={role.Description}");
                        Roles.Add(role);
                    }
                    StatusMessage = $"Loaded {Roles.Count} roles successfully.";
                    _logger.Information($"Successfully loaded {Roles.Count} roles into UI");
                }
                else
                {
                    StatusMessage = "No roles found. Click 'Seed Default Roles' to create them.";
                    _logger.Warning("No roles found");
                }

                // Update the form to show next auto-generated ID
                ClearForm();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading roles: {ex.Message}";
                _logger.Error(ex, "Error loading roles");
                MessageBox.Show($"Error loading roles: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Seeds default roles (User and Admin) using BLL's RoleEdit.
        /// </summary>
        private async Task SeedDefaultRolesAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Seeding default roles...";
                _logger.Information("Seeding default roles using BLL...");

                // Create User role
                var userRole = await RoleEdit.NewRoleAsync();
                userRole.Name = "User";
                userRole.Description = "Standard user with basic permissions";
                await userRole.SaveAsync();

                // Create Admin role
                var adminRole = await RoleEdit.NewRoleAsync();
                adminRole.Name = "Admin";
                adminRole.Description = "Administrator with elevated permissions (e.g., view logs, manage users)";
                await adminRole.SaveAsync();

                StatusMessage = "Default roles seeded successfully!";
                _logger.Information("Default roles seeded successfully");
                MessageBox.Show("Default roles (User and Admin) have been created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Reload roles
                await LoadRolesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error seeding roles: {ex.Message}";
                _logger.Error(ex, "Error seeding roles");
                MessageBox.Show($"Error seeding roles: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Adds a new role using BLL's RoleEdit.
        /// </summary>
        private async Task AddRoleAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Adding new role...";
                _logger.Information($"Adding new role: {EditRoleName}");

                // Create a new RoleEdit business object
                var newRole = await RoleEdit.NewRoleAsync();
                newRole.Name = EditRoleName;
                newRole.Description = EditRoleDescription;

                // Validate using BLL business rules
                if (!newRole.IsValid)
                {
                    var validationErrors = string.Join("\n", newRole.BrokenRulesCollection);
                    _logger.Warning("Role validation failed: {Errors}", validationErrors);
                    MessageBox.Show($"Please correct the following errors:\n\n{validationErrors}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Save the role (this will call Insert in the Data Access region)
                await newRole.SaveAsync();

                StatusMessage = $"Role '{EditRoleName}' added successfully!";
                _logger.Information($"Role '{EditRoleName}' added successfully");
                MessageBox.Show($"Role '{EditRoleName}' has been added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
                await LoadRolesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding role: {ex.Message}";
                _logger.Error(ex, "Error adding role");
                MessageBox.Show($"Error adding role: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Updates an existing role using BLL's RoleEdit.
        /// </summary>
        private async Task UpdateRoleAsync()
        {
            try
            {
                if (SelectedRole == null)
                {
                    MessageBox.Show("Please select a role to update.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsLoading = true;
                StatusMessage = "Updating role...";
                _logger.Information($"Updating role: {EditRoleName}");

                // Get the role to edit
                var roleEdit = await RoleEdit.GetRoleAsync(EditRoleId);
                roleEdit.Name = EditRoleName;
                roleEdit.Description = EditRoleDescription;

                // Validate using BLL business rules
                if (!roleEdit.IsValid)
                {
                    var validationErrors = string.Join("\n", roleEdit.BrokenRulesCollection);
                    _logger.Warning("Role validation failed: {Errors}", validationErrors);
                    MessageBox.Show($"Please correct the following errors:\n\n{validationErrors}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Save the role (this will call Update in the Data Access region)
                await roleEdit.SaveAsync();

                StatusMessage = $"Role '{EditRoleName}' updated successfully!";
                _logger.Information($"Role '{EditRoleName}' updated successfully");
                MessageBox.Show($"Role '{EditRoleName}' has been updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
                await LoadRolesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating role: {ex.Message}";
                _logger.Error(ex, "Error updating role");
                MessageBox.Show($"Error updating role: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Deletes a role using BLL's RoleEdit.
        /// </summary>
        private async Task DeleteRoleAsync()
        {
            try
            {
                if (SelectedRole == null)
                {
                    MessageBox.Show("Please select a role to delete.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Are you sure you want to delete role '{SelectedRole.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                IsLoading = true;
                StatusMessage = "Deleting role...";
                _logger.Information($"Deleting role: {SelectedRole.Name}");

                // Use BLL's static delete method
                await RoleEdit.DeleteRoleAsync(SelectedRole.Id);

                StatusMessage = $"Role '{SelectedRole.Name}' deleted successfully!";
                _logger.Information($"Role '{SelectedRole.Name}' deleted successfully");
                MessageBox.Show($"Role '{SelectedRole.Name}' has been deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
                await LoadRolesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting role: {ex.Message}";
                _logger.Error(ex, "Error deleting role");
                MessageBox.Show($"Error deleting role: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Clears the form fields and sets the next auto-generated Role ID.
        /// </summary>
        private void ClearForm()
        {
            SelectedRole = null;
            // Show the next auto-generated ID (starts from 3, as User=1 and Admin=2 are reserved)
            EditRoleId = Roles.Count > 0 ? Roles.Max(r => r.Id) + 1 : 3;
            EditRoleName = string.Empty;
            EditRoleDescription = string.Empty;
        }

        #endregion
    }
}
