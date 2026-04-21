using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OOP_FINALS.AdminMainDashboard
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  VIEW-MODELS
    // ═══════════════════════════════════════════════════════════════════════════

    public class BookingItem
    {
        public string GuestName { get; set; }
        public string RoomInfo { get; set; }
        public string CheckIn { get; set; }
        public string CheckOut { get; set; }
        public string Status { get; set; }
        public string Amount { get; set; }
        public Brush StatusBg { get; set; }
        public Brush StatusFg { get; set; }
    }

    public class ActivityItem
    {
        public string Description { get; set; }
        public string TimeAgo { get; set; }
        public Brush DotColor { get; set; }
    }

    public class HousekeepingItem
    {
        public string RoomInfo { get; set; }
        public string Status { get; set; }
    }

    public class InventoryAlertItem
    {
        public string ItemName { get; set; }
        public string StockInfo { get; set; }
    }

    /// <summary>Holds data for one month bar on the chart.</summary>
    public class MonthlyChartBar
    {
        public string MonthLabel { get; set; }  // "Jan" … "Dec"
        public int GuestCount { get; set; }
        public decimal Revenue { get; set; }

        // Geometry set at render-time (used for hit-testing)
        public double X { get; set; }
        public double Width { get; set; }
        public double Top { get; set; }
        public double BarH { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  PAGE CODE-BEHIND
    // ═══════════════════════════════════════════════════════════════════════════

    public partial class DashboardPage : Page
    {
        private DispatcherTimer _refreshTimer;
        private List<MonthlyChartBar> _chartBars = new List<MonthlyChartBar>();
        private readonly string _currentStaffId = "1"; // replace with real session value

        // ── Status badge brushes ────────────────────────────────────────────
        private static readonly Brush BgCheckedIn = new SolidColorBrush(Color.FromRgb(0xE6, 0xF1, 0xFB));
        private static readonly Brush FgCheckedIn = new SolidColorBrush(Color.FromRgb(0x18, 0x5F, 0xA5));
        private static readonly Brush BgConfirmed = new SolidColorBrush(Color.FromRgb(0xEA, 0xF3, 0xDE));
        private static readonly Brush FgConfirmed = new SolidColorBrush(Color.FromRgb(0x3B, 0x6D, 0x11));
        private static readonly Brush BgCheckedOut = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9));
        private static readonly Brush FgCheckedOut = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
        private static readonly Brush BgPending = new SolidColorBrush(Color.FromRgb(0xFA, 0xEE, 0xDA));
        private static readonly Brush FgPending = new SolidColorBrush(Color.FromRgb(0x85, 0x4F, 0x0B));

        // ── Activity dot brushes ────────────────────────────────────────────
        private static readonly Brush DotBlue = new SolidColorBrush(Color.FromRgb(0x18, 0x5F, 0xA5));
        private static readonly Brush DotGreen = new SolidColorBrush(Color.FromRgb(0x63, 0x99, 0x22));
        private static readonly Brush DotGray = new SolidColorBrush(Color.FromRgb(0x88, 0x87, 0x80));
        private static readonly Brush DotAmber = new SolidColorBrush(Color.FromRgb(0xBA, 0x75, 0x17));

        // ── Chart brushes ───────────────────────────────────────────────────
        private static readonly Brush BarNormal = new SolidColorBrush(Color.FromRgb(0x7A, 0x4A, 0x17));
        private static readonly Brush BarCurrent = new SolidColorBrush(Color.FromRgb(0x18, 0x5F, 0xA5));
        private static readonly Brush GridLine = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
        private static readonly Brush LabelColor = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));

        // ═══════════════════════════════════════════════════════════════════
        //  CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════════════

        public DashboardPage()
        {
            InitializeComponent();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  PAGE LIFECYCLE
        // ═══════════════════════════════════════════════════════════════════

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            bool dbOk = TestDatabaseConnection();

            if (dbOk)
                LoadDashboardStats();
            else
                MessageBox.Show(
                    "Unable to connect to the database. Please check your connection and try again.",
                    "Database Connection Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

            // Auto-refresh every 60 seconds
            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
            _refreshTimer.Tick += (s, _) =>
            {
                if (TestDatabaseConnection()) LoadDashboardStats();
            };
            _refreshTimer.Start();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _refreshTimer?.Stop();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  DATABASE TEST
        // ═══════════════════════════════════════════════════════════════════

        private bool TestDatabaseConnection()
        {
            try
            {
                var db = new DatabaseHelper();
                var result = db.ExecuteScalar(@"SELECT 
                                                 COUNT(*) 
                                                FROM INFORMATION_SCHEMA.TABLES");
                return result != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] Connection failed: {ex.Message}");
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  MAIN DASHBOARD LOADER
        // ═══════════════════════════════════════════════════════════════════

        private void LoadDashboardStats()
        {
            try
            {
                var db = new DatabaseHelper();
                LoadWelcomeUser(db);
                LoadStatCards(db);
                LoadRoomStatus(db);
                LoadRecentBookings(db);
                LoadQuickStats(db);
                LoadRecentActivity(db);
                LoadMonthlyChart(db);
                LoadHousekeeping(db);
                LoadInventoryAlerts(db);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Dashboard] Load error: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  WELCOME HEADER
        //  FIX: use a parameterized query instead of string interpolation
        //       to avoid SQL injection via _currentStaffId.
        // ═══════════════════════════════════════════════════════════════════

        private void LoadWelcomeUser(DatabaseHelper db)
        {
            try
            {
                // Pass StaffID as a parameter — never interpolate user-controlled values.
                const string sql = @"
                                   SELECT 
                                    FullName 
                                   FROM Staff
                                   WHERE StaffID = @StaffID AND Status = 'Active'";

                var name = db.ExecuteScalarWithParam(sql, "@StaffID", _currentStaffId);
                if (name != null)
                {
                    string fullName = name.ToString();
                    WelcomeSubText.Text = $"Welcome back, {fullName} — here's what's happening today";
                    UserNameText.Text = fullName;
                    UserInitialsText.Text = GetInitials(fullName);
                }
            }
            catch (Exception ex) { Console.WriteLine($"[Welcome] {ex.Message}"); }
        }

        private static string GetInitials(string fullName)
        {
            string[] parts = fullName.Split(' ');
            string result = "";
            foreach (var p in parts)
                if (!string.IsNullOrEmpty(p)) result += p[0];
            return result.Length >= 2
                ? result.Substring(0, 2).ToUpper()
                : result.ToUpper();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  STAT CARDS
        // ═══════════════════════════════════════════════════════════════════

        private void LoadStatCards(DatabaseHelper db)
        {
            // Total active bookings
            var bookings = db.ExecuteScalar(@"
                SELECT 
                 COUNT(*) 
                FROM Reservations
                WHERE LOWER(ReservationStatus) 
                IN ('confirmed','checked-in','checked in')");
            TotalBookingsText.Text = bookings != null
                ? Convert.ToInt32(bookings).ToString("N0") : "0";

            // Today check-ins
            var checkins = db.ExecuteScalar(@"
                SELECT
                COUNT(*) 
                FROM Reservations
                WHERE CAST(CheckInDate AS DATE) = CAST(GETDATE() AS DATE)
                  AND ReservationStatus IN ('Confirmed','Checked-In','Checked In')");
            TodayCheckinsText.Text = checkins != null
                ? Convert.ToInt32(checkins).ToString() : "0";

            // Monthly revenue (completed payments this month)
            var revenue = db.ExecuteScalar(@"
                SELECT 
                 ISNULL(SUM(AmountPaid), 0)
                FROM Payment
                WHERE MONTH(PaymentDate) = MONTH(GETDATE())
                  AND YEAR(PaymentDate)  = YEAR(GETDATE())
                  AND PaymentStatus      = 'Completed'");
            decimal rev = revenue != null ? Convert.ToDecimal(revenue) : 0;
            TodayRevenueText.Text = "₱" + rev.ToString("N0");

            // Occupancy rate
            var occTable = db.ExecuteQueryDataTable(@"
                SELECT
                  ISNULL(CAST(occ.Cnt AS FLOAT) / NULLIF(tot.Cnt, 0), 0) * 100 AS Rate
                FROM
                (SELECT COUNT(*) AS Cnt FROM Rooms WHERE Status = 'Occupied')     occ,
                (SELECT COUNT(*) AS Cnt FROM Rooms WHERE Status != 'Maintenance') tot");

            if (occTable != null && occTable.Rows.Count > 0)
            {
                decimal rate = Convert.ToDecimal(occTable.Rows[0]["Rate"]);
                OccupancyRateText.Text = rate.ToString("F0") + "%";
            }
            else OccupancyRateText.Text = "0%";
        }

        // ═══════════════════════════════════════════════════════════════════
        //  ROOM STATUS ROW
        // ═══════════════════════════════════════════════════════════════════

        private void LoadRoomStatus(DatabaseHelper db)
        {
            //  ROOM STATUS ROW
            var tbl = db.ExecuteQueryDataTable(@"
                SELECT 
                 Status, 
                 COUNT(*) AS Cnt
                FROM Rooms
                GROUP BY Status");

            int occupied = 0, available = 0, maintenance = 0;
            if (tbl != null)
            {
                foreach (DataRow row in tbl.Rows)
                {
                    string st = row["Status"].ToString().ToLower();
                    int cnt = Convert.ToInt32(row["Cnt"]);
                    if (st == "occupied") occupied = cnt;
                    else if (st == "available") available = cnt;
                    else if (st == "maintenance") maintenance = cnt;
                }
            }

            OccupiedText.Text = occupied.ToString();
            AvailableText.Text = available.ToString();
            MaintenanceText.Text = maintenance.ToString();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  RECENT BOOKINGS TABLE
        // ═══════════════════════════════════════════════════════════════════

        private void LoadRecentBookings(DatabaseHelper db)
        {
            try
            {
                //  RECENT BOOKINGS TABLE
                var tbl = db.ExecuteQueryDataTable(@"
                    SELECT TOP 6
                        c.FirstName + ' ' + c.LastName AS GuestName,
                        rt.TypeName + ' #' + rm.RoomNumber  AS RoomInfo,
                        FORMAT(r.CheckInDate,  'MMM dd') AS CheckIn,
                        FORMAT(r.CheckOutDate, 'MMM dd') AS CheckOut,
                        r.ReservationStatus AS Status,
                        ISNULL(b.FinalAmount, 0) AS Amount
                    FROM Reservations r
                    JOIN Customers  c  ON r.CustomerID  = c.CustomerID
                    JOIN Rooms      rm ON r.RoomID       = rm.RoomID
                    JOIN RoomTypes  rt ON rm.RoomTypeID  = rt.RoomTypeID
                    LEFT JOIN Billing b ON b.ReservationID = r.ReservationID
                    WHERE r.ReservationStatus IN ('Confirmed','Checked-In','Checked In','Checked-Out')
                    ORDER BY r.ReservationDate DESC");

                BookingsListBox.Items.Clear();

                if (tbl == null || tbl.Rows.Count == 0)
                {
                    BookingsListBox.Items.Add(new BookingItem
                    {
                        GuestName = "No recent bookings",
                        StatusBg = BgPending,
                        StatusFg = FgPending
                    });
                    return;
                }

                foreach (DataRow row in tbl.Rows)
                {
                    string status = row["Status"].ToString();
                    GetStatusBrushes(status, out Brush bg, out Brush fg);
                    decimal amt = Convert.ToDecimal(row["Amount"]);

                    BookingsListBox.Items.Add(new BookingItem
                    {
                        GuestName = row["GuestName"].ToString(),
                        RoomInfo = row["RoomInfo"].ToString(),
                        CheckIn = row["CheckIn"].ToString(),
                        CheckOut = row["CheckOut"].ToString(),
                        Status = status,
                        Amount = "₱" + amt.ToString("N0"),
                        StatusBg = bg,
                        StatusFg = fg
                    });
                }
            }
            catch (Exception ex) { Console.WriteLine($"[Bookings] {ex.Message}"); }
        }

        private static void GetStatusBrushes(string status, out Brush bg, out Brush fg)
        {
            string lower = status.ToLower();
            if (lower.Contains("checked-in") || lower.Contains("checked in")) { bg = BgCheckedIn; fg = FgCheckedIn; }
            else if (lower.Contains("checked-out") || lower.Contains("checked out")) { bg = BgCheckedOut; fg = FgCheckedOut; }
            else if (lower.Contains("confirmed")) { bg = BgConfirmed; fg = FgConfirmed; }
            else { bg = BgPending; fg = FgPending; }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  QUICK STATS
        // ═══════════════════════════════════════════════════════════════════

        private void LoadQuickStats(DatabaseHelper db)
        {
            //  QUICK STATS
            var cleaned = db.ExecuteScalar(@"
                SELECT 
                 COUNT(DISTINCT RoomID)
                FROM HouseKeeping
                WHERE CleaningStatus = 'Completed'");
            CleanedTodayText.Text = cleaned != null ? Convert.ToInt32(cleaned).ToString() : "0";

            var newGuests = db.ExecuteScalar(@"
                SELECT 
                 COUNT(DISTINCT r.CustomerID)
                FROM Reservations r
                WHERE CAST(r.CheckInDate AS DATE) = CAST(GETDATE() AS DATE)
                  AND r.ReservationStatus IN ('Confirmed','Checked-In','Checked In')");
            NewGuestsTodayText.Text = newGuests != null ? Convert.ToInt32(newGuests).ToString() : "0";

            var weekIn = db.ExecuteScalar(@"
                SELECT 
                 COUNT(*)
                FROM Reservations
                WHERE CheckInDate >= DATEADD(DAY, -6, CAST(GETDATE() AS DATE))
                  AND LOWER(ReservationStatus) IN ('confirmed','checked-in','checked in')");
            WeekCheckinsText.Text = weekIn != null ? Convert.ToInt32(weekIn).ToString() : "0";

            var checkouts = db.ExecuteScalar(@"
                SELECT 
                 COUNT(*)
                FROM Reservations
                WHERE CAST(CheckOutDate AS DATE) = CAST(GETDATE() AS DATE)
                  AND LOWER(ReservationStatus) IN ('checked-out','checked out')");
            CheckoutsTodayText.Text = checkouts != null ? Convert.ToInt32(checkouts).ToString() : "0";
        }

        private void LoadRecentActivity(DatabaseHelper db)
        {
            try
            {
                //LoadRecentActivity
                var tbl = db.ExecuteQueryDataTable(@"
                    SELECT TOP 5
                        c.FirstName + ' ' + c.LastName AS Name,
                        r.ReservationStatus,
                        r.ReservationDate
                    FROM Reservations r
                    JOIN Customers c ON r.CustomerID = c.CustomerID
                    ORDER BY r.ReservationDate DESC");

                ActivityListBox.Items.Clear();
                if (tbl == null) return;

                foreach (DataRow row in tbl.Rows)
                {
                    string name = row["Name"].ToString();
                    string status = row["ReservationStatus"].ToString();
                    DateTime dt = Convert.ToDateTime(row["ReservationDate"]);

                    ActivityListBox.Items.Add(new ActivityItem
                    {
                        Description = BuildActivityDescription(name, status),
                        TimeAgo = GetTimeAgo(dt),
                        DotColor = PickDotColor(status)
                    });
                }
            }
            catch (Exception ex) { Console.WriteLine($"[Activity] {ex.Message}"); }
        }

        private static string BuildActivityDescription(string name, string status)
        {
            string lower = status.ToLower();
            if (lower.Contains("checked-in") || lower.Contains("checked in")) return $"{name} checked in";
            if (lower.Contains("checked-out") || lower.Contains("checked out")) return $"{name} checked out";
            if (lower.Contains("confirmed")) return $"New booking confirmed — {name}";
            return $"Reservation updated — {name}";
        }

        private static Brush PickDotColor(string status)
        {
            string lower = status.ToLower();
            if (lower.Contains("checked-in") || lower.Contains("checked in")) return DotBlue;
            if (lower.Contains("checked-out") || lower.Contains("checked out")) return DotGray;
            if (lower.Contains("confirmed")) return DotGreen;
            return DotAmber;
        }

        private static string GetTimeAgo(DateTime dt)
        {
            TimeSpan diff = DateTime.Now - dt;
            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hr ago";
            return $"{(int)diff.TotalDays} day(s) ago";
        }

        // ═══════════════════════════════════════════════════════════════════
        //  MONTHLY CHART — DATA LOAD
        // ═══════════════════════════════════════════════════════════════════

        private void LoadMonthlyChart(DatabaseHelper db)
        {
            try
            {
                //  MONTHLY CHART — DATA LOAD
                int year = DateTime.Now.Year;
                ChartYearLabel.Text = year.ToString();

                var tbl = db.ExecuteQueryDataTable($@"
                    SELECT
                        MONTH(r.CheckInDate)             AS MonthNum,
                        COUNT(DISTINCT r.CustomerID)     AS Guests,
                        ISNULL(SUM(p.AmountPaid), 0)     AS Revenue
                    FROM Reservations r
                    LEFT JOIN Payment p
                           ON p.ReservationID = r.ReservationID
                          AND p.PaymentStatus = 'Completed'
                          AND YEAR(p.PaymentDate) = {year}
                    WHERE YEAR(r.CheckInDate) = {year}
                    GROUP BY MONTH(r.CheckInDate)
                    ORDER BY MonthNum");

                string[] abbr = { "Jan","Feb","Mar","Apr","May","Jun",
                                   "Jul","Aug","Sep","Oct","Nov","Dec" };

                var dict = new Dictionary<int, (int guests, decimal revenue)>();
                if (tbl != null)
                    foreach (DataRow row in tbl.Rows)
                        dict[Convert.ToInt32(row["MonthNum"])] =
                            (Convert.ToInt32(row["Guests"]), Convert.ToDecimal(row["Revenue"]));

                var bars = new List<MonthlyChartBar>();
                for (int m = 1; m <= 12; m++)
                {
                    var (g, rv) = dict.ContainsKey(m) ? dict[m] : (0, 0m);
                    bars.Add(new MonthlyChartBar
                    {
                        MonthLabel = abbr[m - 1],
                        GuestCount = g,
                        Revenue = rv
                    });
                }

                _chartBars = bars;

                // FIX: defer rendering until the canvas has been laid out so
                //      ActualWidth is valid.  BarChartCanvas_SizeChanged will
                //      also call RenderBarChart() once the canvas has a real size.
                Dispatcher.BeginInvoke(
                    DispatcherPriority.Loaded,
                    new Action(RenderBarChart));
            }
            catch (Exception ex) { Console.WriteLine($"[Chart] {ex.Message}"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  MONTHLY CHART — RENDER
        // ═══════════════════════════════════════════════════════════════════

        private void RenderBarChart()
        {
            BarChartCanvas.Children.Clear();
            if (_chartBars == null || _chartBars.Count == 0) return;

            double cw = BarChartCanvas.ActualWidth > 10 ? BarChartCanvas.ActualWidth : 800;
            double ch = BarChartCanvas.ActualHeight > 10 ? BarChartCanvas.ActualHeight : 220;

            const double topPad = 14;
            const double bottomPad = 28;
            double maxBarH = ch - topPad - bottomPad;

            int maxGuests = 1;
            foreach (var b in _chartBars)
                if (b.GuestCount > maxGuests) maxGuests = b.GuestCount;

            int n = _chartBars.Count;
            double barW = (cw - 40) / (n * 1.7);
            double gap = barW * 0.7;
            double totalW = n * barW + (n - 1) * gap;
            double startX = (cw - totalW) / 2.0;
            int today = DateTime.Now.Month;

            // Horizontal grid lines
            for (int i = 1; i <= 4; i++)
            {
                double lineY = topPad + maxBarH - maxBarH * i / 4.0;

                BarChartCanvas.Children.Add(new Line
                {
                    X1 = 0,
                    Y1 = lineY,
                    X2 = cw,
                    Y2 = lineY,
                    Stroke = GridLine,
                    StrokeDashArray = new DoubleCollection { 4, 4 },
                    StrokeThickness = 1
                });

                int labelVal = maxGuests * i / 4;
                var gridLbl = new TextBlock { Text = labelVal.ToString(), FontSize = 9, Foreground = LabelColor };
                Canvas.SetLeft(gridLbl, 2);
                Canvas.SetTop(gridLbl, lineY - 8);
                BarChartCanvas.Children.Add(gridLbl);
            }

            // Bars
            for (int i = 0; i < n; i++)
            {
                var bar = _chartBars[i];
                double barH = maxBarH * bar.GuestCount / (double)maxGuests;
                double x = startX + i * (barW + gap);
                double top = topPad + maxBarH - barH;
                bool isCurr = (i + 1 == today);

                bar.X = x; bar.Width = barW; bar.Top = top; bar.BarH = Math.Max(barH, 2);

                var rect = new Rectangle
                {
                    Width = barW,
                    Height = Math.Max(barH, 2),
                    RadiusX = 4,
                    RadiusY = 4,
                    Fill = isCurr ? BarCurrent : BarNormal
                };

                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, top);

                BarChartCanvas.Children.Add(rect);

                if (bar.GuestCount > 0)
                {
                    var valLbl = new TextBlock
                    {
                        Text = bar.GuestCount.ToString(),
                        FontSize = 9,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = isCurr ? BarCurrent : BarNormal,
                        Width = barW + 10,
                        TextAlignment = TextAlignment.Center
                    };
                    Canvas.SetLeft(valLbl, x - 5);
                    Canvas.SetTop(valLbl, top - 14);
                    BarChartCanvas.Children.Add(valLbl);
                }

                var monthLbl = new TextBlock
                {
                    Text = bar.MonthLabel,
                    FontSize = 10,
                    Foreground = isCurr ? BarCurrent : LabelColor,
                    FontWeight = isCurr ? FontWeights.SemiBold : FontWeights.Normal,
                    Width = barW + 10,
                    TextAlignment = TextAlignment.Center
                };
                Canvas.SetLeft(monthLbl, x - 5);
                Canvas.SetTop(monthLbl, ch - bottomPad + 4);
                BarChartCanvas.Children.Add(monthLbl);
            }
        }

        private void BarChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RenderBarChart();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  CHART TOOLTIP
        // ═══════════════════════════════════════════════════════════════════

        private void BarChart_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_chartBars == null || _chartBars.Count == 0) return;

            var pos = e.GetPosition(BarChartCanvas);

            MonthlyChartBar hit = null;
            foreach (var bar in _chartBars)
            {
                if (pos.X >= bar.X && pos.X <= bar.X + bar.Width
                 && pos.Y >= bar.Top && pos.Y <= bar.Top + bar.BarH)
                { hit = bar; break; }
            }

            if (hit != null)
            {
                TooltipMonth.Text = $"{hit.MonthLabel} {DateTime.Now.Year}";
                TooltipGuests.Text = $"👤  {hit.GuestCount} guest(s)";
                TooltipRevenue.Text = $"💰  ₱{hit.Revenue:N0}";

                double tx = Math.Min(pos.X, BarChartCanvas.ActualWidth - 160);
                ChartTooltip.Margin = new Thickness(Math.Max(tx, 0), 6, 0, 0);
                ChartTooltip.Visibility = Visibility.Visible;
            }
            else
            {
                ChartTooltip.Visibility = Visibility.Collapsed;
            }
        }

        private void BarChart_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ChartTooltip.Visibility = Visibility.Collapsed;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  HOUSEKEEPING
        // ═══════════════════════════════════════════════════════════════════

        private void LoadHousekeeping(DatabaseHelper db)
        {
            try
            {
                //  HOUSEKEEPING
                var tbl = db.ExecuteQueryDataTable(@"
                    SELECT TOP 5
                        rm.RoomNumber,
                        rt.TypeName,
                        h.CleaningStatus
                    FROM HouseKeeping h
                    JOIN Rooms     rm ON h.RoomID     = rm.RoomID
                    JOIN RoomTypes rt ON rm.RoomTypeID = rt.RoomTypeID
                    WHERE h.CleaningStatus != 'Completed'
                    ORDER BY h.CleaningDate DESC");

                HousekeepingListBox.Items.Clear();

                if (tbl != null && tbl.Rows.Count > 0)
                {
                    foreach (DataRow row in tbl.Rows)
                        HousekeepingListBox.Items.Add(new HousekeepingItem
                        {
                            RoomInfo = $"{row["TypeName"]} #{row["RoomNumber"]}",
                            Status = row["CleaningStatus"].ToString()
                        });
                }
                else
                {
                    HousekeepingListBox.Items.Add(new HousekeepingItem
                    { RoomInfo = "All rooms are clean", Status = "Done" });
                }
            }
            catch (Exception ex) { Console.WriteLine($"[Housekeeping] {ex.Message}"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  INVENTORY ALERTS
        // ═══════════════════════════════════════════════════════════════════

        private void LoadInventoryAlerts(DatabaseHelper db)
        {
            try
            {
                //var tbl = db.ExecuteQueryDataTable(@"
                //    SELECT TOP 5 ItemName, QuantityInStock, ReorderLevel
                //    FROM Inventory
                //    WHERE QuantityInStock <= ReorderLevel
                //      AND Status = 'Active'
                //    ORDER BY QuantityInStock ASC");

                //  INVENTORY ALERTS
                var tbl = db.ExecuteQueryDataTable(@"
                    SELECT TOP 5 ItemName, QuantityInStock, ReorderLevel
                  FROM Inventory
                  WHERE QuantityInStock <= ReorderLevel
                    AND Status = 'NotAvailable'
                  ORDER BY QuantityInStock ASC");

                InventoryAlertListBox.Items.Clear();

                if (tbl != null && tbl.Rows.Count > 0)
                {
                    foreach (DataRow row in tbl.Rows)
                    {
                        int stock = Convert.ToInt32(row["QuantityInStock"]);
                        int reorder = Convert.ToInt32(row["ReorderLevel"]);
                        InventoryAlertListBox.Items.Add(new InventoryAlertItem
                        {
                            ItemName = row["ItemName"].ToString(),
                            StockInfo = $"{stock} left (min {reorder})"
                        });
                    }
                }
                else
                {
                    InventoryAlertListBox.Items.Add(new InventoryAlertItem
                    { ItemName = "No low stock alerts", StockInfo = "All good" });
                }
            }
            catch (Exception ex) { Console.WriteLine($"[Inventory] {ex.Message}"); }
        }
    }
}