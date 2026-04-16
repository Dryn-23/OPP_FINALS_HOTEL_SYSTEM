using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace OOP_FINALS.HotelReservation
{
    public partial class HotelReservation : Window
    {
        // ── Connection String ─────────────────────────────────────────────────
        private const string ConnStr =
            "Server=DESKTOP-51HVDT7;Database=JohnCis_HotelManagement_System;Integrated Security=True;";

        private DispatcherTimer _timer;
        private ObservableCollection<RoomViewModel> _rooms = new ObservableCollection<RoomViewModel>();
        private ObservableCollection<BookingViewModel> _bookings = new ObservableCollection<BookingViewModel>();

        // ── Constructor ───────────────────────────────────────────────────────
        public HotelReservation()
        {
            InitializeComponent();
        }

        // ── Window Loaded ─────────────────────────────────────────────────────
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StartClock();
            LoadRooms();
            LoadStaff();
            LoadRoomCombo();
            LoadBookings();
            RefreshDashboard();
            dpCheckIn.SelectedDate = DateTime.Today;
            dpCheckOut.SelectedDate = DateTime.Today.AddDays(1);
        }

        // ── Drag to move ──────────────────────────────────────────────────────
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        // ── Window controls ───────────────────────────────────────────────────
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        // ═════════════════════════════════════════════════════════════════════
        // CLOCK
        // ═════════════════════════════════════════════════════════════════════
        private void StartClock()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, _) =>
                lblClock.Text = DateTime.Now.ToString("dddd, MMMM dd yyyy   HH:mm:ss");
            _timer.Start();
            lblClock.Text = DateTime.Now.ToString("dddd, MMMM dd yyyy   HH:mm:ss");
        }

        // ═════════════════════════════════════════════════════════════════════
        // DATABASE HELPERS
        // ═════════════════════════════════════════════════════════════════════
        private SqlConnection OpenConnection()
        {
            SqlConnection conn = new SqlConnection(ConnStr);
            conn.Open();
            return conn;
        }

        private static object Scalar(SqlConnection conn, string sql)
        {
            SqlCommand cmd = new SqlCommand(sql, conn);
            return cmd.ExecuteScalar() ?? 0;
        }

        // ═════════════════════════════════════════════════════════════════════
        // LOAD – ROOM MAP
        // ═════════════════════════════════════════════════════════════════════
        private void LoadRooms()
        {
            _rooms.Clear();
            SqlConnection conn = null;
            SqlDataReader dr = null;
            try
            {
                conn = OpenConnection();
                string sql = @"
                    SELECT r.RoomID, r.RoomNumber, r.Status,
                           rt.TypeName, rt.PricePerNight
                    FROM   Rooms r
                    JOIN   RoomTypes rt ON rt.RoomTypeID = r.RoomTypeID
                    ORDER  BY r.RoomNumber";

                SqlCommand cmd = new SqlCommand(sql, conn);
                dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    string status = dr["Status"].ToString();
                    RoomViewModel room = new RoomViewModel
                    {
                        RoomID = (int)dr["RoomID"],
                        RoomNumber = dr["RoomNumber"].ToString(),
                        TypeName = dr["TypeName"].ToString(),
                        PricePerNight = Convert.ToDecimal(dr["PricePerNight"]),
                        Status = status,
                        StatusColor = GetStatusColor(status),
                        StatusBadgeBg = GetStatusBadgeBg(status),
                        BackgroundColor = new SolidColorBrush(Color.FromArgb(255, 28, 28, 28)),
                        BorderColor = GetStatusColor(status)
                    };
                    room.RoomClickCommand = new RelayCommand(o => OnRoomClicked((RoomViewModel)o));
                    _rooms.Add(room);
                }

                icRooms.ItemsSource = _rooms;
            }
            catch (Exception ex) { ShowDbError("LoadRooms", ex); }
            finally
            {
                dr?.Close();
                conn?.Close();
            }
        }

        private void OnRoomClicked(RoomViewModel room)
        {
            if (room.Status == "Available")
            {
                foreach (ComboBoxItem item in cmbRoom.Items)
                {
                    if (item.Tag != null && item.Tag.ToString() == room.RoomID.ToString())
                    {
                        cmbRoom.SelectedItem = item;
                        break;
                    }
                }
            }

            MessageBox.Show(
                $"Room {room.RoomNumber}\n" +
                $"Type   : {room.TypeName}\n" +
                $"Rate   : ₱{room.PricePerNight:N2} / night\n" +
                $"Status : {room.Status}",
                "Room Details", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ═════════════════════════════════════════════════════════════════════
        // LOAD – ROOM COMBO (Available rooms only)
        // ═════════════════════════════════════════════════════════════════════
        private void LoadRoomCombo()
        {
            cmbRoom.Items.Clear();
            SqlConnection conn = null;
            SqlDataReader dr = null;
            try
            {
                conn = OpenConnection();
                string sql = @"
                    SELECT r.RoomID, r.RoomNumber, rt.TypeName, rt.PricePerNight
                    FROM   Rooms r
                    JOIN   RoomTypes rt ON rt.RoomTypeID = r.RoomTypeID
                    WHERE  r.Status = 'Available'
                    ORDER  BY r.RoomNumber";

                SqlCommand cmd = new SqlCommand(sql, conn);
                dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    ComboBoxItem item = new ComboBoxItem
                    {
                        Content = $"{dr["RoomNumber"]}  —  {dr["TypeName"]}  (₱{dr["PricePerNight"]:N0}/night)",
                        Tag = dr["RoomID"].ToString()
                    };
                    cmbRoom.Items.Add(item);
                }
            }
            catch (Exception ex) { ShowDbError("LoadRoomCombo", ex); }
            finally
            {
                dr?.Close();
                conn?.Close();
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // LOAD – STAFF COMBO
        // ═════════════════════════════════════════════════════════════════════
        private void LoadStaff()
        {
            cmbStaff.Items.Clear();
            SqlConnection conn = null;
            SqlDataReader dr = null;
            try
            {
                conn = OpenConnection();
                string sql = @"
                    SELECT StaffID, FullName FROM Staff
                    WHERE  Status = 'Active'
                    ORDER  BY FullName";

                SqlCommand cmd = new SqlCommand(sql, conn);
                dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    ComboBoxItem item = new ComboBoxItem
                    {
                        Content = dr["FullName"].ToString(),
                        Tag = dr["StaffID"].ToString()
                    };
                    cmbStaff.Items.Add(item);
                }
            }
            catch (Exception ex) { ShowDbError("LoadStaff", ex); }
            finally
            {
                dr?.Close();
                conn?.Close();
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // LOAD – BOOKINGS LIST
        // ═════════════════════════════════════════════════════════════════════
        private void LoadBookings(string filter = "")
        {
            _bookings.Clear();
            SqlConnection conn = null;
            SqlDataReader dr = null;
            try
            {
                conn = OpenConnection();
                string sql = @"
                    SELECT r.ReservationID,
                           c.FirstName + ' ' + c.LastName AS GuestName,
                           rm.RoomNumber,
                           rt.TypeName  AS RoomType,
                           r.CheckInDate,
                           r.CheckOutDate,
                           r.NumberOfGuest,
                           b.FinalAmount,
                           r.ReservationStatus,
                           p.PaymentStatus
                    FROM   Reservations r
                    JOIN   Customers  c  ON c.CustomerID   = r.CustomerID
                    JOIN   Rooms      rm ON rm.RoomID       = r.RoomID
                    JOIN   RoomTypes  rt ON rt.RoomTypeID   = rm.RoomTypeID
                    LEFT JOIN Billing  b ON b.ReservationID = r.ReservationID
                    LEFT JOIN Payment  p ON p.ReservationID = r.ReservationID
                    WHERE  r.ReservationStatus <> 'Cancelled'";

                if (!string.IsNullOrWhiteSpace(filter))
                    sql += @" AND (
                               c.FirstName + ' ' + c.LastName LIKE @f
                            OR CAST(r.ReservationID AS VARCHAR) LIKE @f
                            OR rm.RoomNumber LIKE @f)";

                sql += " ORDER BY r.ReservationID DESC";

                SqlCommand cmd = new SqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(filter))
                    cmd.Parameters.AddWithValue("@f", $"%{filter}%");

                dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    string totalStr = dr["FinalAmount"] == DBNull.Value ? "—" : $"₱{Convert.ToDecimal(dr["FinalAmount"]):N2}";
                    string paymentStatus = dr["PaymentStatus"] == DBNull.Value ? "Unpaid" : dr["PaymentStatus"].ToString();

                    _bookings.Add(new BookingViewModel
                    {
                        ReservationID = (int)dr["ReservationID"],
                        GuestName = dr["GuestName"].ToString(),
                        RoomNumber = dr["RoomNumber"].ToString(),
                        RoomType = dr["RoomType"].ToString(),
                        CheckIn = Convert.ToDateTime(dr["CheckInDate"]).ToString("MM/dd/yyyy"),
                        CheckOut = Convert.ToDateTime(dr["CheckOutDate"]).ToString("MM/dd/yyyy"),
                        Guests = dr["NumberOfGuest"].ToString(),
                        Total = totalStr,
                        Status = dr["ReservationStatus"].ToString(),
                        PaymentStatus = paymentStatus
                    });
                }

                lvBookings.ItemsSource = _bookings;
            }
            catch (Exception ex) { ShowDbError("LoadBookings", ex); }
            finally
            {
                dr?.Close();
                conn?.Close();
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // DASHBOARD STATS
        // ═════════════════════════════════════════════════════════════════════
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
                    WHERE  CheckInDate = CAST(GETDATE() AS DATE)
                      AND  ReservationStatus = 'Confirmed'").ToString();

                decimal rev = Convert.ToDecimal(Scalar(conn, @"
                    SELECT ISNULL(SUM(p.AmountPaid), 0)
                    FROM   Payment p
                    JOIN   Reservations r ON r.ReservationID = p.ReservationID
                    WHERE  CAST(r.ReservationDate AS DATE) = CAST(GETDATE() AS DATE)
                      AND  p.PaymentStatus = 'Paid'"));

                lblRevenue.Text = $"₱{rev:N2}";
            }
            catch (Exception ex) { ShowDbError("RefreshDashboard", ex); }
            finally { conn?.Close(); }
        }

        // ═════════════════════════════════════════════════════════════════════
        // BILLING PREVIEW
        // ═════════════════════════════════════════════════════════════════════
        private void CmbRoom_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => UpdateBillingPreview();

        private void DatePicker_Changed(object sender, SelectionChangedEventArgs e)
            => UpdateBillingPreview();

        private void UpdateBillingPreview()
        {
            lblRoomRate.Text = lblNights.Text = lblTax.Text = lblTotal.Text = "";

            ComboBoxItem roomItem = cmbRoom.SelectedItem as ComboBoxItem;
            if (roomItem == null) return;

            string tag = roomItem.Tag as string;
            if (!int.TryParse(tag, out int roomId)) return;

            decimal rate = 0;
            SqlConnection conn = null;
            try
            {
                conn = OpenConnection();
                SqlCommand cmd = new SqlCommand(@"
                    SELECT rt.PricePerNight
                    FROM   Rooms r
                    JOIN   RoomTypes rt ON rt.RoomTypeID = r.RoomTypeID
                    WHERE  r.RoomID = @id", conn);
                cmd.Parameters.AddWithValue("@id", roomId);
                object val = cmd.ExecuteScalar();
                if (val != null && val != DBNull.Value)
                    rate = Convert.ToDecimal(val);
            }
            catch { /* silent */ }
            finally { conn?.Close(); }

            int nights = 0;
            if (dpCheckIn.SelectedDate.HasValue && dpCheckOut.SelectedDate.HasValue)
                nights = Math.Max(0, (dpCheckOut.SelectedDate.Value - dpCheckIn.SelectedDate.Value).Days);

            decimal subtotal = rate * nights;
            decimal tax = subtotal * 0.12m;
            decimal total = subtotal + tax;

            lblRoomRate.Text = $"₱{rate:N2}";
            lblNights.Text = nights.ToString();
            lblTax.Text = $"₱{tax:N2}";
            lblTotal.Text = $"₱{total:N2}";
        }

        // ═════════════════════════════════════════════════════════════════════
        // CONFIRM BOOKING  ← full transaction with ROLLBACK / COMMIT
        // ═════════════════════════════════════════════════════════════════════
        private void BtnBook_Click(object sender, RoutedEventArgs e)
        {
            // ── Validation ────────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(txtFirstName.Text)) { Warn("First name is required."); return; }
            if (string.IsNullOrWhiteSpace(txtLastName.Text)) { Warn("Last name is required."); return; }
            if (string.IsNullOrWhiteSpace(txtContactNumber.Text)) { Warn("Contact number is required."); return; }
            if (string.IsNullOrWhiteSpace(txtEmail.Text)) { Warn("Email address is required."); return; }
            if (cmbIDType.SelectedItem == null) { Warn("Please select a valid ID type."); return; }
            if (string.IsNullOrWhiteSpace(txtIDNumber.Text)) { Warn("ID number is required."); return; }
            if (cmbRoom.SelectedItem == null) { Warn("Please select a room."); return; }
            if (!dpCheckIn.SelectedDate.HasValue) { Warn("Check-in date is required."); return; }
            if (!dpCheckOut.SelectedDate.HasValue) { Warn("Check-out date is required."); return; }
            if (dpCheckOut.SelectedDate <= dpCheckIn.SelectedDate) { Warn("Check-out must be after check-in."); return; }
            if (cmbGuests.SelectedItem == null) { Warn("Please select number of guests."); return; }
            if (cmbPaymentMethod.SelectedItem == null) { Warn("Please select a payment method."); return; }
            if (cmbStaff.SelectedItem == null) { Warn("Please select a staff member."); return; }

            // ── Parse values ──────────────────────────────────────────────────
            ComboBoxItem roomItem = (ComboBoxItem)cmbRoom.SelectedItem;
            if (!int.TryParse(roomItem.Tag.ToString(), out int roomId))
            {
                Warn("Invalid room selection."); return;
            }

            ComboBoxItem staffItem = (ComboBoxItem)cmbStaff.SelectedItem;
            if (!int.TryParse(staffItem.Tag.ToString(), out int staffId))
            {
                Warn("Invalid staff selection."); return;
            }

            int guests = int.Parse(((ComboBoxItem)cmbGuests.SelectedItem).Content.ToString());
            DateTime checkIn = dpCheckIn.SelectedDate.Value;
            DateTime checkOut = dpCheckOut.SelectedDate.Value;
            int nights = (checkOut - checkIn).Days;

            string idType = ((ComboBoxItem)cmbIDType.SelectedItem).Content.ToString();
            string payMethod = ((ComboBoxItem)cmbPaymentMethod.SelectedItem).Content.ToString();

            decimal amountPaid = 0;
            if (!string.IsNullOrWhiteSpace(txtAmountPaid.Text) &&
                !decimal.TryParse(txtAmountPaid.Text, out amountPaid))
            {
                Warn("Amount paid must be a valid number."); return;
            }

            // ── Database transaction ──────────────────────────────────────────
            SqlConnection conn = null;
            SqlTransaction tx = null;
            try
            {
                conn = OpenConnection();
                tx = conn.BeginTransaction();   // ← BEGIN TRANSACTION

                // 1. Get room rate
                decimal rate = 0;
                SqlCommand cmdRate = new SqlCommand(@"
                    SELECT rt.PricePerNight
                    FROM   Rooms r
                    JOIN   RoomTypes rt ON rt.RoomTypeID = r.RoomTypeID
                    WHERE  r.RoomID = @id", conn, tx);
                cmdRate.Parameters.AddWithValue("@id", roomId);
                object rateVal = cmdRate.ExecuteScalar();
                if (rateVal == null || rateVal == DBNull.Value)
                    throw new Exception("Could not retrieve room rate.");
                rate = Convert.ToDecimal(rateVal);

                decimal subtotal = rate * nights;
                decimal tax = subtotal * 0.12m;
                decimal total = subtotal + tax;

                // 2. Upsert Customer
                SqlCommand cmdCustCheck = new SqlCommand(@"
                    SELECT CustomerID FROM Customers
                    WHERE  ContactNumber = @contact", conn, tx);
                cmdCustCheck.Parameters.AddWithValue("@contact", txtContactNumber.Text.Trim());

                object custObj = cmdCustCheck.ExecuteScalar();
                int customerId;

                if (custObj != null && custObj != DBNull.Value)
                {
                    // Update existing customer
                    customerId = Convert.ToInt32(custObj);
                    SqlCommand cmdUpdCust = new SqlCommand(@"
                        UPDATE Customers
                        SET    FirstName = @fn, LastName = @ln,
                               Email = @em, IDType = @idt, IDNumber = @idn
                        WHERE  CustomerID = @cid", conn, tx);
                    cmdUpdCust.Parameters.AddWithValue("@fn", txtFirstName.Text.Trim());
                    cmdUpdCust.Parameters.AddWithValue("@ln", txtLastName.Text.Trim());
                    cmdUpdCust.Parameters.AddWithValue("@em", txtEmail.Text.Trim());
                    cmdUpdCust.Parameters.AddWithValue("@idt", idType);
                    cmdUpdCust.Parameters.AddWithValue("@idn", txtIDNumber.Text.Trim());
                    cmdUpdCust.Parameters.AddWithValue("@cid", customerId);
                    cmdUpdCust.ExecuteNonQuery();
                }
                else
                {
                    // Insert new customer
                    SqlCommand cmdInsCust = new SqlCommand(@"
                        INSERT INTO Customers (FirstName, LastName, ContactNumber, Email, IDType, IDNumber)
                        OUTPUT INSERTED.CustomerID
                        VALUES (@fn, @ln, @contact, @em, @idt, @idn)", conn, tx);
                    cmdInsCust.Parameters.AddWithValue("@fn", txtFirstName.Text.Trim());
                    cmdInsCust.Parameters.AddWithValue("@ln", txtLastName.Text.Trim());
                    cmdInsCust.Parameters.AddWithValue("@contact", txtContactNumber.Text.Trim());
                    cmdInsCust.Parameters.AddWithValue("@em", txtEmail.Text.Trim());
                    cmdInsCust.Parameters.AddWithValue("@idt", idType);
                    cmdInsCust.Parameters.AddWithValue("@idn", txtIDNumber.Text.Trim());
                    customerId = (int)cmdInsCust.ExecuteScalar();
                }

                // 3. Insert Reservation
                SqlCommand cmdRes = new SqlCommand(@"
                    INSERT INTO Reservations
                           (CustomerID, RoomID, CheckInDate, CheckOutDate,
                            NumberOfGuest, ReservationDate, ReservationStatus, StaffID)
                    OUTPUT INSERTED.ReservationID
                    VALUES (@cid, @rid, @ci, @co, @ng, GETDATE(), 'Confirmed', @sid)", conn, tx);
                cmdRes.Parameters.AddWithValue("@cid", customerId);
                cmdRes.Parameters.AddWithValue("@rid", roomId);
                cmdRes.Parameters.AddWithValue("@ci", checkIn);
                cmdRes.Parameters.AddWithValue("@co", checkOut);
                cmdRes.Parameters.AddWithValue("@ng", guests);
                cmdRes.Parameters.AddWithValue("@sid", staffId);
                int reservationId = (int)cmdRes.ExecuteScalar();

                // 4. Insert Billing
                SqlCommand cmdBill = new SqlCommand(@"
                    INSERT INTO Billing
                           (ReservationID, RoomRate, NumberOfNights, TaxAmount, FinalAmount)
                    VALUES (@rid, @rate, @nights, @tax, @total)", conn, tx);
                cmdBill.Parameters.AddWithValue("@rid", reservationId);
                cmdBill.Parameters.AddWithValue("@rate", rate);
                cmdBill.Parameters.AddWithValue("@nights", nights);
                cmdBill.Parameters.AddWithValue("@tax", tax);
                cmdBill.Parameters.AddWithValue("@total", total);
                cmdBill.ExecuteNonQuery();

                // 5. Insert Payment
                string payStatus = (amountPaid >= total) ? "Paid" : (amountPaid > 0 ? "Partial" : "Unpaid");
                SqlCommand cmdPay = new SqlCommand(@"
                    INSERT INTO Payment
                           (ReservationID, PaymentMethod, ReferenceNumber,
                            AmountPaid, PaymentDate, PaymentStatus)
                    VALUES (@rid, @meth, @ref, @amt, GETDATE(), @pst)", conn, tx);
                cmdPay.Parameters.AddWithValue("@rid", reservationId);
                cmdPay.Parameters.AddWithValue("@meth", payMethod);
                cmdPay.Parameters.AddWithValue("@ref", string.IsNullOrWhiteSpace(txtPaymentRef.Text) ? (object)DBNull.Value : txtPaymentRef.Text.Trim());
                cmdPay.Parameters.AddWithValue("@amt", amountPaid);
                cmdPay.Parameters.AddWithValue("@pst", payStatus);
                cmdPay.ExecuteNonQuery();

                // 6. Update Room status → Occupied
                SqlCommand cmdRoom = new SqlCommand(@"
                    UPDATE Rooms SET Status = 'Occupied' WHERE RoomID = @rid", conn, tx);
                cmdRoom.Parameters.AddWithValue("@rid", roomId);
                cmdRoom.ExecuteNonQuery();

                tx.Commit();   // ← COMMIT — all steps succeeded

                MessageBox.Show(
                    $"✔ Booking confirmed!\n\n" +
                    $"Reservation ID : {reservationId}\n" +
                    $"Guest          : {txtFirstName.Text.Trim()} {txtLastName.Text.Trim()}\n" +
                    $"Room           : {roomItem.Content}\n" +
                    $"Check-In       : {checkIn:MM/dd/yyyy}\n" +
                    $"Check-Out      : {checkOut:MM/dd/yyyy}\n" +
                    $"Total Due      : ₱{total:N2}\n" +
                    $"Amount Paid    : ₱{amountPaid:N2}\n" +
                    $"Payment Status : {payStatus}",
                    "Booking Confirmed", MessageBoxButton.OK, MessageBoxImage.Information);

                ClearForm();
                LoadRooms();
                LoadRoomCombo();
                LoadBookings();
                RefreshDashboard();
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { /* swallow secondary error */ }  // ← ROLLBACK on error
                ShowDbError("BtnBook_Click", ex);
            }
            finally
            {
                conn?.Close();
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // CLEAR FORM
        // ═════════════════════════════════════════════════════════════════════
        private void BtnClear_Click(object sender, RoutedEventArgs e) => ClearForm();

        private void ClearForm()
        {
            txtFirstName.Text = "";
            txtLastName.Text = "";
            txtContactNumber.Text = "";
            txtEmail.Text = "";
            txtIDNumber.Text = "";
            txtPaymentRef.Text = "";
            txtAmountPaid.Text = "";
            cmbIDType.SelectedIndex = -1;
            cmbRoom.SelectedIndex = -1;
            cmbGuests.SelectedIndex = -1;
            cmbPaymentMethod.SelectedIndex = -1;
            cmbStaff.SelectedIndex = -1;
            dpCheckIn.SelectedDate = DateTime.Today;
            dpCheckOut.SelectedDate = DateTime.Today.AddDays(1);
            lblRoomRate.Text = lblNights.Text = lblTax.Text = lblTotal.Text = "";
        }

        // ═════════════════════════════════════════════════════════════════════
        // REFRESH BUTTON
        // ═════════════════════════════════════════════════════════════════════
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadRooms();
            LoadRoomCombo();
            LoadBookings();
            RefreshDashboard();
        }

        // ═════════════════════════════════════════════════════════════════════
        // SEARCH RESERVATION
        // ═════════════════════════════════════════════════════════════════════
        private void BtnSearchReservation_Click(object sender, RoutedEventArgs e)
        {
            string keyword = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter guest name, reservation ID, or room number:",
                "Search Reservation", "");

            if (!string.IsNullOrWhiteSpace(keyword))
                LoadBookings(keyword);
        }

        // ═════════════════════════════════════════════════════════════════════
        // CHECK-OUT GUEST  ← transaction with ROLLBACK / COMMIT
        // ═════════════════════════════════════════════════════════════════════
        private void BtnCheckOut_Click(object sender, RoutedEventArgs e)
        {
            BookingViewModel selected = lvBookings.SelectedItem as BookingViewModel;
            if (selected == null)
            {
                Warn("Please select a reservation from the list to check out.");
                return;
            }

            if (selected.Status != "Confirmed")
            {
                Warn($"Only 'Confirmed' reservations can be checked out.\nCurrent status: {selected.Status}");
                return;
            }

            MessageBoxResult confirm = MessageBox.Show(
                $"Check out guest '{selected.GuestName}' from Room {selected.RoomNumber}?",
                "Confirm Check-Out", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            SqlConnection conn = null;
            SqlTransaction tx = null;
            try
            {
                conn = OpenConnection();
                tx = conn.BeginTransaction();   // ← BEGIN TRANSACTION

                // 1. Update reservation status → Checked Out
                SqlCommand cmdRes = new SqlCommand(@"
                    UPDATE Reservations
                    SET    ReservationStatus = 'Checked Out'
                    WHERE  ReservationID = @rid", conn, tx);
                cmdRes.Parameters.AddWithValue("@rid", selected.ReservationID);
                cmdRes.ExecuteNonQuery();

                // 2. Update room status → Cleaning
                SqlCommand cmdRoom = new SqlCommand(@"
                    UPDATE Rooms
                    SET    Status = 'Cleaning'
                    WHERE  RoomNumber = @rn", conn, tx);
                cmdRoom.Parameters.AddWithValue("@rn", selected.RoomNumber);
                cmdRoom.ExecuteNonQuery();

                tx.Commit();   // ← COMMIT

                MessageBox.Show(
                    $"Guest '{selected.GuestName}' has been checked out.\n" +
                    $"Room {selected.RoomNumber} is now marked for cleaning.",
                    "Check-Out Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadRooms();
                LoadRoomCombo();
                LoadBookings();
                RefreshDashboard();
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { }   // ← ROLLBACK
                ShowDbError("BtnCheckOut_Click", ex);
            }
            finally { conn?.Close(); }
        }

        // ═════════════════════════════════════════════════════════════════════
        // CANCEL RESERVATION  ← transaction with ROLLBACK / COMMIT
        // ═════════════════════════════════════════════════════════════════════
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            BookingViewModel selected = lvBookings.SelectedItem as BookingViewModel;
            if (selected == null)
            {
                Warn("Please select a reservation from the list to cancel.");
                return;
            }

            if (selected.Status == "Checked Out")
            {
                Warn("Cannot cancel a reservation that has already been checked out.");
                return;
            }

            MessageBoxResult confirm = MessageBox.Show(
                $"Cancel reservation #{selected.ReservationID} for '{selected.GuestName}'?\n\nThis action cannot be undone.",
                "Confirm Cancellation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            SqlConnection conn = null;
            SqlTransaction tx = null;
            try
            {
                conn = OpenConnection();
                tx = conn.BeginTransaction();   // ← BEGIN TRANSACTION

                // 1. Mark reservation as Cancelled
                SqlCommand cmdRes = new SqlCommand(@"
                    UPDATE Reservations
                    SET    ReservationStatus = 'Cancelled'
                    WHERE  ReservationID = @rid", conn, tx);
                cmdRes.Parameters.AddWithValue("@rid", selected.ReservationID);
                cmdRes.ExecuteNonQuery();

                // 2. Free the room back to Available
                SqlCommand cmdRoom = new SqlCommand(@"
                    UPDATE Rooms
                    SET    Status = 'Available'
                    WHERE  RoomNumber = @rn", conn, tx);
                cmdRoom.Parameters.AddWithValue("@rn", selected.RoomNumber);
                cmdRoom.ExecuteNonQuery();

                tx.Commit();   // ← COMMIT

                MessageBox.Show(
                    $"Reservation #{selected.ReservationID} has been cancelled.\n" +
                    $"Room {selected.RoomNumber} is now available.",
                    "Reservation Cancelled", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadRooms();
                LoadRoomCombo();
                LoadBookings();
                RefreshDashboard();
            }
            catch (Exception ex)
            {
                try { tx?.Rollback(); } catch { }   // ← ROLLBACK
                ShowDbError("BtnCancel_Click", ex);
            }
            finally { conn?.Close(); }
        }

        // ═════════════════════════════════════════════════════════════════════
        // HOUSEKEEPING LOG  ← transaction with ROLLBACK / COMMIT
        // ═════════════════════════════════════════════════════════════════════
        private void BtnHousekeeping_Click(object sender, RoutedEventArgs e)
        {
            // Show rooms currently in "Cleaning" status
            SqlConnection conn = null;
            SqlDataReader dr = null;
            try
            {
                conn = OpenConnection();
                SqlCommand cmd = new SqlCommand(@"
                    SELECT r.RoomNumber, rt.TypeName, r.Status
                    FROM   Rooms r
                    JOIN   RoomTypes rt ON rt.RoomTypeID = r.RoomTypeID
                    WHERE  r.Status IN ('Cleaning', 'Maintenance')
                    ORDER  BY r.RoomNumber", conn);

                dr = cmd.ExecuteReader();
                string log = "═══════════════════════════════\n";
                log += "  HOUSEKEEPING / MAINTENANCE LOG\n";
                log += "═══════════════════════════════\n\n";
                bool hasRows = false;

                while (dr.Read())
                {
                    hasRows = true;
                    log += $"  Room {dr["RoomNumber"]}  ({dr["TypeName"]})  —  {dr["Status"]}\n";
                }

                if (!hasRows) log += "  No rooms currently need housekeeping.";

                dr.Close();
                dr = null;

                MessageBoxResult result = MessageBox.Show(
                    log + "\n\nMark all 'Cleaning' rooms as 'Available'?",
                    "Housekeeping Log", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    SqlTransaction tx = conn.BeginTransaction();   // ← BEGIN TRANSACTION
                    try
                    {
                        SqlCommand cmdUpdate = new SqlCommand(@"
                            UPDATE Rooms SET Status = 'Available'
                            WHERE  Status = 'Cleaning'", conn, tx);
                        int updated = cmdUpdate.ExecuteNonQuery();

                        tx.Commit();   // ← COMMIT

                        MessageBox.Show(
                            $"{updated} room(s) marked as Available.",
                            "Housekeeping Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                        LoadRooms();
                        LoadRoomCombo();
                        RefreshDashboard();
                    }
                    catch (Exception ex)
                    {
                        try { tx.Rollback(); } catch { }   // ← ROLLBACK
                        ShowDbError("BtnHousekeeping_Click (update)", ex);
                    }
                }
            }
            catch (Exception ex) { ShowDbError("BtnHousekeeping_Click", ex); }
            finally
            {
                dr?.Close();
                conn?.Close();
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // BOOKING LIST – SELECTION CHANGED
        // ═════════════════════════════════════════════════════════════════════
        private void LvBookings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BookingViewModel selected = lvBookings.SelectedItem as BookingViewModel;
            if (selected == null) return;

            // Auto-fill search box with selected guest name for quick reference
            txtSearch.Text = selected.GuestName;
        }

        // ═════════════════════════════════════════════════════════════════════
        // SEARCH TEXT CHANGED
        // ═════════════════════════════════════════════════════════════════════
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadBookings(txtSearch.Text.Trim());
        }

        // ═════════════════════════════════════════════════════════════════════
        // STATUS COLORS
        // ═════════════════════════════════════════════════════════════════════
        private SolidColorBrush GetStatusColor(string status)
        {
            switch (status)
            {
                case "Available": return new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71));
                case "Occupied": return new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));
                case "Cleaning": return new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00));
                case "Maintenance": return new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
                default: return new SolidColorBrush(Colors.Gray);
            }
        }

        private SolidColorBrush GetStatusBadgeBg(string status)
        {
            switch (status)
            {
                case "Available": return new SolidColorBrush(Color.FromArgb(40, 0x2E, 0xCC, 0x71));
                case "Occupied": return new SolidColorBrush(Color.FromArgb(40, 0xE5, 0x39, 0x35));
                case "Cleaning": return new SolidColorBrush(Color.FromArgb(40, 0xFF, 0x98, 0x00));
                case "Maintenance": return new SolidColorBrush(Color.FromArgb(40, 0x55, 0x55, 0x55));
                default: return new SolidColorBrush(Color.FromArgb(40, 0x88, 0x88, 0x88));
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // HELPERS
        // ═════════════════════════════════════════════════════════════════════
        private static void Warn(string msg) =>
            MessageBox.Show(msg, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);

        private static void ShowDbError(string source, Exception ex) =>
            MessageBox.Show($"Error in {source}:\n\n{ex.Message}",
                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // VIEW MODELS
    // ═════════════════════════════════════════════════════════════════════════
    public class RoomViewModel : INotifyPropertyChanged
    {
        public int RoomID { get; set; }
        public string RoomNumber { get; set; }
        public string TypeName { get; set; }
        public decimal PricePerNight { get; set; }
        public string Status { get; set; }
        public SolidColorBrush StatusColor { get; set; }
        public SolidColorBrush StatusBadgeBg { get; set; }
        public SolidColorBrush BackgroundColor { get; set; }
        public SolidColorBrush BorderColor { get; set; }
        public ICommand RoomClickCommand { get; set; }

        // Suppress "event never used" warning — required by INotifyPropertyChanged contract
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
    }

    // ═════════════════════════════════════════════════════════════════════════
    // RELAY COMMAND
    // ═════════════════════════════════════════════════════════════════════════
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
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}