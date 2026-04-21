using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OOP_FINALS
{
    public partial class BookingsPage : Page
    {
        private readonly string connectionString =
            @"Server=DESKTOP-51HVDT7;Database=JohnCis_HotelManagement_System;Trusted_Connection=True;TrustServerCertificate=True;";

        private List<BookingItem> allBookings = new List<BookingItem>();
        private List<BookingItem> filteredBookings = new List<BookingItem>();

        private string _placeholderText = "🔍 Search guests, rooms, phone...";
        private bool _isPlaceholderShowing = true;

        public BookingsPage()
        {
            InitializeComponent();
            LoadBookings();
            Loaded += BookingsPage_Loaded;
        }
        private async void BookingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Wait for UI to fully load
            await System.Threading.Tasks.Task.Delay(100);
            LoadBookings();
        }
        private void LoadBookings()
        {
            try
            {
                allBookings.Clear();
                filteredBookings.Clear();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                   // Bookings
                    conn.Open();

                    string query = @"
                        SELECT
                            r.ReservationID,
                            c.FirstName + ' ' + c.LastName AS GuestName,
                            r.RoomID AS RoomNumber,
                            rt.TypeName AS RoomType,
                            r.CheckInDate,
                            r.CheckOutDate,
                            DATEDIFF(day, r.CheckInDate, r.CheckOutDate) AS NightsStay,
                            b.TotalAmount,
                            r.ReservationStatus AS Status
                        FROM Reservations r
                        JOIN Customers c ON r.CustomerID = c.CustomerID
                        JOIN Rooms ro ON r.RoomID = ro.RoomID
                        JOIN RoomTypes rt ON ro.RoomTypeID = rt.RoomTypeID
                        LEFT JOIN Billing b ON r.ReservationID = b.ReservationID
                        WHERE r.ReservationStatus IS NOT NULL
                        ORDER BY r.CheckInDate DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allBookings.Add(new BookingItem
                            {
                                ReservationID = reader["ReservationID"]?.ToString() ?? "",
                                GuestName = reader["GuestName"]?.ToString() ?? "",
                                RoomNumber = reader["RoomNumber"]?.ToString() ?? "",
                                RoomType = reader["RoomType"]?.ToString() ?? "",
                                CheckInDate = reader["CheckInDate"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["CheckInDate"])
                                    : DateTime.Now,
                                CheckOutDate = reader["CheckOutDate"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["CheckOutDate"])
                                    : DateTime.Now.AddDays(1),
                                NightsStay = reader["NightsStay"] != DBNull.Value ? (int)reader["NightsStay"] : 1,
                                TotalAmount = reader["TotalAmount"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["TotalAmount"]).ToString("C", CultureInfo.CurrentCulture)
                                    : "₱0.00",
                                Status = reader["Status"]?.ToString() ?? "Unknown"
                            });
                        }
                    }
                }

                filteredBookings = new List<BookingItem>(allBookings);
                UpdateDataGrid();
                UpdateStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load bookings:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                allBookings = new List<BookingItem>();
                filteredBookings = new List<BookingItem>();
                UpdateDataGrid();
            }
        }



        private void UpdateStats()
        {
            // Double-check ALL UI elements exist
            if (txtBookingCount == null || txtTotalRevenue == null ||
                txtOccupancy == null || txtPendingPayments == null ||
                filteredBookings == null)
                return;

            try
            {
                var count = filteredBookings.Count;
                txtBookingCount.Text = $"{count} Active";

                decimal totalRevenue = 0;
                foreach (var booking in filteredBookings)
                {
                    if (decimal.TryParse(booking.TotalAmount.Replace("₱", "").Replace(",", "").Trim(), out decimal amt))
                        totalRevenue += amt;
                }

                txtTotalRevenue.Text = $"₱{totalRevenue:N2}";
                txtPendingPayments.Text = filteredBookings.Count(b => b.Status == "Confirmed").ToString();
                txtOccupancy.Text = $"{Math.Min(count * 2, 100):N0}%"; // 50 rooms assumed
            }
            catch
            {
                // Ignore stat errors
            }
        }

        private void ApplyFilters()
        {
            if (allBookings == null || !allBookings.Any())
            {
                filteredBookings = new List<BookingItem>();
                UpdateDataGrid();
                return;
            }

            filteredBookings = allBookings.Where(b =>
            {
                // Room Type Filter
                if (RoomTypeFilter?.SelectedIndex > 0)
                {
                    var selectedType = ((ComboBoxItem)RoomTypeFilter.SelectedItem)?.Content?.ToString();
                    if (!string.IsNullOrEmpty(selectedType) && b.RoomType != selectedType)
                        return false;
                }

                // Status Filter
                if (StatusFilter?.SelectedIndex > 0)
                {
                    var selectedStatus = ((ComboBoxItem)StatusFilter.SelectedItem)?.Content?.ToString();
                    if (!string.IsNullOrEmpty(selectedStatus) && b.Status != selectedStatus)
                        return false;
                }

                // Date Range Filter
                if (FromDateFilter?.SelectedDate.HasValue == true && b.CheckInDate < FromDateFilter.SelectedDate.Value)
                    return false;

                if (ToDateFilter?.SelectedDate.HasValue == true && b.CheckInDate > ToDateFilter.SelectedDate.Value)
                    return false;

                // Search Filter
                var searchText = GetSearchText();
                if (!string.IsNullOrEmpty(searchText) && !_isPlaceholderShowing)
                {
                    if (!b.ReservationID.ToLower().Contains(searchText) &&
                        !b.GuestName.ToLower().Contains(searchText) &&
                        !b.RoomNumber.ToLower().Contains(searchText) &&
                        !b.RoomType.ToLower().Contains(searchText))
                        return false;
                }

                return true;
            }).ToList() ?? new List<BookingItem>();

            UpdateDataGrid();
        }

        private string GetSearchText()
        {
            return SearchTextBox?.Text?.ToLower() ?? "";
        }

        private void UpdateDataGrid()
        {
            // Check if UI elements are loaded
            if (dgBookings == null) return;

            dgBookings.ItemsSource = null;
            dgBookings.ItemsSource = filteredBookings ?? new List<BookingItem>();

            UpdateStats();
        }

        // ========== ALL REQUIRED EVENT HANDLERS ==========

        // Search TextBox Events
        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (_isPlaceholderShowing && textBox?.Text == _placeholderText)
            {
                textBox.Text = "";
                _isPlaceholderShowing = false;
                textBox.Foreground = (SolidColorBrush)FindResource("TextPrimary");
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (string.IsNullOrWhiteSpace(textBox?.Text))
            {
                textBox.Text = _placeholderText;
                _isPlaceholderShowing = true;
                textBox.Foreground = (SolidColorBrush)FindResource("TextSecondary");
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();

        // Filter Events
        private void FilterChanged(object sender, RoutedEventArgs e) => ApplyFilters();

        // Button Events
        private void NewBookingButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Navigate to New Booking Form", "New Booking",
                MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Navigate to booking creation page
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e) => LoadBookings();

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (filteredBookings.Count == 0)
            {
                MessageBox.Show("No bookings to export.", "Export CSV",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                FileName = $"Bookings_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("ReservationID,GuestName,RoomNumber,RoomType,CheckInDate,CheckOutDate,Nights,TotalAmount,Status");

                    foreach (var b in filteredBookings)
                    {
                        sb.AppendLine($"\"{b.ReservationID}\",\"{b.GuestName}\",\"{b.RoomNumber}\",\"{b.RoomType}\",\"{b.CheckInDate:dd/MM/yyyy}\",\"{b.CheckOutDate:dd/MM/yyyy}\",\"{b.NightsStay}\",\"{b.TotalAmount}\",\"{b.Status}\"");
                    }

                    File.WriteAllText(saveFileDialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show($"✅ Exported {filteredBookings.Count} bookings!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed:\n{ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // DataGrid Events
        private void dgBookings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle selection if needed
        }

        private void SelectAllCheckbox_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            foreach (BookingItem booking in filteredBookings)
            {
                booking.IsSelected = checkBox?.IsChecked ?? false;
            }
            UpdateDataGrid();
        }

        // Action Buttons (TODO: Implement database updates)
        private void CheckIn_Click(object sender, RoutedEventArgs e)
        {
            var selected = filteredBookings.FirstOrDefault(b => b.IsSelected);
            if (selected != null)
            {
                UpdateReservationStatus(selected.ReservationID, "Checked In");
                MessageBox.Show($"✅ {selected.GuestName} checked in!", "Success");
            }
        }

        private void CheckOut_Click(object sender, RoutedEventArgs e)
        {
            var selected = filteredBookings.FirstOrDefault(b => b.IsSelected);
            if (selected != null)
            {
                UpdateReservationStatus(selected.ReservationID, "Checked Out");
                MessageBox.Show($"✅ {selected.GuestName} checked out!", "Success");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            var selected = filteredBookings.FirstOrDefault(b => b.IsSelected);
            if (selected != null)
            {
                UpdateReservationStatus(selected.ReservationID, "Cancelled");
                MessageBox.Show($"❌ Booking {selected.ReservationID} cancelled!", "Cancelled");
            }
        }

        private void UpdateReservationStatus(string reservationId, string status)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE Reservations SET ReservationStatus = @status WHERE ReservationID = @id";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@id", reservationId);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadBookings(); // Refresh data
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class BookingItem
    {
        public string ReservationID { get; set; } = "";
        public string GuestName { get; set; } = "";
        public string RoomNumber { get; set; } = "";
        public string RoomType { get; set; } = "";
        public DateTime CheckInDate { get; set; } = DateTime.Now;
        public DateTime CheckOutDate { get; set; } = DateTime.Now.AddDays(1);
        public int NightsStay { get; set; } = 1;
        public string TotalAmount { get; set; } = "₱0.00";
        public string Status { get; set; } = "Unknown";
        public bool IsSelected { get; set; }
    }
}