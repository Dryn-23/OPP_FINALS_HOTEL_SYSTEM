using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace OOP_FINALS
{
    public partial class AddStaffWindow : Window
    {
        private readonly string connectionString = @"Server=DESKTOP-51HVDT7;Database=JohnCis_HotelManagement_System;Trusted_Connection=True;";
        private bool isPasswordVisible = false;

        public string FullName { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string ContactNumber { get; private set; }
        public string Email { get; private set; }
        public string Department { get; private set; }
        public bool IsSaved { get; private set; }

        public AddStaffWindow()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ResetErrors(); // Reset previous errors

            if (ValidateInput())
            {
                try
                {
                    SaveToDatabase();
                    MessageBox.Show("✅ Staff member added successfully!", "Success",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    IsSaved = true;
                    DialogResult = true;
                    this.Close();
                }
                catch (SqlException ex)
                {
                    MessageBox.Show("❌ Database Error: " + ex.Message, "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("❌ Error: " + ex.Message, "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private bool ValidateInput()
        {
            bool isValid = true;

            // First Name
            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) || FirstNameTextBox.Text == "First Name")
            {
                ShowError(FirstNameTextBox, "Please enter first name.");
                isValid = false;
            }

            // Last Name
            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text) || LastNameTextBox.Text == "Last Name")
            {
                ShowError(LastNameTextBox, "Please enter last name.");
                isValid = false;
            }

            // Username
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                ShowError(UsernameTextBox, "Please enter username.");
                isValid = false;
            }

            // Password
            if (PasswordBox.Password.Length < 3)
            {
                ShowError(PasswordBox, "Password must be at least 3 characters.");
                isValid = false;
            }

            // Contact Number
            if (string.IsNullOrWhiteSpace(ContactTextBox.Text))
            {
                ShowError(ContactTextBox, "Please enter contact number.");
                isValid = false;
            }

            return isValid;
        }

        private void ShowError(Control control, string message)
        {
            // Apply error styling to control
            control.Style = (Style)FindResource("ErrorInput");
            MessageBox.Show(message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            control.Focus();
        }

        private void ResetErrors()
        {
            // Reset all controls to normal style
            FirstNameTextBox.Style = (Style)FindResource("ModernInput");
            LastNameTextBox.Style = (Style)FindResource("ModernInput");
            UsernameTextBox.Style = (Style)FindResource("ModernInput");
            PasswordBox.Style = (Style)FindResource("ModernPassword");
            ContactTextBox.Style = (Style)FindResource("ModernInput");
            EmailTextBox.Style = (Style)FindResource("ModernInput");
        }

        private void SaveToDatabase()
        {
            FullName = $"{FirstNameTextBox.Text.Trim()} {LastNameTextBox.Text.Trim()}";
            Username = UsernameTextBox.Text.Trim();
            Password = PasswordBox.Password;
            ContactNumber = ContactTextBox.Text.Trim();
            Email = EmailTextBox.Text.Trim();
            Department = GetSelectedDepartment();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "INSERT INTO Staff (RoleID, Fullname, Username, PasswordHash, ContactNumber, Email, Status) VALUES (@RoleID, @Fullname, @Username, @PasswordHash, @ContactNumber, @Email, @Status)";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@RoleID", GetRoleId(Department));
                    cmd.Parameters.AddWithValue("@Fullname", FullName);
                    cmd.Parameters.AddWithValue("@Username", Username);
                    cmd.Parameters.AddWithValue("@PasswordHash", HashPassword(Password));
                    cmd.Parameters.AddWithValue("@ContactNumber", ContactNumber);
                    cmd.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(Email) ? (object)DBNull.Value : Email);
                    cmd.Parameters.AddWithValue("@Status", "Active");

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private string GetSelectedDepartment()
        {
            if (DepartmentComboBox.SelectedItem is ComboBoxItem item)
                return item.Content.ToString();
            return "Receptionist";
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        private int GetRoleId(string department)
        {
            switch(department)
{
    case "Receptionist": return 1;
                case "Housekeeping": return 2;
                case "Manager": return 3;
                case "Maintenance": return 4;
                case "Accounting": return 5;
                case "Security": return 6;
                default: return 1;
                }
            }

        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (isPasswordVisible)
            {
                // Switch to PasswordBox (hide text)
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordBox.Visibility = Visibility.Visible;
                PasswordTextBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Switch to TextBox (show text)
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
            }
            isPasswordVisible = !isPasswordVisible;
        }
    }
}