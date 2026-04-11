using OOP_FINALS;
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OOP_FINALS
{
    public partial class ResetPassword : Window
    {
        private string connectionString =
            @"Server=DESKTOP-51HVDT7;Database=JohnCis_HotelManagement_System;Trusted_Connection=True;";
        private string verifiedEmail;  // ✅ New field
        private bool isNewPasswordVisible = false;
        private bool isConfirmPasswordVisible = false;

        public ResetPassword(string email = null)
        {
            InitializeComponent();
            verifiedEmail = email ?? "";
            if (!string.IsNullOrEmpty(verifiedEmail))
                LoadUsernameFromEmail(verifiedEmail);
        }


        private void LoadUsernameFromEmail(string email)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("SELECT UserName FROM Staff WHERE Email=@e", conn))
                    {
                        cmd.Parameters.AddWithValue("@e", email);
                        var username = cmd.ExecuteScalar()?.ToString();
                        if (!string.IsNullOrEmpty(username))
                            txtUsername.Text = username;
                    }
                }
            }
            catch { }
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        // 🔄 SYNC PASSWORD → TEXTBOX
        private void txtNewPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isNewPasswordVisible)
                txtNewPasswordVisible.Text = txtNewPassword.Password;
        }

        private void txtConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isConfirmPasswordVisible)
                txtConfirmPasswordVisible.Text = txtConfirmPassword.Password;
        }

        // 🔄 SYNC TEXTBOX → PASSWORD
        private void txtNewPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isNewPasswordVisible)
                txtNewPassword.Password = txtNewPasswordVisible.Text;
        }

        private void txtConfirmPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isConfirmPasswordVisible)
                txtConfirmPassword.Password = txtConfirmPasswordVisible.Text;
        }

        // 👁 GENERIC TOGGLE METHOD
        private void TogglePassword(
            ref bool isVisible,
            PasswordBox passwordBox,
            TextBox textBox,
            Button button)
        {
            isVisible = !isVisible;

            if (isVisible)
            {
                textBox.Text = passwordBox.Password;
                textBox.Visibility = Visibility.Visible;
                passwordBox.Visibility = Visibility.Collapsed;
                button.Content = "🙈";
            }
            else
            {
                passwordBox.Password = textBox.Text;
                textBox.Visibility = Visibility.Collapsed;
                passwordBox.Visibility = Visibility.Visible;
                button.Content = "👁";
            }
        }

        private void ToggleNewPassword_Click(object sender, RoutedEventArgs e)
        {
            TogglePassword(ref isNewPasswordVisible, txtNewPassword, txtNewPasswordVisible, btnToggleNewPassword);
        }

        private void ToggleConfirmPassword_Click(object sender, RoutedEventArgs e)
        {
            TogglePassword(ref isConfirmPasswordVisible, txtConfirmPassword, txtConfirmPasswordVisible, btnToggleConfirmPassword);
        }

        // 🔐 RESET LOGIC
        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string newPass = txtNewPassword.Password.Trim();
            string confirmPass = txtConfirmPassword.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(confirmPass))
            {
                MessageBox.Show("Please fill in all fields.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPass.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters long.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPass != confirmPass)
            {
                MessageBox.Show("Passwords do not match.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNewPassword.Focus();
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // 🔹 1. Check current password
                    string checkQuery = @"SELECT PasswordHash FROM Staff WHERE UserName = @username";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@username", username);
                        object resultObj = checkCmd.ExecuteScalar();

                        if (resultObj == null)
                        {
                            MessageBox.Show("Username not found.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        string currentPassword = resultObj.ToString();

                        if (currentPassword == newPass) // ⚠ Compare old & new password
                        {
                            MessageBox.Show("You are using your old password. Please choose a new one.",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            txtNewPassword.Focus();
                            return;
                        }
                    }

                    // 🔹 2. Update password
                    string updateQuery = @"UPDATE Staff 
                                   SET PasswordHash = @password 
                                   WHERE UserName = @username";

                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", newPass); // ⚠ Hash in real apps

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Password successfully reset!",
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                            // Open main login window again
                            MainWindow login = new MainWindow();
                            login.Show();
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Failed to update password.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackToLogin_Click(object sender, MouseButtonEventArgs e)
        {
            MainWindow reset = new MainWindow();
            reset.Show();
            this.Close();
        }

    }
}