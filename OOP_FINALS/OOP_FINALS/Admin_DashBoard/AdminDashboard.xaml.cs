using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OOP_FINALS
{
    public partial class AdminDashboard : Window
    {
        private readonly string connectionString =
            @"Server=DESKTOP-51HVDT7;Database=JohnCis_HotelManagement_System;Trusted_Connection=True;TrustServerCertificate=True;";

        public DashboardData DashboardInfo { get; set; } = new DashboardData();

        public AdminDashboard()
        {
            InitializeComponent();
            LoadDashboardData("Monthly");

            // Safe navigation
            if (MainFrame != null)
            {
                MainFrame.Navigate(new DashboardPageAdmin(DashboardInfo));
            }
        }

        private void LoadDashboardData(string filter)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // 🔹 Total Staff
                    using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(COUNT(*), 0) FROM Staff WHERE Status='Active'", conn))
                        DashboardInfo.TotalStaff = cmd.ExecuteScalar().ToString();

                    // 🔹 Available Rooms
                    using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(COUNT(*), 0) FROM Rooms WHERE Status='Available'", conn))
                        DashboardInfo.TotalRooms = cmd.ExecuteScalar().ToString();

                    // 🔹 Total Bookings (Filtered)
                    string bookingQuery;

                    if (filter == "Daily")
                    {
                        bookingQuery = "SELECT ISNULL(COUNT(*),0) FROM Reservations WHERE CAST(CheckInDate AS DATE) = CAST(GETDATE() AS DATE)";
                    }
                    else if (filter == "Weekly")
                    {
                        bookingQuery = "SELECT ISNULL(COUNT(*),0) FROM Reservations WHERE DATEPART(WEEK, CheckInDate) = DATEPART(WEEK, GETDATE())";
                    }
                    else // Monthly
                    {
                        bookingQuery = "SELECT ISNULL(COUNT(*),0) FROM Reservations WHERE MONTH(CheckInDate) = MONTH(GETDATE())";
                    }
                    using (SqlCommand cmd = new SqlCommand(bookingQuery, conn))
                        DashboardInfo.TotalBookings = cmd.ExecuteScalar().ToString();

                    // 🔹 Today's Check-ins
                    using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(COUNT(*),0) FROM Reservations WHERE CAST(CheckInDate AS DATE) = CAST(GETDATE() AS DATE)", conn))
                        DashboardInfo.CheckIns = cmd.ExecuteScalar().ToString();

                    // 🔹 Today's Check-outs
                    using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(COUNT(*),0) FROM Reservations WHERE CAST(CheckOutDate AS DATE) = CAST(GETDATE() AS DATE)", conn))
                        DashboardInfo.CheckOuts = cmd.ExecuteScalar().ToString();

                    // 🔹 Total Revenue (Payments only)
                    using (SqlCommand cmd = new SqlCommand(@"
                SELECT ISNULL(SUM(AmountPaid),0) 
                FROM Payment
                WHERE PaymentStatus IN ('Completed','Partial')
            ", conn))
                    {
                        DashboardInfo.TotalRevenue = "₱" + Convert.ToDecimal(cmd.ExecuteScalar()).ToString("N0");
                    }

                    // 🔹 Revenue Progress (last 30 days vs previous 30 days)
                    using (SqlCommand cmd = new SqlCommand(@"
                DECLARE @CurrentRevenue DECIMAL(18,2)
                DECLARE @PrevRevenue DECIMAL(18,2)

                SELECT @CurrentRevenue = ISNULL(SUM(p.AmountPaid),0)
                FROM Payment p
                JOIN Reservations r ON p.ReservationID = r.ReservationID
                WHERE p.PaymentStatus IN ('Completed','Partial')
                  AND r.CheckInDate >= DATEADD(DAY,-30,GETDATE())

                SELECT @PrevRevenue = ISNULL(SUM(p.AmountPaid),0)
                FROM Payment p
                JOIN Reservations r ON p.ReservationID = r.ReservationID
                WHERE p.PaymentStatus IN ('Completed','Partial')
                  AND r.CheckInDate >= DATEADD(DAY,-60,GETDATE())
                  AND r.CheckInDate < DATEADD(DAY,-30,GETDATE())

                SELECT @CurrentRevenue AS CurrentRevenue, @PrevRevenue AS PrevRevenue
            ", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                decimal current = Convert.ToDecimal(reader["CurrentRevenue"]);
                                decimal previous = Convert.ToDecimal(reader["PrevRevenue"]);

                                if (previous > 0)
                                {
                                    double progress = ((double)current / (double)previous) * 100;
                                    DashboardInfo.RevenueProgress = Math.Min(100, progress);
                                    double trendPercent = ((double)current - (double)previous) / (double)previous * 100;
                                    DashboardInfo.RevenueTrend = trendPercent >= 0 ? $"+{trendPercent:F1}%" : $"{trendPercent:F1}%";
                                }
                                else
                                {
                                    DashboardInfo.RevenueProgress = 100;
                                    DashboardInfo.RevenueTrend = "+100%";
                                }
                            }
                        }
                    }

                    // 🔹 Occupancy Rate
                    using (SqlCommand cmd = new SqlCommand(@"
                SELECT CAST(ROUND(
                    CASE WHEN COUNT(*) = 0 THEN 0
                         ELSE SUM(CASE WHEN Status='Occupied' THEN 1 ELSE 0 END) * 100.0 / COUNT(*) 
                    END, 0) AS INT) AS OccupancyRate
                FROM Rooms
            ", conn))
                    {
                        var rate = cmd.ExecuteScalar();
                        DashboardInfo.OccupancyRate = rate + "%";
                        DashboardInfo.OccupancyProgress = Convert.ToDouble(rate);
                        DashboardInfo.OccupancyTrend = "+3.2%"; // Optional: calculate from past occupancy
                    }

                    // 🔹 Recent Activities (last 48 hours)
                    DashboardInfo.RecentActivities.Clear();
                    using (SqlCommand cmd = new SqlCommand(@"
                SELECT TOP 5
                    CASE
                        WHEN r.ReservationStatus IN ('Checked-In','Completed') THEN '• Guest checked into Room ' + ISNULL(rm.RoomNumber,'N/A')
                        WHEN r.ReservationStatus = 'Confirmed' THEN '• Room reserved: Reservation #' + CAST(r.ReservationID AS VARCHAR)
                        WHEN p.PaymentStatus IN ('Completed','Partial') THEN '• Payment received: ₱' + CAST(p.AmountPaid AS VARCHAR) + ' (' + p.PaymentMethod + ')'
                        WHEN bd.Description IS NOT NULL THEN '• Extra charge: ' + bd.Description + ' ₱' + CAST(bd.Subtotal AS VARCHAR)
                        ELSE '• System activity logged'
                    END AS ActivityText,
                    ISNULL(r.CheckInDate, GETDATE()) AS ActivityTime
                FROM Reservations r
                LEFT JOIN Rooms rm ON r.RoomID = rm.RoomID
                LEFT JOIN Payment p ON r.ReservationID = p.ReservationID
                LEFT JOIN Billing b ON r.ReservationID = b.ReservationID
                LEFT JOIN Billing_Details bd ON b.BillingID = bd.BillingID
                WHERE ISNULL(r.CheckInDate, GETDATE()) >= DATEADD(HOUR,-48,GETDATE())
                ORDER BY ActivityTime DESC
            ", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DashboardInfo.RecentActivities.Add(new ActivityItem
                                {
                                    ActivityText = reader["ActivityText"].ToString(),
                                    ActivityTime = Convert.ToDateTime(reader["ActivityTime"])
                                });
                            }
                        }
                        DashboardInfo.ActivityCount = DashboardInfo.RecentActivities.Count.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database Error:\n{ex.Message}\n\nStack:\n{ex.StackTrace}",
                                "Database Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadMockData(); // fallback mock
            }
        }
        private void LoadMockData()
        {
            Random rand = new Random(42);
            DashboardInfo.TotalStaff = "8";
            DashboardInfo.TotalRooms = "50";
            DashboardInfo.TotalBookings = rand.Next(10, 25).ToString();
            DashboardInfo.CheckIns = rand.Next(3, 8).ToString();
            DashboardInfo.CheckOuts = rand.Next(2, 5).ToString();
            DashboardInfo.TotalRevenue = "₱45,680";
            DashboardInfo.RevenueProgress = 85;
            DashboardInfo.RevenueTrend = "+15.2%";
            DashboardInfo.OccupancyRate = "72%";
            DashboardInfo.OccupancyProgress = 72;
            DashboardInfo.OccupancyTrend = "+4.1%";

            DashboardInfo.RecentActivities.Clear();
            DashboardInfo.RecentActivities.Add(new ActivityItem { ActivityText = "• Payment ₱3360 (Cash)" });
            DashboardInfo.RecentActivities.Add(new ActivityItem { ActivityText = "• Reservation #10 Paid" });
            DashboardInfo.RecentActivities.Add(new ActivityItem { ActivityText = "• Extra Breakfast ₱750" });
            DashboardInfo.ActivityCount = "3";
        }
        // 🔍 Search Events
        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Text == "🔍 Search...")
            {
                tb.Text = "";
                tb.Foreground = Brushes.Black;
            }
        }

        private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && string.IsNullOrWhiteSpace(tb.Text))
            {
                tb.Text = "🔍 Search...";
                tb.Foreground = Brushes.Gray;
            }
        }

        // 📅 Filter Changed
        private void cmbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFilter?.SelectedItem is ComboBoxItem item)
            {
                LoadDashboardData(item.Content.ToString());
                if (MainFrame != null)
                    MainFrame.Navigate(new DashboardPageAdmin(DashboardInfo));
            }
        }

        // 🧭 Navigation Buttons
        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            LoadDashboardData("Monthly");
            if (MainFrame != null)
                MainFrame.Navigate(new DashboardPageAdmin(DashboardInfo));
        }

        private void Bookings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainFrame != null)
                {
                    // Navigate to the BookingsPage
                    MainFrame.Navigate(new BookingsPage());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open Bookings Page:\n{ex.Message}", "Navigation Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Guests_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("👥 Guests Page\n(Coming Soon!)", "Delisas Hotel",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Dashboard_Click(sender, e);
        }

        private void Analytics_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("📊 Analytics Page\n(Coming Soon!)", "Delisas Hotel",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Dashboard_Click(sender, e);
        }

        private void Transactions_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("💳 Transactions Page\n(Coming Soon!)", "Delisas Hotel",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Dashboard_Click(sender, e);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("⚙️ Settings Page\n(Coming Soon!)", "Delisas Hotel",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Dashboard_Click(sender, e);
        }

        // 🪟 Window Controls
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        private void Housekeeping_Click(object sender, RoutedEventArgs e)
        {
          MainFrame.Navigate(new HousekeepingPage());
        }

        private void Inventory_Click(object sender, RoutedEventArgs e)
        {
          //  MainFrame.Navigate(new InventoryPage());
        }

        private void InventoryUsage_Click(object sender, RoutedEventArgs e)
        {
           // MainFrame.Navigate(new InventoryUsagePage());
        }
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}