using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;

namespace OOP_FINALS
{
    public partial class Dashboard : Window
    {
        private readonly string connectionString =
            @"Server=DESKTOP-51HVDT7;Database=JohnCis_HotelManagement_System;Trusted_Connection=True;";

        public Dashboard()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

               // SqlCommand bookingCmd = new SqlCommand("SELECT COUNT(*) FROM Bookings", conn);
               // txtMyBookings.Text = bookingCmd.ExecuteScalar()?.ToString() ?? "0";

                SqlCommand roomCmd = new SqlCommand("SELECT COUNT(*) FROM Rooms WHERE Status='Available'", conn);
              //  txtRooms.Text = roomCmd.ExecuteScalar()?.ToString() ?? "0";
            }
        }

        // DRAG
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        // MIN
        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        // CLOSE
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // BUTTONS
        private void Bookings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Open Bookings");
        }

        private void Rooms_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Open Rooms");
        }

        private void Profile_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Open Profile");
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            this.Close();
        }
    }
}