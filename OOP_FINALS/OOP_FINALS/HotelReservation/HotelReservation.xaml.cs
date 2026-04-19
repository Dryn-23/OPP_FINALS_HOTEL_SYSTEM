using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace OOP_FINALS.HotelReservation
{
    public partial class HotelReservation : Window
    {
        // ── Connection String ──────────────────────────────────────────────
        private const string ConnStr =
            "Server=DESKTOP-51HVDT7;Database=JohnCis_HotelManagement_System;" +
            "Integrated Security=True;TrustServerCertificate=True;";

        // ── SMTP Settings ──────────────────────────────────────────────────
        private const string SmtpHost = "smtp.gmail.com";
        private const int SmtpPort = 587;
        private const string SmtpUser = "johncislonge@gmail.com";
        private const string SmtpPassword = "ptqxsovngnlzmzke";
        private const string SmtpFrom = "johncislonge@gmail.com";
        private const string HotelName = "Johncis Lodge";

        // ── State ──────────────────────────────────────────────────────────
        private DispatcherTimer _timer;
        private ObservableCollection<RoomCardViewModel> _roomCards = new ObservableCollection<RoomCardViewModel>();
        private ObservableCollection<RoomViewModel> _rooms = new ObservableCollection<RoomViewModel>();
        private ObservableCollection<BookingViewModel> _bookings = new ObservableCollection<BookingViewModel>();
        private ObservableCollection<BillingDetailItem> _billingItems = new ObservableCollection<BillingDetailItem>();

        private RoomCardViewModel _selectedRoom = null;
        private BookingViewModel _extendTarget = null;   // reservation being extended
        private int _currentStep = 1;
        private bool _initializing = true;

        // ── Constructor ────────────────────────────────────────────────────
        public HotelReservation()
        {
            InitializeComponent();
            _initializing = false;
        }

        // ── Window Loaded ──────────────────────────────────────────────────
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _initializing = true;
            StartClock();
            LoadStaff();
            LoadRoomTypeFilter();
            LoadRoomCards();
            LoadRooms();
            LoadBookings();
            RefreshDashboard();

            dpCheckIn.SelectedDate = DateTime.Today;
            dpCheckOut.SelectedDate = DateTime.Today.AddDays(1);

            lvBillingItems.ItemsSource = _billingItems;
            cmbExtSessions.SelectedIndex = 0;

            ShowPanel("newbooking");
            GoToStep(1);

            if (cmbRoomTypeFilter.Items.Count > 0)
                cmbRoomTypeFilter.SelectedIndex = 0;

            _initializing = false;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        // ══════════════════════════════════════════════════════════════════
        // PANEL NAVIGATION
        // ══════════════════════════════════════════════════════════════════
        private void ShowPanel(string panel)
        {
            panelNewBooking.Visibility = Visibility.Collapsed;
            panelBookings.Visibility = Visibility.Collapsed;
            panelRoomMap.Visibility = Visibility.Collapsed;
            panelExtendStay.Visibility = Visibility.Collapsed;

            switch (panel)
            {
                case "newbooking":
                    panelNewBooking.Visibility = Visibility.Visible;
                    lblPageTitle.Text = "New Reservation";
                    break;

                case "bookings":
                    panelBookings.Visibility = Visibility.Visible;
                    lblPageTitle.Text = "Active Reservations";
                    LoadBookings();
                    break;

                case "roommap":
                    panelRoomMap.Visibility = Visibility.Visible;
                    lblPageTitle.Text = "Room Map";
                    icRoomsLarge.ItemsSource = _rooms;
                    if (_rooms.Count == 0) LoadRooms();
                    break;

                case "extendstay":
                    panelExtendStay.Visibility = Visibility.Visible;
                    lblPageTitle.Text = "Extend Stay";
                    break;
            }
        }

        private void NavNewBooking_Click(object sender, RoutedEventArgs e) { ShowPanel("newbooking"); GoToStep(1); }
        private void NavRoomMap_Click(object sender, RoutedEventArgs e) => ShowPanel("roommap");
        private void NavBookings_Click(object sender, RoutedEventArgs e) => ShowPanel("bookings");

        // ══════════════════════════════════════════════════════════════════
        // STEP WIZARD
        // ══════════════════════════════════════════════════════════════════
        private void GoToStep(int step)
        {
            _currentStep = step;
            pageStep1.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
            pageStep2.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
            pageStep3.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;
            pageStep4.Visibility = step == 4 ? Visibility.Visible : Visibility.Collapsed;
            UpdateStepUI();
        }

        private void UpdateStepUI()
        {
            SetStep(step1Circle, step1Lbl, "🛏", "Select Room", _currentStep == 1, _currentStep > 1);
            SetStep(step2Circle, step2Lbl, "👤", "Guest Info", _currentStep == 2, _currentStep > 2);
            SetStep(step3Circle, step3Lbl, "💳", "Payment", _currentStep == 3, _currentStep > 3);
            SetStep(step4Circle, step4Lbl, "✅", "Confirmation", _currentStep == 4, false);

            if (step2Icon != null) step2Icon.Opacity = _currentStep >= 2 ? 1.0 : 0.4;
            if (step3Icon != null) step3Icon.Opacity = _currentStep >= 3 ? 1.0 : 0.4;
            if (step4Icon != null) step4Icon.Opacity = _currentStep >= 4 ? 1.0 : 0.4;
        }

        private void SetStep(Border circle, TextBlock label, string icon, string text, bool active, bool done)
        {
            if (active)
            {
                circle.Background = new SolidColorBrush(Color.FromRgb(0xC9, 0x92, 0x2A));
                circle.BorderBrush = Brushes.Transparent;
                circle.BorderThickness = new Thickness(0);
                label.Foreground = new SolidColorBrush(Color.FromRgb(0xC9, 0x92, 0x2A));
            }
            else if (done)
            {
                circle.Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x22, 0x08));
                circle.BorderBrush = new SolidColorBrush(Color.FromRgb(0x6B, 0x4A, 0x10));
                circle.BorderThickness = new Thickness(1);
                label.Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x4A, 0x10));
            }
            else
            {
                circle.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x1A, 0x0E));
                circle.BorderBrush = new SolidColorBrush(Color.FromRgb(0x3D, 0x28, 0x10));
                circle.BorderThickness = new Thickness(1);
                label.Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x30, 0x10));
            }
            label.Text = text;
        }

        // ── Step Buttons ───────────────────────────────────────────────────
        private void BtnStep1Next_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRoom == null)
            { Warn("Please select a room first."); return; }
            if (dpCheckIn.SelectedDate == null || dpCheckOut.SelectedDate == null)
            { Warn("Please select valid check-in and check-out dates."); return; }
            if (dpCheckOut.SelectedDate.Value <= dpCheckIn.SelectedDate.Value)
            { Warn("Check-out date must be after check-in date."); return; }

            lblCheckInDisplay.Text = dpCheckIn.SelectedDate.Value.ToString("MMMM dd, yyyy");
            lblCheckOutDisplay.Text = dpCheckOut.SelectedDate.Value.ToString("MMMM dd, yyyy");
            GoToStep(2);
        }

        private void BtnStep2Back_Click(object sender, RoutedEventArgs e) => GoToStep(1);

        private void BtnStep2Next_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFirstName.Text)) { Warn("First name is required."); return; }
            if (string.IsNullOrWhiteSpace(txtLastName.Text)) { Warn("Last name is required."); return; }
            if (string.IsNullOrWhiteSpace(txtContactNumber.Text)) { Warn("Contact number is required."); return; }
            if (string.IsNullOrWhiteSpace(txtEmail.Text)) { Warn("Email address is required."); return; }
            if (cmbIDType.SelectedItem == null) { Warn("Please select a valid ID type."); return; }
            if (string.IsNullOrWhiteSpace(txtIDNumber.Text)) { Warn("ID number is required."); return; }
            if (cmbGuests.SelectedItem == null) { Warn("Please select number of guests."); return; }
            if (cmbStaff.SelectedItem == null) { Warn("Please select a staff member."); return; }

            UpdateBillingSummary();
            GoToStep(3);
        }

        private void BtnStep3Back_Click(object sender, RoutedEventArgs e) => GoToStep(2);

        // ══════════════════════════════════════════════════════════════════
        // CLOCK
        // ══════════════════════════════════════════════════════════════════
        private void StartClock()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, _) => lblClock.Text = DateTime.Now.ToString("ddd, MMM dd yyyy  HH:mm:ss");
            _timer.Start();
            lblClock.Text = DateTime.Now.ToString("ddd, MMM dd yyyy  HH:mm:ss");
        }

        // ══════════════════════════════════════════════════════════════════
        // DB HELPERS
        // ══════════════════════════════════════════════════════════════════
        private SqlConnection OpenConnection()
        {
            var conn = new SqlConnection(ConnStr);
            conn.Open();
            return conn;
        }

        private static object Scalar(SqlConnection conn, string sql, SqlTransaction tx = null)
        {
            using (var cmd = tx != null
                ? new SqlCommand(sql, conn, tx)
                : new SqlCommand(sql, conn))
                return cmd.ExecuteScalar() ?? 0;
        }

        // ══════════════════════════════════════════════════════════════════
        // LOAD – ROOM TYPE FILTER
        // ══════════════════════════════════════════════════════════════════
        private void LoadRoomTypeFilter()
        {
            SqlConnection conn = null; SqlDataReader dr = null;
            try
            {
                conn = OpenConnection();
                dr = new SqlCommand("SELECT TypeName FROM RoomTypes ORDER BY TypeName", conn).ExecuteReader();
                while (dr.Read())
                    cmbRoomTypeFilter.Items.Add(new ComboBoxItem { Content = dr["TypeName"].ToString() });
            }
            catch { }
            finally { dr?.Close(); conn?.Close(); }
        }

        // ══════════════════════════════════════════════════════════════════
        // LOAD – ROOM CARDS
        // PricePerNight column = price per 3-hour session
        // ══════════════════════════════════════════════════════════════════
        private void LoadRoomCards(string typeFilter = "")
        {
            _roomCards.Clear();
            _selectedRoom = null;
            if (selectedRoomBar != null) selectedRoomBar.Visibility = Visibility.Collapsed;

            SqlConnection conn = null; SqlDataReader dr = null;
            try
            {
                conn = OpenConnection();
                string sql = @"
                    SELECT r.RoomID, r.RoomNumber, r.Status,
                           rt.TypeName, rt.Description, rt.PricePerNight
                    FROM   Rooms r
                    JOIN   RoomTypes rt ON rt.RoomTypeID = r.RoomTypeID";

                if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "All Types")
                    sql += " WHERE rt.TypeName = @tf";

                sql += " ORDER BY r.RoomNumber";

                var cmd = new SqlCommand(sql, conn);
                if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "All Types")
                    cmd.Parameters.AddWithValue("@tf", typeFilter);

                dr = cmd.ExecuteReader();
                int total = 0, avail = 0;

                while (dr.Read())
                {
                    total++;
                    string status = dr["Status"].ToString();
                    bool isAvailable = status == "Available";
                    if (isAvailable) avail++;

                    var card = new RoomCardViewModel
                    {
                        RoomID = (int)dr["RoomID"],
                        RoomNumber = dr["RoomNumber"].ToString(),
                        TypeName = dr["TypeName"].ToString(),
                        Description = dr["Description"] != DBNull.Value ? dr["Description"].ToString() : "",
                        PricePerNight = Convert.ToDecimal(dr["PricePerNight"]),
                        Status = status,
                        IsAvailable = isAvailable,
                        StatusColor = GetStatusColor(status),
                        StatusBadgeBg = GetStatusBadgeBg(status),
                        BackgroundColor = new SolidColorBrush(isAvailable
                            ? Color.FromArgb(255, 38, 24, 14)
                            : Color.FromArgb(255, 30, 18, 14)),
                        BorderColor = GetStatusColor(status),
                        SelectHint = isAvailable ? "Click to select ↓" : "Not available",
                        HintColor = isAvailable
                            ? new SolidColorBrush(Color.FromRgb(0xC9, 0x92, 0x2A))
                            : new SolidColorBrush(Color.FromRgb(0x4A, 0x38, 0x28)),
                        BookBtnVisible = isAvailable ? Visibility.Visible : Visibility.Collapsed,
                        HintVisible = isAvailable ? Visibility.Collapsed : Visibility.Visible
                    };

                    var captured = card;
                    card.SelectCommand = new RelayCommand(
                        o => OnRoomCardClicked(captured),
                        o => isAvailable);

                    _roomCards.Add(card);
                }

                if (icRoomCards != null) icRoomCards.ItemsSource = _roomCards;
                if (lblRoomStatus != null)
                    lblRoomStatus.Text = string.Format("Showing {0} room(s)  ·  {1} available", total, avail);
            }
            catch (Exception ex) { ShowDbError("LoadRoomCards", ex); }
            finally { dr?.Close(); conn?.Close(); }
        }

        private void OnRoomCardClicked(RoomCardViewModel card)
        {
            if (!card.IsAvailable) return;

            // Deselect previous
            if (_selectedRoom != null)
            {
                _selectedRoom.BorderColor = GetStatusColor(_selectedRoom.Status);
                _selectedRoom.BackgroundColor = new SolidColorBrush(Color.FromArgb(255, 38, 24, 14));
                _selectedRoom.SelectHint = "Click to select ↓";
                _selectedRoom.HintColor = new SolidColorBrush(Color.FromRgb(0xC9, 0x92, 0x2A));
                _selectedRoom.BookBtnVisible = Visibility.Visible;
                _selectedRoom.IsSelected = false;
            }

            _selectedRoom = card;
            card.BorderColor = new SolidColorBrush(Color.FromRgb(0xC9, 0x92, 0x2A));
            card.BackgroundColor = new SolidColorBrush(Color.FromArgb(255, 45, 28, 10));
            card.SelectHint = "✔  Selected";
            card.HintColor = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x74));
            card.BookBtnVisible = Visibility.Collapsed;
            card.HintVisible = Visibility.Visible;
            card.IsSelected = true;

            // CHANGED label: /3 hrs
            lblSelectedRoomSummary.Text = string.Format("Room {0}  ·  {1}  ·  ₱{2:N0}/3 hrs",
                card.RoomNumber, card.TypeName, card.PricePerNight);
            selectedRoomBar.Visibility = Visibility.Visible;

            UpdateBillingSummary();
        }

        private void BtnDeselect_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRoom != null)
            {
                _selectedRoom.BorderColor = GetStatusColor(_selectedRoom.Status);
                _selectedRoom.BackgroundColor = new SolidColorBrush(Color.FromArgb(255, 38, 24, 14));
                _selectedRoom.SelectHint = "Click to select ↓";
                _selectedRoom.HintColor = new SolidColorBrush(Color.FromRgb(0xC9, 0x92, 0x2A));
                _selectedRoom.BookBtnVisible = Visibility.Visible;
                _selectedRoom.IsSelected = false;
                _selectedRoom = null;
            }
            if (selectedRoomBar != null) selectedRoomBar.Visibility = Visibility.Collapsed;
        }

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e) => LoadRoomCards(GetTypeFilter());
        private void RoomTypeFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_initializing) return;
            LoadRoomCards(GetTypeFilter());
        }

        private string GetTypeFilter()
        {
            var item = cmbRoomTypeFilter.SelectedItem as ComboBoxItem;
            return item?.Content?.ToString() ?? "All Types";
        }

        // ══════════════════════════════════════════════════════════════════
        // LOAD – ROOM MAP
        // ══════════════════════════════════════════════════════════════════
        private void LoadRooms()
        {
            _rooms.Clear();
            SqlConnection conn = null; SqlDataReader dr = null;
            try
            {
                conn = OpenConnection();
                dr = new SqlCommand(@"
                    SELECT r.RoomID, r.RoomNumber, r.Status,
                           rt.TypeName, rt.PricePerNight
                    FROM   Rooms r
                    JOIN   RoomTypes rt ON rt.RoomTypeID = r.RoomTypeID
                    ORDER  BY r.RoomNumber", conn).ExecuteReader();

                while (dr.Read())
                {
                    string status = dr["Status"].ToString();
                    _rooms.Add(new RoomViewModel
                    {
                        RoomID = (int)dr["RoomID"],
                        RoomNumber = dr["RoomNumber"].ToString(),
                        TypeName = dr["TypeName"].ToString(),
                        PricePerNight = Convert.ToDecimal(dr["PricePerNight"]),
                        Status = status,
                        StatusColor = GetStatusColor(status),
                        StatusBadgeBg = GetStatusBadgeBg(status),
                        BackgroundColor = new SolidColorBrush(Color.FromArgb(255, 38, 24, 14)),
                        BorderColor = GetStatusColor(status)
                    });
                }
                icRoomsLarge.ItemsSource = _rooms;
            }
            catch (Exception ex) { ShowDbError("LoadRooms", ex); }
            finally { dr?.Close(); conn?.Close(); }
        }

        // ══════════════════════════════════════════════════════════════════
        // LOAD – STAFF
        // ══════════════════════════════════════════════════════════════════
        private void LoadStaff()
        {
            cmbStaff.Items.Clear();
            SqlConnection conn = null; SqlDataReader dr = null;
            try
            {
                conn = OpenConnection();
                dr = new SqlCommand(@"
                    SELECT s.StaffID, s.FullName, r.RoleName
                    FROM   Staff s JOIN Roles r ON r.RoleID = s.RoleID
                    WHERE  s.Status = 'Active' ORDER BY s.FullName", conn).ExecuteReader();

                while (dr.Read())
                    cmbStaff.Items.Add(new ComboBoxItem
                    {
                        Content = string.Format("{0}  ({1})", dr["FullName"], dr["RoleName"]),
                        Tag = dr["StaffID"].ToString()
                    });

                if (cmbStaff.Items.Count > 0)
                {
                    cmbStaff.SelectedIndex = 0;
                    var item = (ComboBoxItem)cmbStaff.SelectedItem;
                    string cnt = item.Content?.ToString() ?? "";
                    lblStaffName.Text = cnt.Contains("(") ? cnt.Split('(')[0].Trim() : cnt;
                    lblStaffRole.Text = cnt.Contains("(") ? cnt.Split('(')[1].TrimEnd(')') : "Staff";
                }
            }
            catch (Exception ex) { ShowDbError("LoadStaff", ex); }
            finally { dr?.Close(); conn?.Close(); }
        }

        // ══════════════════════════════════════════════════════════════════
        // LOAD – BOOKINGS
        // ══════════════════════════════════════════════════════════════════
        private void LoadBookings(string filter = "")
        {
            _bookings.Clear();
            SqlConnection conn = null; SqlDataReader dr = null;
            try
            {
                conn = OpenConnection();
                string sql = @"
                    SELECT r.ReservationID,
                           c.FirstName + ' ' + c.LastName AS GuestName,
                           rm.RoomNumber,
                           rt.TypeName                     AS RoomType,
                           r.CheckInDate,
                           r.CheckOutDate,
                           r.NumberOfGuest,
                           b.FinalAmount,
                           r.ReservationStatus,
                           p.PaymentStatus,
                           p.PaymentDate
                    FROM   Reservations r
                    JOIN   Customers  c  ON c.CustomerID    = r.CustomerID
                    JOIN   Rooms      rm ON rm.RoomID        = r.RoomID
                    JOIN   RoomTypes  rt ON rt.RoomTypeID    = rm.RoomTypeID
                    LEFT JOIN Billing b  ON b.ReservationID  = r.ReservationID
                    LEFT JOIN Payment p  ON p.ReservationID  = r.ReservationID
                    WHERE  r.ReservationStatus <> 'Cancelled'";

                if (!string.IsNullOrWhiteSpace(filter))
                    sql += @" AND (c.FirstName + ' ' + c.LastName LIKE @f
                               OR  CAST(r.ReservationID AS VARCHAR) LIKE @f
                               OR  rm.RoomNumber LIKE @f)";

                sql += " ORDER BY r.ReservationID DESC";

                var cmd = new SqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(filter))
                    cmd.Parameters.AddWithValue("@f", string.Format("%{0}%", filter));

                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    string payDate = "—";
                    if (dr["PaymentDate"] != DBNull.Value)
                        payDate = Convert.ToDateTime(dr["PaymentDate"]).ToString("MM/dd/yyyy HH:mm");

                    _bookings.Add(new BookingViewModel
                    {
                        ReservationID = (int)dr["ReservationID"],
                        GuestName = dr["GuestName"].ToString(),
                        RoomNumber = dr["RoomNumber"].ToString(),
                        RoomType = dr["RoomType"].ToString(),
                        CheckIn = Convert.ToDateTime(dr["CheckInDate"]).ToString("MM/dd/yyyy"),
                        CheckOut = Convert.ToDateTime(dr["CheckOutDate"]).ToString("MM/dd/yyyy"),
                        Guests = dr["NumberOfGuest"].ToString(),
                        Total = dr["FinalAmount"] == DBNull.Value
                                          ? "—"
                                          : string.Format("₱{0:N2}", Convert.ToDecimal(dr["FinalAmount"])),
                        Status = dr["ReservationStatus"].ToString(),
                        PaymentStatus = dr["PaymentStatus"] == DBNull.Value ? "Unpaid" : dr["PaymentStatus"].ToString(),
                        PaymentDate = payDate,
                        // Store raw checkout for extension use
                        RawCheckOut = Convert.ToDateTime(dr["CheckOutDate"])
                    });
                }
                lvBookings.ItemsSource = _bookings;
            }
            catch (Exception ex) { ShowDbError("LoadBookings", ex); }
            finally { dr?.Close(); conn?.Close(); }
        }

        // ══════════════════════════════════════════════════════════════════
        // DASHBOARD
        // ══════════════════════════════════════════════════════════════════
        private void RefreshDashboard()
        {
            SqlConnection conn = null;
            try
            {
                conn = OpenConnection();
                lblOccupied.Text = Scalar(conn, "SELECT COUNT(*) FROM Rooms WHERE Status='Occupied'").ToString();
                lblAvailable.Text = Scalar(conn, "SELECT COUNT(*) FROM Rooms WHERE Status='Available'").ToString();
                lblArrivals.Text = Scalar(conn, @"
                    SELECT COUNT(*) FROM Reservations
                    WHERE CheckInDate = CAST(GETDATE() AS DATE)
                    AND   ReservationStatus = 'Confirmed'").ToString();

                decimal rev = Convert.ToDecimal(Scalar(conn, @"
                    SELECT ISNULL(SUM(p.AmountPaid),0)
                    FROM   Payment p
                    JOIN   Reservations r ON r.ReservationID = p.ReservationID
                    WHERE  CAST(r.ReservationDate AS DATE) = CAST(GETDATE() AS DATE)
                    AND    p.PaymentStatus = 'Paid'"));

                lblRevenue.Text = string.Format("₱{0:N0}", rev);
            }
            catch (Exception ex) { ShowDbError("RefreshDashboard", ex); }
            finally { conn?.Close(); }
        }

        // ══════════════════════════════════════════════════════════════════
        // BILLING SUMMARY
        // Each "session" = 3 hrs. PricePerNight = rate per session.
        // Sessions = day difference between check-in and check-out.
        // ══════════════════════════════════════════════════════════════════
        private void UpdateBillingSummary()
        {
            if (_selectedRoom == null || lblTotal == null) return;

            decimal rate = _selectedRoom.PricePerNight;
            int sessions = 0;

            if (dpCheckIn.SelectedDate.HasValue && dpCheckOut.SelectedDate.HasValue)
                sessions = Math.Max(0, (dpCheckOut.SelectedDate.Value - dpCheckIn.SelectedDate.Value).Days);

            decimal subtotal = rate * sessions;
            decimal tax = subtotal * 0.12m;

            decimal discount = 0;
            if (txtDiscount != null) decimal.TryParse(txtDiscount.Text, out discount);
            discount = Math.Max(0, discount);

            decimal total = Math.Max(0, subtotal + tax - discount);

            summRoomNumber.Text = string.Format("Room {0}", _selectedRoom.RoomNumber);
            summRoomType.Text = _selectedRoom.TypeName;
            summDates.Text = "";

            if (dpCheckIn.SelectedDate.HasValue && dpCheckOut.SelectedDate.HasValue)
                summDates.Text = string.Format("{0:MMM dd, yyyy}  →  {1:MMM dd, yyyy}",
                    dpCheckIn.SelectedDate.Value, dpCheckOut.SelectedDate.Value);

            // CHANGED labels: Rate/3 hrs, Sessions
            lblRoomRate.Text = string.Format("₱{0:N2}", rate);
            lblNights.Text = sessions.ToString();
            lblSubtotal.Text = string.Format("₱{0:N2}", subtotal);
            lblTax.Text = string.Format("₱{0:N2}", tax);
            lblDiscount.Text = string.Format("-₱{0:N2}", discount);
            lblTotal.Text = string.Format("₱{0:N2}", total);

            UpdatePaymentStatus(total);
        }

        private void Discount_Changed(object sender, TextChangedEventArgs e) => UpdateBillingSummary();
        private void AmountPaid_Changed(object sender, TextChangedEventArgs e)
        {
            if (lblTotal == null) return;
            string raw = lblTotal.Text.Replace("₱", "").Replace(",", "");
            if (decimal.TryParse(raw, out decimal total)) UpdatePaymentStatus(total);
        }

        private void UpdatePaymentStatus(decimal total)
        {
            if (txtAmountPaid == null || lblPaymentStatus == null) return;

            decimal paid = 0;
            decimal.TryParse(txtAmountPaid.Text, out paid);

            if (paid >= total && total > 0)
            {
                lblPaymentStatus.Text = "✔  Fully Paid";
                lblPaymentStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x74));
            }
            else if (paid > 0)
            {
                lblPaymentStatus.Text = string.Format("⚠  Partial — Balance: ₱{0:N2}", total - paid);
                lblPaymentStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0x90, 0x40));
            }
            else
            {
                lblPaymentStatus.Text = "— Enter amount above";
                lblPaymentStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x30, 0x10));
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // ADD BILLING ITEM
        // ══════════════════════════════════════════════════════════════════
        private void AddBillingItem_Click(object sender, RoutedEventArgs e)
        {
            string desc = txtItemDesc.Text.Trim();
            int qty = 0;
            decimal price = 0;

            if (string.IsNullOrWhiteSpace(desc)) { Warn("Enter a description."); return; }
            if (!int.TryParse(txtItemQty.Text.Trim(), out qty) || qty <= 0) { Warn("Enter a valid quantity."); return; }
            if (!decimal.TryParse(txtItemPrice.Text.Trim(), out price) || price < 0) { Warn("Enter a valid unit price."); return; }

            _billingItems.Add(new BillingDetailItem
            {
                Description = desc,
                Quantity = qty,
                RawQty = qty,
                UnitPrice = string.Format("₱{0:N2}", price),
                Subtotal = string.Format("₱{0:N2}", qty * price),
                RawUnitPrice = price
            });

            txtItemDesc.Text = txtItemQty.Text = txtItemPrice.Text = "";
        }

        // ══════════════════════════════════════════════════════════════════
        // CONFIRM BOOKING
        // ══════════════════════════════════════════════════════════════════
        private void BtnBook_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPaymentMethod.SelectedItem == null)
            { Warn("Please select a payment method."); return; }

            decimal amountPaid = 0;
            if (!string.IsNullOrWhiteSpace(txtAmountPaid.Text) &&
                !decimal.TryParse(txtAmountPaid.Text, out amountPaid))
            { Warn("Amount paid must be a valid number."); return; }

            decimal discount = 0;
            if (!string.IsNullOrWhiteSpace(txtDiscount.Text))
                decimal.TryParse(txtDiscount.Text, out discount);
            discount = Math.Max(0, discount);

            int roomId = _selectedRoom.RoomID;
            int staffId = GetCurrentStaffId();
            int guests = int.Parse(((ComboBoxItem)cmbGuests.SelectedItem).Content.ToString());
            DateTime checkIn = dpCheckIn.SelectedDate.Value;
            DateTime checkOut = dpCheckOut.SelectedDate.Value;
            int sessions = (checkOut - checkIn).Days;
            string idType = ((ComboBoxItem)cmbIDType.SelectedItem).Content.ToString();
            string payMethod = ((ComboBoxItem)cmbPaymentMethod.SelectedItem).Content.ToString();
            decimal rate = _selectedRoom.PricePerNight;
            decimal subtotal = rate * sessions;
            decimal tax = subtotal * 0.12m;
            decimal total = Math.Max(0, subtotal + tax - discount);
            string payStatus = amountPaid >= total ? "Paid" : amountPaid > 0 ? "Partial" : "Unpaid";
            DateTime payDate = DateTime.Now;

            SqlConnection conn = null;
            SqlTransaction tx = null;
            try
            {
                conn = OpenConnection();

                // Pre-check availability
                using (var chk = new SqlCommand("SELECT Status FROM Rooms WHERE RoomID = @rid", conn))
                {
                    chk.Parameters.AddWithValue("@rid", roomId);
                    var st = chk.ExecuteScalar();
                    if (st == null || st.ToString() != "Available")
                    {
                        Warn(string.Format("Room {0} is no longer available ({1}). Please select another room.",
                            _selectedRoom.RoomNumber, st));
                        LoadRoomCards(GetTypeFilter());
                        GoToStep(1);
                        return;
                    }
                }

                tx = conn.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

                // 1. Upsert Customer
                int customerId;
                using (var cmdChk = new SqlCommand(
                    "SELECT CustomerID FROM Customers WHERE ContactNumber = @contact", conn, tx))
                {
                    cmdChk.Parameters.AddWithValue("@contact", txtContactNumber.Text.Trim());
                    var obj = cmdChk.ExecuteScalar();

                    if (obj != null && obj != DBNull.Value)
                    {
                        customerId = Convert.ToInt32(obj);
                        using (var u = new SqlCommand(@"
                            UPDATE Customers
                            SET FirstName=@fn,LastName=@ln,Email=@em,
                                ValidIDType=@idt,ValidIDNumber=@idn
                            WHERE CustomerID=@cid", conn, tx))
                        {
                            u.Parameters.AddWithValue("@fn", txtFirstName.Text.Trim());
                            u.Parameters.AddWithValue("@ln", txtLastName.Text.Trim());
                            u.Parameters.AddWithValue("@em", txtEmail.Text.Trim());
                            u.Parameters.AddWithValue("@idt", idType);
                            u.Parameters.AddWithValue("@idn", txtIDNumber.Text.Trim());
                            u.Parameters.AddWithValue("@cid", customerId);
                            u.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (var ins = new SqlCommand(@"
                            INSERT INTO Customers
                                   (FirstName,LastName,ContactNumber,Email,ValidIDType,ValidIDNumber)
                            OUTPUT INSERTED.CustomerID
                            VALUES (@fn,@ln,@contact,@em,@idt,@idn)", conn, tx))
                        {
                            ins.Parameters.AddWithValue("@fn", txtFirstName.Text.Trim());
                            ins.Parameters.AddWithValue("@ln", txtLastName.Text.Trim());
                            ins.Parameters.AddWithValue("@contact", txtContactNumber.Text.Trim());
                            ins.Parameters.AddWithValue("@em", txtEmail.Text.Trim());
                            ins.Parameters.AddWithValue("@idt", idType);
                            ins.Parameters.AddWithValue("@idn", txtIDNumber.Text.Trim());
                            customerId = (int)ins.ExecuteScalar();
                        }
                    }
                }

                // 2. Insert Reservation
                int reservationId;
                using (var cmdRes = new SqlCommand(@"
                    INSERT INTO Reservations
                           (CustomerID,RoomID,CheckInDate,CheckOutDate,
                            NumberOfGuest,ReservationDate,ReservationStatus,CreatedBy)
                    OUTPUT INSERTED.ReservationID
                    VALUES (@cid,@rid,@ci,@co,@ng,GETDATE(),'Confirmed',@sid)", conn, tx))
                {
                    cmdRes.Parameters.AddWithValue("@cid", customerId);
                    cmdRes.Parameters.AddWithValue("@rid", roomId);
                    cmdRes.Parameters.AddWithValue("@ci", checkIn);
                    cmdRes.Parameters.AddWithValue("@co", checkOut);
                    cmdRes.Parameters.AddWithValue("@ng", guests);
                    cmdRes.Parameters.AddWithValue("@sid", staffId);
                    reservationId = (int)cmdRes.ExecuteScalar();
                }

                // 3. Insert Billing
                int billingId;
                using (var cmdBill = new SqlCommand(@"
                    INSERT INTO Billing
                           (ReservationID,TotalAmount,TaxAmount,DiscountAmount,FinalAmount,BillingStatus)
                    OUTPUT INSERTED.BillingID
                    VALUES (@rid,@sub,@tax,@disc,@total,'Active')", conn, tx))
                {
                    cmdBill.Parameters.AddWithValue("@rid", reservationId);
                    cmdBill.Parameters.AddWithValue("@sub", subtotal);
                    cmdBill.Parameters.AddWithValue("@tax", tax);
                    cmdBill.Parameters.AddWithValue("@disc", discount);
                    cmdBill.Parameters.AddWithValue("@total", total);
                    billingId = (int)cmdBill.ExecuteScalar();
                }

                // 4. Billing Details
                InsertBillingDetail(conn, tx, billingId, "Room Charge (3-hr session)", sessions, rate);
                foreach (var item in _billingItems)
                    InsertBillingDetail(conn, tx, billingId, item.Description, item.RawQty, item.RawUnitPrice);

                // 5. Payment
                using (var cmdPay = new SqlCommand(@"
                    INSERT INTO Payment
                           (ReservationID,AmountPaid,PaymentMethod,
                            PaymentReferenceNumber,PaymentStatus,PaymentDate)
                    VALUES (@rid,@amt,@meth,@ref,@pst,@pdt)", conn, tx))
                {
                    cmdPay.Parameters.AddWithValue("@rid", reservationId);
                    cmdPay.Parameters.AddWithValue("@amt", amountPaid);
                    cmdPay.Parameters.AddWithValue("@meth", payMethod);
                    cmdPay.Parameters.AddWithValue("@ref",
                        string.IsNullOrWhiteSpace(txtPaymentRef.Text)
                            ? (object)DBNull.Value
                            : txtPaymentRef.Text.Trim());
                    cmdPay.Parameters.AddWithValue("@pst", payStatus);
                    cmdPay.Parameters.AddWithValue("@pdt",
                        amountPaid > 0 ? (object)payDate : DBNull.Value);
                    cmdPay.ExecuteNonQuery();
                }

                // 6. Mark Occupied
                new SqlCommand("UPDATE Rooms SET Status='Occupied' WHERE RoomID=@rid", conn, tx)
                    .WithParam("@rid", roomId).ExecuteNonQuery();

                tx.Commit();

                string guestEmail = txtEmail.Text.Trim();
                string guestName = string.Format("{0} {1}", txtFirstName.Text.Trim(), txtLastName.Text.Trim());
                string roomLabel = string.Format("Room {0} — {1}", _selectedRoom.RoomNumber, _selectedRoom.TypeName);

                TrySendConfirmationEmail(guestEmail, guestName, reservationId, roomLabel,
                    checkIn, checkOut, sessions, total, amountPaid, payStatus, payDate);

                PopulateReceipt(reservationId, guestName, guestEmail, roomLabel,
                    checkIn, checkOut, sessions, total, amountPaid, payStatus, payDate);

                LoadRoomCards(GetTypeFilter());
                LoadRooms();
                LoadBookings();
                RefreshDashboard();
                _billingItems.Clear();
                GoToStep(4);
            }
            catch (Exception ex)
            {
                if (tx != null) try { tx.Rollback(); } catch { }
                ShowDbError("BtnBook_Click — Transaction rolled back.", ex);
            }
            finally { tx?.Dispose(); conn?.Close(); }
        }

        private static void InsertBillingDetail(SqlConnection conn, SqlTransaction tx,
            int billingId, string description, int qty, decimal unitPrice)
        {
            using (var cmd = new SqlCommand(@"
                INSERT INTO Billing_Details (BillingID,Description,Quantity,UnitPrice,Subtotal)
                VALUES (@bid,@desc,@qty,@up,@sub)", conn, tx))
            {
                cmd.Parameters.AddWithValue("@bid", billingId);
                cmd.Parameters.AddWithValue("@desc", description);
                cmd.Parameters.AddWithValue("@qty", qty);
                cmd.Parameters.AddWithValue("@up", unitPrice);
                cmd.Parameters.AddWithValue("@sub", qty * unitPrice);
                cmd.ExecuteNonQuery();
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // RECEIPT (Step 4)
        // ══════════════════════════════════════════════════════════════════
        private void PopulateReceipt(int resId, string guest, string email, string room,
            DateTime checkIn, DateTime checkOut, int sessions, decimal total,
            decimal amountPaid, string payStatus, DateTime paymentDate)
        {
            rcptResID.Text = string.Format("#RES-{0:D5}", resId);
            rcptGuest.Text = guest;
            rcptEmail.Text = email;
            rcptRoom.Text = room;
            rcptCheckIn.Text = checkIn.ToString("MMMM dd, yyyy");
            rcptCheckOut.Text = checkOut.ToString("MMMM dd, yyyy");
            rcptNights.Text = sessions.ToString();
            rcptTotal.Text = string.Format("₱{0:N2}", total);
            rcptAmountPaid.Text = string.Format("₱{0:N2}", amountPaid);
            rcptPayStatus.Text = payStatus;

            if (rcptPaymentDate != null)
                rcptPaymentDate.Text = amountPaid > 0
                    ? paymentDate.ToString("MMMM dd, yyyy  HH:mm:ss")
                    : "—";

            rcptPayStatus.Foreground = payStatus == "Paid"
                ? new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x74))
                : payStatus == "Partial"
                    ? new SolidColorBrush(Color.FromRgb(0xE0, 0x90, 0x40))
                    : new SolidColorBrush(Color.FromRgb(0xE0, 0x55, 0x55));

            lblConfirmSubtitle.Text = string.Format("A confirmation email has been sent to {0}", email);
        }

        private void BtnViewBookings_Click(object sender, RoutedEventArgs e) => ShowPanel("bookings");
        private void BtnNewBookingFromConfirm_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            ShowPanel("newbooking");
            GoToStep(1);
        }

        // ══════════════════════════════════════════════════════════════════
        // EXTEND STAY  — full dedicated panel
        // ══════════════════════════════════════════════════════════════════
        private void BtnExtendStay_Click(object sender, RoutedEventArgs e)
        {
            // If a booking is already selected in the list, go straight to the form
            var sel = lvBookings.SelectedItem as BookingViewModel;
            if (sel != null && sel.Status == "Confirmed")
            {
                LoadExtendPanel(sel);
                return;
            }

            // Otherwise open bookings first so the user can pick one
            ShowPanel("bookings");
            Warn("Select a Confirmed reservation from the list below, then click '📅 Extend Stay' again.");
        }

        private void LoadExtendPanel(BookingViewModel bvm)
        {
            _extendTarget = bvm;

            // Populate current booking info labels
            extResID.Text = string.Format("#RES-{0:D5}", bvm.ReservationID);
            extGuestName.Text = bvm.GuestName;
            extRoomInfo.Text = string.Format("Room {0}  ·  {1}", bvm.RoomNumber, bvm.RoomType);
            extCurrentCheckOut.Text = bvm.RawCheckOut.ToString("MMMM dd, yyyy");

            // Reset extension form
            cmbExtSessions.SelectedIndex = 0;
            cmbExtPayMethod.SelectedIndex = 0;
            txtExtPayRef.Text = "";
            txtExtDiscount.Text = "0";
            txtExtAmountPaid.Text = "";
            lblExtPayStatus.Text = "— Enter amount above";
            lblExtPayStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x30, 0x10));

            // Fetch rate from DB
            LoadExtendRate(bvm.ReservationID);

            ShowPanel("extendstay");
        }

        private decimal _extRatePerSession = 0;

        private void LoadExtendRate(int reservationId)
        {
            SqlConnection conn = null;
            try
            {
                conn = OpenConnection();
                using (var cmd = new SqlCommand(@"
                    SELECT rt.PricePerNight
                    FROM   Reservations r
                    JOIN   Rooms rm     ON rm.RoomID     = r.RoomID
                    JOIN   RoomTypes rt ON rt.RoomTypeID = rm.RoomTypeID
                    WHERE  r.ReservationID = @rid", conn))
                {
                    cmd.Parameters.AddWithValue("@rid", reservationId);
                    var obj = cmd.ExecuteScalar();
                    _extRatePerSession = obj != null && obj != DBNull.Value
                        ? Convert.ToDecimal(obj) : 0;
                }
            }
            catch { _extRatePerSession = 0; }
            finally { conn?.Close(); }

            RefreshExtensionSummary();
        }

        private int GetExtSessions()
        {
            // ComboBox items: "1  (3 hrs)", "2  (6 hrs)", ...
            var item = cmbExtSessions.SelectedItem as ComboBoxItem;
            if (item == null) return 1;
            string txt = item.Content.ToString().Trim();
            if (int.TryParse(txt.Split(' ')[0], out int n)) return n;
            return 1;
        }

        private void RefreshExtensionSummary()
        {
            if (_extendTarget == null) return;

            int sessions = GetExtSessions();
            decimal rate = _extRatePerSession;
            decimal subtotal = rate * sessions;
            decimal tax = subtotal * 0.12m;

            decimal discount = 0;
            if (txtExtDiscount != null) decimal.TryParse(txtExtDiscount.Text, out discount);
            discount = Math.Max(0, discount);

            decimal total = Math.Max(0, subtotal + tax - discount);

            DateTime newCO = _extendTarget.RawCheckOut.AddDays(sessions);

            // Update labels
            extSummRoom.Text = string.Format("Room {0}", _extendTarget.RoomNumber);
            extSummType.Text = _extendTarget.RoomType;
            extSummPeriod.Text = string.Format("{0}  →  {1:MMM dd, yyyy}",
                _extendTarget.RawCheckOut.ToString("MMM dd, yyyy"), newCO);

            extSummRate.Text = string.Format("₱{0:N2}", rate);
            extSummSessions.Text = sessions.ToString();
            extSummSubtotal.Text = string.Format("₱{0:N2}", subtotal);
            extSummTax.Text = string.Format("₱{0:N2}", tax);
            extSummDiscount.Text = string.Format("-₱{0:N2}", discount);
            extSummTotal.Text = string.Format("₱{0:N2}", total);

            if (extNewCheckOut != null)
                extNewCheckOut.Text = newCO.ToString("MMMM dd, yyyy");

            // Payment status hint
            UpdateExtPaymentStatus(total);
        }

        private void UpdateExtPaymentStatus(decimal total)
        {
            if (txtExtAmountPaid == null || lblExtPayStatus == null) return;

            decimal paid = 0;
            decimal.TryParse(txtExtAmountPaid.Text, out paid);

            if (paid >= total && total > 0)
            {
                lblExtPayStatus.Text = "✔  Fully Paid";
                lblExtPayStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x74));
            }
            else if (paid > 0)
            {
                lblExtPayStatus.Text = string.Format("⚠  Partial — Balance: ₱{0:N2}", total - paid);
                lblExtPayStatus.Foreground = new SolidColorBrush(Color.FromRgb(0xE0, 0x90, 0x40));
            }
            else
            {
                lblExtPayStatus.Text = "— Enter amount above";
                lblExtPayStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x30, 0x10));
            }
        }

        private void ExtSessions_Changed(object sender, SelectionChangedEventArgs e) => RefreshExtensionSummary();
        private void ExtDiscount_Changed(object sender, TextChangedEventArgs e) => RefreshExtensionSummary();
        private void ExtAmountPaid_Changed(object sender, TextChangedEventArgs e)
        {
            if (extSummTotal == null) return;
            string raw = extSummTotal.Text.Replace("₱", "").Replace(",", "");
            if (decimal.TryParse(raw, out decimal total)) UpdateExtPaymentStatus(total);
        }

        private void BtnExtendBack_Click(object sender, RoutedEventArgs e) => ShowPanel("bookings");

        private void BtnConfirmExtension_Click(object sender, RoutedEventArgs e)
        {
            if (_extendTarget == null) { Warn("No reservation selected."); return; }
            if (cmbExtPayMethod.SelectedItem == null) { Warn("Please select a payment method."); return; }

            int sessions = GetExtSessions();
            decimal rate = _extRatePerSession;
            decimal subtotal = rate * sessions;
            decimal tax = subtotal * 0.12m;

            decimal discount = 0;
            decimal.TryParse(txtExtDiscount.Text, out discount);
            discount = Math.Max(0, discount);

            decimal total = Math.Max(0, subtotal + tax - discount);
            decimal paid = 0;
            if (!string.IsNullOrWhiteSpace(txtExtAmountPaid.Text))
                decimal.TryParse(txtExtAmountPaid.Text, out paid);

            string payStatus = paid >= total ? "Paid" : paid > 0 ? "Partial" : "Unpaid";
            DateTime newCheckOut = _extendTarget.RawCheckOut.AddDays(sessions);
            string payMethod = ((ComboBoxItem)cmbExtPayMethod.SelectedItem).Content.ToString();
            int resId = _extendTarget.ReservationID;

            // Confirm dialog
            var result = MessageBox.Show(
                string.Format(
                    "Confirm extension for {0}?\n\n" +
                    "  Room           : {1}\n" +
                    "  Sessions added : {2} × ₱{3:N2}/3 hrs\n" +
                    "  New Check-Out  : {4:MMMM dd, yyyy}\n" +
                    "  Extra Total    : ₱{5:N2}\n" +
                    "  Amount Paid    : ₱{6:N2}  ({7})",
                    _extendTarget.GuestName,
                    _extendTarget.RoomNumber,
                    sessions, rate,
                    newCheckOut,
                    total, paid, payStatus),
                "Confirm Extension",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            SqlConnection conn = null;
            SqlTransaction tx = null;
            try
            {
                conn = OpenConnection();
                tx = conn.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

                // 1. Update CheckOutDate
                new SqlCommand("UPDATE Reservations SET CheckOutDate=@co WHERE ReservationID=@rid", conn, tx)
                    .WithParam("@co", newCheckOut)
                    .WithParam("@rid", resId)
                    .ExecuteNonQuery();

                // 2. Get BillingID
                int billingId;
                using (var cmdBid = new SqlCommand(
                    "SELECT BillingID FROM Billing WHERE ReservationID=@rid", conn, tx))
                {
                    cmdBid.Parameters.AddWithValue("@rid", resId);
                    billingId = Convert.ToInt32(cmdBid.ExecuteScalar());
                }

                // 3. Update Billing totals
                using (var cmdBill = new SqlCommand(@"
                    UPDATE Billing
                    SET TotalAmount   = TotalAmount   + @extra,
                        TaxAmount     = TaxAmount     + @tax,
                        FinalAmount   = FinalAmount   + @total
                    WHERE BillingID = @bid", conn, tx))
                {
                    cmdBill.Parameters.AddWithValue("@extra", subtotal);
                    cmdBill.Parameters.AddWithValue("@tax", tax);
                    cmdBill.Parameters.AddWithValue("@total", total);
                    cmdBill.Parameters.AddWithValue("@bid", billingId);
                    cmdBill.ExecuteNonQuery();
                }

                // 4. Billing Detail for the extension
                InsertBillingDetail(conn, tx, billingId,
                    string.Format("Stay Extension ({0} session(s) × 3 hrs)", sessions), sessions, rate);

                // 5. Payment record for extension
                string extRef = string.IsNullOrWhiteSpace(txtExtPayRef.Text)
                    ? string.Format("EXT-{0}-{1}", resId, DateTime.Now.ToString("yyyyMMddHHmmss"))
                    : txtExtPayRef.Text.Trim();

                using (var cmdPay = new SqlCommand(@"
                    INSERT INTO Payment
                           (ReservationID,AmountPaid,PaymentMethod,
                            PaymentReferenceNumber,PaymentStatus,PaymentDate)
                    VALUES (@rid,@amt,@meth,@ref,@pst,@pdt)", conn, tx))
                {
                    cmdPay.Parameters.AddWithValue("@rid", resId);
                    cmdPay.Parameters.AddWithValue("@amt", paid);
                    cmdPay.Parameters.AddWithValue("@meth", payMethod);
                    cmdPay.Parameters.AddWithValue("@ref", extRef);
                    cmdPay.Parameters.AddWithValue("@pst", payStatus);
                    cmdPay.Parameters.AddWithValue("@pdt",
                        paid > 0 ? (object)DateTime.Now : DBNull.Value);
                    cmdPay.ExecuteNonQuery();
                }

                tx.Commit();

                MessageBox.Show(
                    string.Format(
                        "✅  Stay extended successfully!\n\n" +
                        "  Guest         : {0}\n" +
                        "  Room          : {1}\n" +
                        "  New Check-Out : {2:MMMM dd, yyyy}\n" +
                        "  Extra Charge  : ₱{3:N2}\n" +
                        "  Paid          : ₱{4:N2}  ({5})",
                        _extendTarget.GuestName, _extendTarget.RoomNumber,
                        newCheckOut, total, paid, payStatus),
                    "Extension Confirmed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _extendTarget = null;
                LoadBookings();
                RefreshDashboard();
                ShowPanel("bookings");
            }
            catch (Exception ex)
            {
                if (tx != null) try { tx.Rollback(); } catch { }
                ShowDbError("BtnConfirmExtension_Click", ex);
            }
            finally { tx?.Dispose(); conn?.Close(); }
        }

        // ══════════════════════════════════════════════════════════════════
        // EMAIL
        // ══════════════════════════════════════════════════════════════════
        private void TrySendConfirmationEmail(string toEmail, string guestName,
            int reservationId, string roomLabel,
            DateTime checkIn, DateTime checkOut,
            int sessions, decimal total, decimal amountPaid, string payStatus,
            DateTime paymentDate)
        {
            try
            {
                string body = BuildEmailBody(guestName, reservationId, roomLabel,
                    checkIn, checkOut, sessions, total, amountPaid, payStatus, paymentDate);

                using (var mail = new MailMessage())
                {
                    mail.From = new MailAddress(SmtpFrom, HotelName);
                    mail.Subject = string.Format("[{0}] Booking Confirmation — #RES-{1:D5}",
                        HotelName, reservationId);
                    mail.Body = body;
                    mail.IsBodyHtml = true;
                    mail.To.Add(toEmail);

                    using (var smtp = new SmtpClient(SmtpHost, SmtpPort))
                    {
                        smtp.EnableSsl = true;
                        smtp.Credentials = new NetworkCredential(SmtpUser, SmtpPassword);
                        smtp.Send(mail);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Booking saved, but confirmation email could not be sent.\n\nReason: {0}", ex.Message),
                    "Email Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static string BuildEmailBody(string guestName, int reservationId,
            string roomLabel, DateTime checkIn, DateTime checkOut,
            int sessions, decimal total, decimal amountPaid, string payStatus,
            DateTime paymentDate)
        {
            string payDateLine = amountPaid > 0
                ? string.Format("<tr><td>Payment Date</td><td><strong>{0:MMMM dd, yyyy  HH:mm:ss}</strong></td></tr>", paymentDate)
                : "";

            return string.Format(@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/>
<style>
  body{{font-family:'Segoe UI',Arial,sans-serif;background:#f5f0eb;margin:0;padding:20px;color:#333}}
  .card{{max-width:600px;margin:0 auto;background:#fff;border-radius:12px;
         box-shadow:0 4px 20px rgba(0,0,0,.12);overflow:hidden}}
  .header{{background:#1a0f05;padding:28px;text-align:center}}
  .header h1{{color:#C9922A;margin:0;font-size:22px;letter-spacing:2px}}
  .header p{{color:#6B4A10;margin:6px 0 0;font-size:13px}}
  .body{{padding:28px}}
  .greeting{{font-size:17px;color:#1a0f05;margin-bottom:18px}}
  .res-id{{text-align:center;background:#fdf6ec;border:2px solid #C9922A;
           border-radius:10px;padding:14px;margin:18px 0}}
  .res-id .label{{color:#6B4A10;font-size:12px;font-weight:600;text-transform:uppercase}}
  .res-id .value{{color:#C9922A;font-size:26px;font-weight:700;margin-top:4px}}
  table{{width:100%;border-collapse:collapse;margin:14px 0}}
  tr:nth-child(even) td{{background:#fdf8f4}}
  td{{padding:10px 12px;font-size:13px;border-bottom:1px solid #f0e8e0}}
  td:first-child{{color:#6B4A10;font-weight:600;width:40%}}
  td:last-child{{color:#1a0f05;font-weight:500}}
  .total-row td{{border-top:2px solid #C9922A;font-size:15px;font-weight:700}}
  .total-row td:last-child{{color:#4CAF74}}
  .footer{{background:#fdf6ec;padding:18px;text-align:center;color:#6B4A10;font-size:12px}}
  .footer strong{{color:#C9922A}}
</style>
</head>
<body>
<div class='card'>
  <div class='header'><h1>JOHNCIS LODGE</h1><p>Booking Confirmation</p></div>
  <div class='body'>
    <p class='greeting'>Dear <strong>{0}</strong>,</p>
    <p>Your reservation has been confirmed. Here are your booking details:</p>
    <div class='res-id'>
      <div class='label'>Reservation ID</div>
      <div class='value'>#RES-{1:D5}</div>
    </div>
    <table>
      <tr><td>Room</td><td>{2}</td></tr>
      <tr><td>Check-In</td><td>{3:MMMM dd, yyyy}</td></tr>
      <tr><td>Check-Out</td><td>{4:MMMM dd, yyyy}</td></tr>
      <tr><td>Sessions (3 hrs each)</td><td>{5}</td></tr>
      <tr><td>Amount Paid</td><td>&#8369;{6:N2}</td></tr>
      {9}
      <tr class='total-row'><td>Total Due</td><td>&#8369;{7:N2}</td></tr>
      <tr><td>Payment Status</td><td><strong>{8}</strong></td></tr>
    </table>
    <p style='color:#6B4A10;font-size:13px;'>
      Please present this email or your Reservation ID at check-in.
    </p>
  </div>
  <div class='footer'>
    <strong>Johncis Lodge</strong> &middot; Thank you for choosing us!<br/>
    This is an automated confirmation. Please do not reply to this email.
  </div>
</div>
</body></html>",
                guestName, reservationId, roomLabel,
                checkIn, checkOut, sessions, amountPaid, total, payStatus, payDateLine);
        }

        // ══════════════════════════════════════════════════════════════════
        // CLEAR FORM
        // ══════════════════════════════════════════════════════════════════
        private void ClearForm()
        {
            txtFirstName.Text = txtLastName.Text = txtContactNumber.Text =
            txtEmail.Text = txtIDNumber.Text = txtPaymentRef.Text =
            txtAmountPaid.Text = txtItemDesc.Text = txtItemQty.Text = txtItemPrice.Text = "";
            txtDiscount.Text = "0";

            cmbIDType.SelectedIndex = cmbGuests.SelectedIndex = cmbPaymentMethod.SelectedIndex = -1;

            dpCheckIn.SelectedDate = DateTime.Today;
            dpCheckOut.SelectedDate = DateTime.Today.AddDays(1);

            _billingItems.Clear();
            _selectedRoom = null;

            if (selectedRoomBar != null) selectedRoomBar.Visibility = Visibility.Collapsed;
            if (lblTotal != null) lblTotal.Text = "";
            if (lblRoomRate != null) lblRoomRate.Text = "";
            if (lblNights != null) lblNights.Text = "";
            if (lblSubtotal != null) lblSubtotal.Text = "";
            if (lblTax != null) lblTax.Text = "";
        }

        // ══════════════════════════════════════════════════════════════════
        // REFRESH
        // ══════════════════════════════════════════════════════════════════
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadRoomCards(GetTypeFilter());
            LoadRooms();
            LoadBookings();
            RefreshDashboard();
        }

        // ══════════════════════════════════════════════════════════════════
        // SEARCH
        // ══════════════════════════════════════════════════════════════════
        private void BtnSearchReservation_Click(object sender, RoutedEventArgs e)
        {
            string keyword = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter guest name, reservation ID, or room number:",
                "Search Reservation", "");
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                LoadBookings(keyword);
                ShowPanel("bookings");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // CHECK-OUT
        // ══════════════════════════════════════════════════════════════════
        private void BtnCheckOut_Click(object sender, RoutedEventArgs e)
        {
            var sel = lvBookings.SelectedItem as BookingViewModel;
            if (sel == null) { Warn("Select a reservation from the bookings list first."); return; }
            if (sel.Status != "Confirmed")
            { Warn(string.Format("Only 'Confirmed' reservations can be checked out.\nCurrent: {0}", sel.Status)); return; }

            if (MessageBox.Show(
                string.Format("Check out '{0}' from Room {1}?", sel.GuestName, sel.RoomNumber),
                "Confirm Check-Out", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            SqlConnection conn = null; SqlTransaction tx = null;
            try
            {
                conn = OpenConnection();
                tx = conn.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

                new SqlCommand("UPDATE Reservations SET ReservationStatus='Checked Out' WHERE ReservationID=@rid", conn, tx)
                    .WithParam("@rid", sel.ReservationID).ExecuteNonQuery();

                new SqlCommand("UPDATE Rooms SET Status='Cleaning' WHERE RoomNumber=@rn", conn, tx)
                    .WithParam("@rn", sel.RoomNumber).ExecuteNonQuery();

                using (var cmdHk = new SqlCommand(@"
                    INSERT INTO HouseKeeping (StaffID,RoomID,CleaningDate,CleaningStatus,Notes)
                    SELECT @sid,(SELECT RoomID FROM Rooms WHERE RoomNumber=@rn),
                           CAST(GETDATE() AS DATE),'Pending','Post checkout cleaning'", conn, tx))
                {
                    cmdHk.Parameters.AddWithValue("@sid", GetCurrentStaffId());
                    cmdHk.Parameters.AddWithValue("@rn", sel.RoomNumber);
                    cmdHk.ExecuteNonQuery();
                }

                tx.Commit();
                MessageBox.Show(
                    string.Format("'{0}' checked out successfully.\nRoom {1} has been queued for cleaning.",
                        sel.GuestName, sel.RoomNumber),
                    "Check-Out Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadRoomCards(GetTypeFilter()); LoadRooms(); LoadBookings(); RefreshDashboard();
            }
            catch (Exception ex)
            {
                if (tx != null) try { tx.Rollback(); } catch { }
                ShowDbError("BtnCheckOut_Click", ex);
            }
            finally { tx?.Dispose(); conn?.Close(); }
        }

        // ══════════════════════════════════════════════════════════════════
        // CANCEL
        // ══════════════════════════════════════════════════════════════════
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            var sel = lvBookings.SelectedItem as BookingViewModel;
            if (sel == null) { Warn("Select a reservation from the bookings list first."); return; }
            if (sel.Status == "Checked Out") { Warn("Cannot cancel a reservation already checked out."); return; }

            if (MessageBox.Show(
                string.Format("Cancel reservation #{0} for '{1}'?\n\nThis cannot be undone.",
                    sel.ReservationID, sel.GuestName),
                "Confirm Cancellation", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            SqlConnection conn = null; SqlTransaction tx = null;
            try
            {
                conn = OpenConnection();
                tx = conn.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);

                new SqlCommand("UPDATE Reservations SET ReservationStatus='Cancelled' WHERE ReservationID=@rid", conn, tx)
                    .WithParam("@rid", sel.ReservationID).ExecuteNonQuery();
                new SqlCommand("UPDATE Billing SET BillingStatus='Cancelled' WHERE ReservationID=@rid", conn, tx)
                    .WithParam("@rid", sel.ReservationID).ExecuteNonQuery();
                new SqlCommand("UPDATE Rooms SET Status='Available' WHERE RoomNumber=@rn", conn, tx)
                    .WithParam("@rn", sel.RoomNumber).ExecuteNonQuery();

                tx.Commit();
                MessageBox.Show(
                    string.Format("Reservation #{0} has been cancelled.\nRoom {1} is now available.",
                        sel.ReservationID, sel.RoomNumber),
                    "Reservation Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadRoomCards(GetTypeFilter()); LoadRooms(); LoadBookings(); RefreshDashboard();
            }
            catch (Exception ex)
            {
                if (tx != null) try { tx.Rollback(); } catch { }
                ShowDbError("BtnCancel_Click", ex);
            }
            finally { tx?.Dispose(); conn?.Close(); }
        }

        // ══════════════════════════════════════════════════════════════════
        // HOUSEKEEPING
        // ══════════════════════════════════════════════════════════════════
        private void BtnHousekeeping_Click(object sender, RoutedEventArgs e)
        {
            SqlConnection conn = null; SqlDataReader dr = null;
            try
            {
                conn = OpenConnection();
                dr = new SqlCommand(@"
                    SELECT hk.HouseKeepingID, r.RoomNumber, rt.TypeName,
                           hk.CleaningDate, hk.CleaningStatus, hk.Notes, s.FullName AS StaffName
                    FROM   HouseKeeping hk
                    JOIN   Rooms     r  ON r.RoomID      = hk.RoomID
                    JOIN   RoomTypes rt ON rt.RoomTypeID = r.RoomTypeID
                    JOIN   Staff     s  ON s.StaffID     = hk.StaffID
                    WHERE  hk.CleaningStatus = 'Pending'
                    ORDER  BY r.RoomNumber", conn).ExecuteReader();

                string log = "═══════════════════════════════\n  HOUSEKEEPING — PENDING ROOMS\n═══════════════════════════════\n\n";
                bool hasRows = false;

                while (dr.Read())
                {
                    hasRows = true;
                    log += string.Format("  Room {0} ({1})  ·  {2:MM/dd/yyyy}\n",
                        dr["RoomNumber"], dr["TypeName"], Convert.ToDateTime(dr["CleaningDate"]));
                    log += string.Format("  Assigned to: {0}  |  {1}\n\n", dr["StaffName"], dr["Notes"]);
                }
                if (!hasRows) log += "  No rooms pending housekeeping.\n";

                dr.Close(); dr = null;

                if (MessageBox.Show(
                    log + "\nMark all pending rooms as Cleaned and set status to Available?",
                    "Housekeeping Log", MessageBoxButton.YesNo, MessageBoxImage.Information) != MessageBoxResult.Yes) return;

                var tx2 = conn.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
                try
                {
                    int updated = new SqlCommand(
                        "UPDATE HouseKeeping SET CleaningStatus='Completed' WHERE CleaningStatus='Pending'",
                        conn, tx2).ExecuteNonQuery();

                    new SqlCommand("UPDATE Rooms SET Status='Available' WHERE Status='Cleaning'",
                        conn, tx2).ExecuteNonQuery();

                    tx2.Commit();
                    MessageBox.Show(
                        string.Format("{0} housekeeping task(s) completed. Rooms are now Available.", updated),
                        "Housekeeping Done", MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadRoomCards(GetTypeFilter()); LoadRooms(); RefreshDashboard();
                }
                catch (Exception ex) { try { tx2.Rollback(); } catch { } ShowDbError("Housekeeping update", ex); }
            }
            catch (Exception ex) { ShowDbError("BtnHousekeeping_Click", ex); }
            finally { dr?.Close(); conn?.Close(); }
        }

        // ══════════════════════════════════════════════════════════════════
        // BOOKINGS LIST EVENTS
        // ══════════════════════════════════════════════════════════════════
        private void LvBookings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var sel = lvBookings.SelectedItem as BookingViewModel;
            if (sel != null && txtSearch != null) txtSearch.Text = sel.GuestName;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
            => LoadBookings(txtSearch.Text.Trim());

        // ══════════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════════
        private int GetCurrentStaffId()
        {
            int id = 0;
            var item = cmbStaff.SelectedItem as ComboBoxItem;
            if (item?.Tag != null && int.TryParse(item.Tag.ToString(), out id)) return id;
            return 1;
        }

        private SolidColorBrush GetStatusColor(string status)
        {
            switch (status)
            {
                case "Available": return new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x74));
                case "Occupied": return new SolidColorBrush(Color.FromRgb(0xE0, 0x55, 0x55));
                case "Cleaning": return new SolidColorBrush(Color.FromRgb(0xE0, 0x90, 0x40));
                case "Maintenance": return new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
                default: return new SolidColorBrush(Colors.Gray);
            }
        }

        private SolidColorBrush GetStatusBadgeBg(string status)
        {
            switch (status)
            {
                case "Available": return new SolidColorBrush(Color.FromArgb(40, 0x4C, 0xAF, 0x74));
                case "Occupied": return new SolidColorBrush(Color.FromArgb(40, 0xE0, 0x55, 0x55));
                case "Cleaning": return new SolidColorBrush(Color.FromArgb(40, 0xE0, 0x90, 0x40));
                case "Maintenance": return new SolidColorBrush(Color.FromArgb(40, 0x66, 0x66, 0x66));
                default: return new SolidColorBrush(Color.FromArgb(40, 0x88, 0x88, 0x88));
            }
        }

        private static void Warn(string msg)
            => MessageBox.Show(msg, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);

        private static void ShowDbError(string source, Exception ex)
            => MessageBox.Show(string.Format("Error in {0}:\n\n{1}", source, ex.Message),
                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    // ════════════════════════════════════════════════════════════════════════
    // SqlCommand extension
    // ════════════════════════════════════════════════════════════════════════
    internal static class SqlCommandExtensions
    {
        public static SqlCommand WithParam(this SqlCommand cmd, string name, object value)
        {
            cmd.Parameters.AddWithValue(name, value ?? (object)DBNull.Value);
            return cmd;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // VIEW MODELS
    // ════════════════════════════════════════════════════════════════════════
    public class RoomCardViewModel : INotifyPropertyChanged
    {
        private SolidColorBrush _borderColor;
        private SolidColorBrush _bgColor;
        private string _selectHint;
        private SolidColorBrush _hintColor;
        private Visibility _bookBtnVisible = Visibility.Visible;
        private Visibility _hintVisible = Visibility.Collapsed;

        public int RoomID { get; set; }
        public string RoomNumber { get; set; }
        public string TypeName { get; set; }
        public string Description { get; set; }
        public decimal PricePerNight { get; set; }

        // Formatted for card display
        public string PriceFormatted => string.Format("{0:N0}", PricePerNight);

        public string Status { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsSelected { get; set; }

        public SolidColorBrush StatusColor { get; set; }
        public SolidColorBrush StatusBadgeBg { get; set; }

        public SolidColorBrush BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; OnPropChanged("BorderColor"); }
        }
        public SolidColorBrush BackgroundColor
        {
            get { return _bgColor; }
            set { _bgColor = value; OnPropChanged("BackgroundColor"); }
        }
        public string SelectHint
        {
            get { return _selectHint; }
            set { _selectHint = value; OnPropChanged("SelectHint"); }
        }
        public SolidColorBrush HintColor
        {
            get { return _hintColor; }
            set { _hintColor = value; OnPropChanged("HintColor"); }
        }
        public Visibility BookBtnVisible
        {
            get { return _bookBtnVisible; }
            set { _bookBtnVisible = value; OnPropChanged("BookBtnVisible"); }
        }
        public Visibility HintVisible
        {
            get { return _hintVisible; }
            set { _hintVisible = value; OnPropChanged("HintVisible"); }
        }

        public ICommand SelectCommand { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropChanged(string n)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(n));
        }
    }

    public class RoomViewModel : INotifyPropertyChanged
    {
        public int RoomID { get; set; }
        public string RoomNumber { get; set; }
        public string TypeName { get; set; }
        public decimal PricePerNight { get; set; }

        // CHANGED: /3hrs label
        public string PriceDisplay => string.Format("₱{0:N0}/3hrs", PricePerNight);

        public string Status { get; set; }
        public SolidColorBrush StatusColor { get; set; }
        public SolidColorBrush StatusBadgeBg { get; set; }
        public SolidColorBrush BackgroundColor { get; set; }
        public SolidColorBrush BorderColor { get; set; }

#pragma warning disable 67
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore 67
    }

    public class BookingViewModel
    {
        public int ReservationID { get; set; }
        public string GuestName { get; set; }
        public string RoomNumber { get; set; }
        public string RoomType { get; set; }
        public string CheckIn { get; set; }
        public string CheckOut { get; set; }
        public string Guests { get; set; }
        public string Total { get; set; }
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentDate { get; set; }

        // Raw DateTime for extension calculations
        public DateTime RawCheckOut { get; set; }
    }

    public class BillingDetailItem
    {
        public string Description { get; set; }
        public int RawQty { get; set; }
        public decimal RawUnitPrice { get; set; }
        public int Quantity { get; set; }
        public string UnitPrice { get; set; }
        public string Subtotal { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════════
    // RELAY COMMAND
    // ════════════════════════════════════════════════════════════════════════
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object p) => _canExecute != null ? _canExecute(p) : true;
        public void Execute(object p) => _execute(p);

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}