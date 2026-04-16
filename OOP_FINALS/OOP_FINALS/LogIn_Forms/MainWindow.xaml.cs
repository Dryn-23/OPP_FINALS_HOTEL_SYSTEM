using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Configuration;
using OOP_FINALS.Secret;

namespace OOP_FINALS
{
    public partial class MainWindow : Window
    {
        //SqlConnection conn;

        private readonly string connectionString =
            @"Server=DESKTOP-51HVDT7;Database=JohnCis_HotelManagement_System;Trusted_Connection=True;";

        public MainWindow()
        {
            InitializeComponent();
            //string connStr = ConfigurationManager.ConnectionStrings["MyDBConnectionString"].ConnectionString;
            //conn = new SqlConnection(connStr);
        }
        public class UserRole
        {
            public int RoleID { get; set; }
            public string RoleName { get; set; }
        }
        // Allow dragging window
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        // Minimize window
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        // Close application
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // LOGIN
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUser.Text.Trim();
            string password = txtPass.Password.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter username and password.");
                return;
            }

            try
            {
                var user = AuthenticateUser(username, password);

                if (user != null)
                {
                    // ✅ Check RoleID OR RoleName
                    if (user.RoleID == 1 || user.RoleName == "Admin")
                    {
                        new MainDashboard().Show();
                    }
                    else if (user.RoleName == "Manager")
                    {
                        VideoPlayerWindow videoWindow = new VideoPlayerWindow();
                        videoWindow.Show();
                        this.Close(); // Optional: close login window
                    }
                    else
                    {
                        new OOP_FINALS.HotelReservation.HotelReservation().Show();
                    }

                    this.Close();
                }
                else
                {
                    MessageBox.Show("Invalid username or password.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        // AUTHENTICATION METHOD
        private UserRole AuthenticateUser(string username, string password)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string query = @"SELECT r.RoleID, r.RoleName
                         FROM Staff s
                         JOIN Roles r ON s.RoleID = r.RoleID
                         WHERE s.UserName = @username
                         AND s.PasswordHash = @password";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new UserRole
                            {
                                RoleID = Convert.ToInt32(reader["RoleID"]),
                                RoleName = reader["RoleName"].ToString()
                            };
                        }
                    }
                }
            }

            return null;
        }
        private bool isPasswordVisible = false;

        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (isPasswordVisible)
            {
                txtPass.Password = txtPassVisible.Text;
                txtPass.Visibility = Visibility.Visible;
                txtPassVisible.Visibility = Visibility.Collapsed;
                isPasswordVisible = false;
            }
            else
            {
                txtPassVisible.Text = txtPass.Password;
                txtPass.Visibility = Visibility.Collapsed;
                txtPassVisible.Visibility = Visibility.Visible;
                isPasswordVisible = true;
            }
        }

        private void txtPass_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isPasswordVisible)
                txtPassVisible.Text = txtPass.Password;
        }

        private void txtPassVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isPasswordVisible)
                txtPass.Password = txtPassVisible.Text;
        }
        private void Reset_Click(object sender, MouseButtonEventArgs e)
        {
            OTPResetPassword reset = new OTPResetPassword();
            reset.Show();
            this.Close();
        }
       
        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                btnMaximize.Content = "□"; // Square for maximize
            }
            else
            {
                WindowState = WindowState.Maximized;
                btnMaximize.Content = "❐"; // Smaller square for restore
            }
        }

        

    }
}