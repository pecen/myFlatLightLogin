using myFlatLightLogin.Core.MVVM;
using myFlatLightLogin.Core.Services;
using myFlatLightLogin.Dal.Dto;
using myFlatLightLogin.DalFirebase;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace myFlatLightLogin.UI.Wpf.MVVM.ViewModel
{
    /// <summary>
    /// ViewModel for managing Firebase roles (Admin functionality).
    /// </summary>
    public class RoleManagementViewModel : ViewModelBase
    {
        private readonly RoleDal _roleDal;

        #region Properties

        private ObservableCollection<RoleDto> _roles = new ObservableCollection<RoleDto>();
        public ObservableCollection<RoleDto> Roles
        {
            get => _roles;
            set => SetProperty(ref _roles, value);
        }

        private RoleDto _selectedRole;
        public RoleDto SelectedRole
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

            // Get auth token from current user for Firebase authentication
            var authToken = CurrentUserService.Instance.CurrentUser?.FirebaseAuthToken;
            _roleDal = new RoleDal(authToken);

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
        /// Loads all roles from Firebase.
        /// </summary>
        private async Task LoadRolesAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading roles from Firebase...";
                Log.Information("Loading roles from Firebase...");

                // Ensure Firebase is initialized
                await _roleDal.InitializeAsync();

                var roles = await Task.Run(() => _roleDal.GetAllRoles());

                Log.Information($"Retrieved {roles?.Count ?? 0} roles from Firebase");

                Roles.Clear();
                if (roles != null && roles.Count > 0)
                {
                    foreach (var role in roles)
                    {
                        Log.Information($"Adding role: ID={role.Id}, Name={role.Name}, Description={role.Description}");
                        Roles.Add(role);
                    }
                    StatusMessage = $"Loaded {Roles.Count} roles successfully.";
                    Log.Information($"Successfully loaded {Roles.Count} roles into UI");
                }
                else
                {
                    StatusMessage = "No roles found in Firebase. Click 'Seed Default Roles' to create them.";
                    Log.Warning("No roles found in Firebase");
                }

                // Update the form to show next auto-generated ID
                ClearForm();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading roles: {ex.Message}";
                Log.Error($"Error loading roles: {ex.Message}");
                MessageBox.Show($"Error loading roles: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Seeds default roles (User and Admin) in Firebase.
        /// </summary>
        private async Task SeedDefaultRolesAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Seeding default roles...";
                Log.Information("Seeding default roles...");

                // Initialize Firebase
                await _roleDal.InitializeAsync();

                // Create User role
                var userRole = new RoleDto
                {
                    Id = 1,
                    Name = "User",
                    Description = "Standard user with basic permissions"
                };

                // Create Admin role
                var adminRole = new RoleDto
                {
                    Id = 2,
                    Name = "Admin",
                    Description = "Administrator with elevated permissions (e.g., view logs, manage users)"
                };

                await Task.Run(() =>
                {
                    _roleDal.InsertRole(userRole);
                    _roleDal.InsertRole(adminRole);
                });

                StatusMessage = "Default roles seeded successfully!";
                Log.Information("Default roles seeded successfully");
                MessageBox.Show("Default roles (User and Admin) have been created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Reload roles
                await LoadRolesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error seeding roles: {ex.Message}";
                Log.Error($"Error seeding roles: {ex.Message}");
                MessageBox.Show($"Error seeding roles: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Adds a new role to Firebase.
        /// </summary>
        private async Task AddRoleAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(EditRoleName))
                {
                    MessageBox.Show("Role name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsLoading = true;
                StatusMessage = "Adding new role...";
                Log.Information($"Adding new role: {EditRoleName}");

                // Auto-generate next Role ID
                // Start from 3 since User=1 and Admin=2 are reserved
                int nextId = Roles.Count > 0 ? Roles.Max(r => r.Id) + 1 : 3;

                var newRole = new RoleDto
                {
                    Id = nextId,
                    Name = EditRoleName,
                    Description = EditRoleDescription
                };

                bool success = await Task.Run(() => _roleDal.InsertRole(newRole));

                if (success)
                {
                    StatusMessage = $"Role '{EditRoleName}' added successfully!";
                    Log.Information($"Role '{EditRoleName}' added successfully");
                    MessageBox.Show($"Role '{EditRoleName}' has been added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearForm();
                    await LoadRolesAsync();
                }
                else
                {
                    StatusMessage = "Failed to add role.";
                    MessageBox.Show("Failed to add role.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding role: {ex.Message}";
                Log.Error($"Error adding role: {ex.Message}");
                MessageBox.Show($"Error adding role: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Updates an existing role in Firebase.
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

                if (string.IsNullOrWhiteSpace(EditRoleName))
                {
                    MessageBox.Show("Role name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsLoading = true;
                StatusMessage = "Updating role...";
                Log.Information($"Updating role: {EditRoleName}");

                var updatedRole = new RoleDto
                {
                    Id = EditRoleId,
                    Name = EditRoleName,
                    Description = EditRoleDescription
                };

                bool success = await Task.Run(() => _roleDal.UpdateRole(updatedRole));

                if (success)
                {
                    StatusMessage = $"Role '{EditRoleName}' updated successfully!";
                    Log.Information($"Role '{EditRoleName}' updated successfully");
                    MessageBox.Show($"Role '{EditRoleName}' has been updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearForm();
                    await LoadRolesAsync();
                }
                else
                {
                    StatusMessage = "Failed to update role.";
                    MessageBox.Show("Failed to update role.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating role: {ex.Message}";
                Log.Error($"Error updating role: {ex.Message}");
                MessageBox.Show($"Error updating role: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Deletes a role from Firebase.
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
                Log.Information($"Deleting role: {SelectedRole.Name}");

                bool success = await Task.Run(() => _roleDal.DeleteRole(SelectedRole.Id));

                if (success)
                {
                    StatusMessage = $"Role '{SelectedRole.Name}' deleted successfully!";
                    Log.Information($"Role '{SelectedRole.Name}' deleted successfully");
                    MessageBox.Show($"Role '{SelectedRole.Name}' has been deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearForm();
                    await LoadRolesAsync();
                }
                else
                {
                    StatusMessage = "Failed to delete role.";
                    MessageBox.Show("Failed to delete role.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting role: {ex.Message}";
                Log.Error($"Error deleting role: {ex.Message}");
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
