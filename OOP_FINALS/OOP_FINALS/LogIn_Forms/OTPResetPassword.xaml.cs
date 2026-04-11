using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace OOP_FINALS
{
    public partial class OTPResetPassword : Window
    {
        private readonly string connectionString = @"Server=DESKTOP-51HVDT7;Database=JohnCis_HotelManagement_System;Trusted_Connection=True;";
        private string storedOtp = "";
        private string verifiedEmail = "";

        public OTPResetPassword()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        // 🔥 SEND OTP
        private async void btnSendOtp_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim();
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            {
                MessageBox.Show("Enter valid email!", "Error");
                return;
            }

            if (!EmailExists(email))
            {
                MessageBox.Show("Email not found in records!", "Not Registered");
                return;
            }

            // Generate OTP
            Random rnd = new Random();
            storedOtp = rnd.Next(100000, 999999).ToString();
            verifiedEmail = email;

            bool sent = await SendOtpViaGmail(email, storedOtp);
            if (sent)
            {
                txtOtp.Visibility = Visibility.Visible;
                btnVerifyOtp.Visibility = Visibility.Visible;
                btnSendOtp.IsEnabled = false;
                btnSendOtp.Content = "OTP SENT ✓";
                MessageBox.Show($"✅ Check {email} for OTP (including spam)!");
            }
        }

        // ✅ VERIFY → OPEN YOUR PASSWORD WINDOW
        private async void btnVerifyOtp_Click(object sender, RoutedEventArgs e)
        {
            if (txtOtp.Password.Trim() == storedOtp)
            {
                txtSuccess.Visibility = Visibility.Visible;
                await Task.Delay(1500); // Show success message

                // ✅ OPEN YOUR EXISTING ResetPassword window
                ResetPassword passwordWindow = new ResetPassword(verifiedEmail);
                passwordWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("❌ Wrong OTP!", "Invalid");
            }
        }

        private async Task<bool> SendOtpViaGmail(string email, string otp)
        {
            try
            {
                var msg = new MimeMessage();
                msg.From.Add(new MailboxAddress("JohnCis Lodge", "johncislonge@gmail.com"));
                msg.To.Add(new MailboxAddress("", email));
                msg.Subject = "🔐 Email Verification";

                msg.Body = new TextPart("html")
                {
                    Text = $@"
                    <h3 style='color:#28AEED'>Your OTP Code:</h3>
                    <div style='background:#28AEED;color:white;font-size:36px;font-weight:bold;
                                width:120px;padding:20px;margin:20px auto;text-align:center;
                                border-radius:10px'>{otp}</div>
                    <p>Valid for 10 minutes.</p>"
                };

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync("johncislonge@gmail.com", "ptqxsovngnlzmzke");
                    await client.SendAsync(msg);
                    await client.DisconnectAsync(true);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidEmail(string email) => email.Contains("@") && email.Contains(".");
        private bool EmailExists(string email)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Staff WHERE Email=@e", conn))
                {
                    cmd.Parameters.AddWithValue("@e", email);
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }
    }
}