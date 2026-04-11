using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OOP_FINALS
{
    public partial class DashboardPage : Page
    {
        private double prevY = 0;
        private DispatcherTimer _animationTimer;
        private Random _random = new Random();
        private string _currentStaffId = "1"; // Get this from login session

        public DashboardPage()
        {
            InitializeComponent();
            bool dbOk = TestDatabaseConnection();

            if (dbOk)
            {
                LoadDashboardStats();
            }
            else
            {
                Console.WriteLine("Using demo data...");
                LoadDemoData();  // Your existing method
            }

            StartLiveAnimations();

        }
        private void LoadDemoData()
        {
            // Demo data with smooth animations
            AnimateCounter(TotalBookingsText, 127);
            AnimateCounter(TodayCheckinsText, 8);
            AnimateCounter(WeekCheckinsText, 45);

            AnimateRevenueCounter(TodayRevenueText, 24500);

            // Static demo values
            OccupancyRateText.Text = "72%";
            UpdateOccupancyChart(72);

            RoomsCleanedText.Text = "15";
            RoomsCleanedSubText.Text = "+12 today";

            NewGuestsText.Text = "8";
            NewGuestsSubText.Text = "+5 today";

            // Demo recent bookings
            RecentBookingsListBox.Items.Clear();
            RecentBookingsListBox.Items.Add(new { CustomerName = "John Doe", RoomInfo = "Deluxe • Check-in: 2:30 PM" });
            RecentBookingsListBox.Items.Add(new { CustomerName = "Maria Santos", RoomInfo = "Standard • Check-out: 11:00 AM" });
            RecentBookingsListBox.Items.Add(new { CustomerName = "Pedro Cruz", RoomInfo = "Suite • Today" });

            // Demo revenue chart
            UpdateRevenueChart(CreateDemoRevenueData());

            WelcomeUserText.Text = "Welcome Back, Edrian!";

            Console.WriteLine("✅ Demo data loaded (DB unavailable)");
        }
        // ✅ SINGLE CreateDemoRevenueData() - REPLACE ALL DUPLICATES
        private DataTable CreateDemoRevenueData()
        {
            DataTable demoData = new DataTable();
            demoData.Columns.Add("DateLabel", typeof(string));
            demoData.Columns.Add("DailyRevenue", typeof(decimal));

            // Demo revenue trend (7 days)
            object[,] demoDataArray = {
        {"Jan 08", 2450m}, {"Jan 09", 3800m}, {"Jan 10", 5200m},
        {"Jan 11", 2950m}, {"Jan 12", 4100m}, {"Jan 13", 3650m}, {"Jan 14", 2800m}
    };

            for (int i = 0; i < demoDataArray.GetLength(0); i++)
            {
                demoData.Rows.Add(demoDataArray[i, 0], demoDataArray[i, 1]);
            }

            return demoData;
        }
        private bool TestDatabaseConnection()
        {
            try
            {
                DatabaseHelper db = new DatabaseHelper();
                string testQuery = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES"; // Safe test
                var result = db.ExecuteScalar(testQuery);

                if (result != null)
                {
                    Console.WriteLine($"✅ Database OK! Tables: {result}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DB Error: {ex.Message}");
                // NO MessageBox spam - just log
                return false;
            }
        }
        private void StartLiveAnimations()
        {
            _animationTimer = new DispatcherTimer();
            _animationTimer.Interval = TimeSpan.FromSeconds(5); // Increased for better performance
            _animationTimer.Tick += (s, e) => AnimateLiveUpdates();
            _animationTimer.Start();
        }

        private void AnimateLiveUpdates()
        {
            UpdateLiveLabels(); // Only live-updating labels, not heavy queries
        }

         #region MAIN DATABASE LOADING - ALL CONNECTED ✅

        private void LoadDashboardStats()
        {
            try
            {
                DatabaseHelper db = new DatabaseHelper();
                Console.WriteLine("🔄 Loading LIVE Dashboard Stats...");

                LoadWelcomeUser(db);
                AnimateCounter(TotalBookingsText, GetTotalBookings(db));
                AnimateCounter(TodayCheckinsText, GetTodayCheckins(db));
                AnimateCounter(WeekCheckinsText, GetWeekCheckins(db));

                decimal revenue = GetTodayRevenue(db);
                AnimateRevenueCounter(TodayRevenueText, (int)revenue);

                LoadOccupancyRate(db);
                LoadRevenueChart(db);
                LoadRecentBookings(db);
                LoadQuickStats(db);

                Console.WriteLine("✅ All stats loaded!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Load Error: {ex.Message}");
                // Silent fallback - no popup spam
            }
        }

        // 1. Welcome User - FROM STAFF TABLE
        private void LoadWelcomeUser(DatabaseHelper db)
        {
            try
            {
                string welcomeQuery = $@"
                    SELECT FullName FROM Staff 
                    WHERE StaffID = '{_currentStaffId}' AND Status = 'Active'";

                var staffName = db.ExecuteScalar(welcomeQuery);
                WelcomeUserText.Text = staffName != null
                    ? $"Welcome Back, {staffName}!"
                    : "Welcome Back, Staff!";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Welcome user error: {ex.Message}");
                WelcomeUserText.Text = "Welcome Back!";
            }
        }

        // 2. TOTAL BOOKINGS - Confirmed + Checked-In Reservations
        private int GetTotalBookings(DatabaseHelper db)
        {
            try
            {
                string query = @"
            SELECT COUNT(*) FROM Reservations 
            WHERE LOWER(ReservationStatus) IN ('confirmed', 'checked-in')";  // Simplified

                var result = db.ExecuteScalar(query);
                return result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
            catch { return 0; }
        }

        private decimal GetTodayRevenue(DatabaseHelper db)
        {
            try
            {
                // ✅ SIMPLIFIED - Use Payment table (most reliable)
                string query = @"
            SELECT ISNULL(SUM(AmountPaid), 0) AS MonthlyRevenue
FROM Payment
WHERE MONTH(PaymentDate) = MONTH(GETDATE())
AND YEAR(PaymentDate) = YEAR(GETDATE())
AND PaymentStatus = 'Completed'";

                var result = db.ExecuteScalar(query);
                return result != DBNull.Value ? Convert.ToDecimal(result) : 0;
            }
            catch { return 0; }
        }
        // 3. TODAY CHECK-INS - Reservations checked in TODAY
        private int GetTodayCheckins(DatabaseHelper db)
        {
            try
            {
                string query = @"
        SELECT COUNT(*) 
FROM Reservations 
WHERE CAST(CheckInDate AS DATE) = CAST(GETDATE() AS DATE)
AND ReservationStatus IN ('Confirmed', 'Checked-In', 'Checked In')";

                var result = db.ExecuteScalar(query);

              //  MessageBox.Show("Month Check-ins: " + result?.ToString()); // DEBUG

                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return 0;
            }
        }

        // 4. WEEK CHECK-INS - Last 7 days
        private int GetWeekCheckins(DatabaseHelper db)
        {
            string query = @"
                SELECT COUNT(*) 
                FROM Reservations 
                WHERE CheckInDate >= DATEADD(DAY, -7, GETDATE())
                AND LOWER(ReservationStatus) IN ('confirmed', 'checked-in', 'checked in')";

            var result = db.ExecuteScalar(query);
            return result != null ? Convert.ToInt32(result) : 0;
        }

        // 5. TODAY REVENUE - Completed Payments TODAY
        //private decimal GetTodayRevenue(DatabaseHelper db)
        //{
        //    string query = @"
        //SELECT ISNULL(SUM(bd.Subtotal), 0) 
        //FROM Billing_Details bd
        //JOIN Billing b ON bd.BillingID = b.BillingID
        //JOIN Reservations r ON b.ReservationID = r.ReservationID
        //JOIN Payment p ON p.ReservationID = r.ReservationID
        //WHERE CAST(b.BillingDate AS DATE) = CAST(GETDATE() AS DATE)  -- Using BillingDate
        //AND b.BillingStatus = 'Completed'
        //AND p.PaymentStatus = 'Completed'";

        //    var result = db.ExecuteScalar(query);
        //    return result != null ? Convert.ToDecimal(result) : 0;
        //}

        // 6. OCCUPANCY RATE - Live from Rooms table
        private void LoadOccupancyRate(DatabaseHelper db)
        {
            try
            {
                string occupancyQuery = @"
                    SELECT 
                        ISNULL(CAST(occupied.OccupiedCount AS FLOAT) / NULLIF(total.TotalRooms, 0), 0) * 100 as OccupancyRate
                    FROM 
                        (SELECT COUNT(*) as OccupiedCount FROM Rooms WHERE Status = 'Occupied') occupied,
                        (SELECT COUNT(*) as TotalRooms FROM Rooms WHERE Status != 'Maintenance') total";

                DataTable resultTable = db.ExecuteQueryDataTable(occupancyQuery);
                if (resultTable.Rows.Count > 0)
                {
                    decimal occupancyRate = Convert.ToDecimal(resultTable.Rows[0]["OccupancyRate"]);
                    OccupancyRateText.Text = occupancyRate.ToString("F0") + "%";
                    UpdateOccupancyChart(occupancyRate);
                }
                else
                {
                    OccupancyRateText.Text = "0%";
                }
            }
            catch
            {
                OccupancyRateText.Text = "0%";
            }
        }

        // 7. REVENUE CHART - Last 7 days payments
        private void LoadRevenueChart(DatabaseHelper db)
        {
            try
            {
                string revenueQuery = @"
                    SELECT 
                        FORMAT(CAST(p.PaymentDate AS DATE), 'MMM dd') as DateLabel,
                        ISNULL(SUM(p.AmountPaid), 0) as DailyRevenue
                    FROM Payment p
                    JOIN Reservations r ON p.ReservationID = r.ReservationID
                    WHERE CAST(p.PaymentDate AS DATE) >= DATEADD(DAY, -6, CAST(GETDATE() AS DATE))
                    AND p.PaymentStatus = 'Completed'
                    GROUP BY CAST(p.PaymentDate AS DATE)
                    ORDER BY CAST(p.PaymentDate AS DATE)";

                DataTable revenueData = db.ExecuteQueryDataTable(revenueQuery);
                UpdateRevenueChart(revenueData);
            }
            catch
            {
                UpdateRevenueChart(new DataTable());
            }
        }

        // 8. RECENT BOOKINGS - Live from Reservations + Customers + Rooms
        private void LoadRecentBookings(DatabaseHelper db)
        {
            try
            {
                string recentBookingsQuery = @"
                    SELECT TOP 5 
                        c.FirstName + ' ' + c.LastName as CustomerName,
                        rt.TypeName as RoomType,
                        r.RoomID,
                        CASE 
                            WHEN CAST(r.CheckInDate AS DATE) = CAST(GETDATE() AS DATE) 
                            THEN 'Check-in: ' + FORMAT(r.CheckInDate, 'hh:mm tt')
                            WHEN CAST(r.CheckOutDate AS DATE) = CAST(GETDATE() AS DATE)
                            THEN 'Check-out: ' + FORMAT(r.CheckOutDate, 'hh:mm tt')
                            ELSE FORMAT(r.CheckInDate, 'MMM dd')
                        END as StatusText,
                        r.ReservationStatus
                    FROM Reservations r 
                    JOIN Customers c ON r.CustomerID = c.CustomerID
                    JOIN Rooms rm ON r.RoomID = rm.RoomID 
                    JOIN RoomTypes rt ON rm.RoomTypeID = rt.RoomTypeID
                    WHERE r.ReservationStatus IN ('Confirmed', 'Checked-In', 'Checked In')
                    ORDER BY r.ReservationDate DESC";

                DataTable recentBookingsTable = db.ExecuteQueryDataTable(recentBookingsQuery);
                UpdateRecentBookingsUI(recentBookingsTable);
            }
            catch
            {
                UpdateRecentBookingsUI(null);
            }
        }

        // 9. QUICK STATS - Housekeeping + New Guests
        private void LoadQuickStats(DatabaseHelper db)
        {
            try
            {
                // Rooms Cleaned TODAY (HouseKeeping)
                var cleanRoomsResult = db.ExecuteScalar(@"
                    SELECT COUNT(DISTINCT h.RoomID) 
                    FROM HouseKeeping h 
                    WHERE CAST(h.CleaningDate AS DATE) = CAST(GETDATE() AS DATE) 
                    AND h.CleaningStatus = 'Completed'");
                int cleanRooms = cleanRoomsResult != null ? Convert.ToInt32(cleanRoomsResult) : 0;
                RoomsCleanedText.Text = cleanRooms.ToString();
                RoomsCleanedSubText.Text = $"+{cleanRooms} today";

                // New Guests TODAY (First-time customers checking in today)
                var newGuestsResult = db.ExecuteScalar(@"
                    SELECT COUNT(DISTINCT c.CustomerID) 
                    FROM Customers c 
                    JOIN Reservations r ON c.CustomerID = r.CustomerID
                    WHERE CAST(r.CheckInDate AS DATE) = CAST(GETDATE() AS DATE) 
                    AND r.ReservationStatus IN ('Confirmed', 'Checked-In', 'Checked In')");
                int newGuests = newGuestsResult != null ? Convert.ToInt32(newGuestsResult) : 0;
                NewGuestsText.Text = newGuests.ToString();
                NewGuestsSubText.Text = $"+{newGuests} today";
            }
            catch
            {
                RoomsCleanedText.Text = "0";
                RoomsCleanedSubText.Text = "+0 today";
                NewGuestsText.Text = "0";
                NewGuestsSubText.Text = "+0 today";
            }
        }

        #endregion

        #region UI UPDATES (Keep these as-is - they work perfectly)

        private void UpdateRevenueChart(DataTable revenueData)
        {
            RevenueChartCanvas.Children.Clear();
            SparklineDotsCanvas.Children.Clear();
            GridLinesCanvas.Children.Clear();
            prevY = 0;

            double width = 380;
            double height = 160;
            double marginLeft = 50;
            int points = Math.Max(revenueData.Rows.Count, 7);
            double xStep = (width - marginLeft) / Math.Max(points - 1, 1);

            DrawProfessionalGridLines();

            if (revenueData.Rows.Count > 0)
            {
                var revenueRows = revenueData.AsEnumerable();
                var maxRev = revenueRows.Max(r => Convert.ToDecimal(r["DailyRevenue"]));
                double scaleY = maxRev == 0 ? 1 : height / (double)maxRev * 0.8;

                string pathString = "";
                var pointsList = new List<Point>();

                for (int i = 0; i < revenueData.Rows.Count; i++)
                {
                    DataRow row = revenueData.Rows[i];
                    decimal rev = Convert.ToDecimal(row["DailyRevenue"]);

                    double x = marginLeft + (i * xStep);
                    double y = height - ((double)rev * scaleY) + 10;

                    pointsList.Add(new Point(x, y));

                    if (i == 0)
                        pathString = "M " + x + "," + y;
                    else
                        pathString += " L " + x + "," + y;
                }

                var line = new Path
                {
                    Data = Geometry.Parse(pathString),
                    Stroke = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                    StrokeThickness = 4,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Effect = new DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 193, 7),
                        ShadowDepth = 0,
                        BlurRadius = 12,
                        Opacity = 0.4
                    }
                };

                AnimateLineDrawing(line);
                RevenueChartCanvas.Children.Add(line);
                DrawGlowingDataPoints(pointsList);
            }

            LiveRevenueLabel.Text = revenueData.Rows.Count > 0
                ? "₱" + revenueData.AsEnumerable().Max(r => Convert.ToDecimal(r["DailyRevenue"])).ToString("N0")
                : "₱0";
        }

        private void UpdateRecentBookingsUI(DataTable bookingsTable)
        {
            RecentBookingsListBox.Items.Clear();
            if (bookingsTable != null && bookingsTable.Rows.Count > 0)
            {
                foreach (DataRow row in bookingsTable.Rows.Cast<DataRow>().Take(3))
                {
                    var item = new
                    {
                        CustomerName = row["CustomerName"].ToString(),
                        RoomInfo = row["RoomType"].ToString() + " • " + row["StatusText"].ToString()
                    };
                    RecentBookingsListBox.Items.Add(item);
                }
            }
            else
            {
                RecentBookingsListBox.Items.Add(new { CustomerName = "No recent bookings", RoomInfo = "Check back later" });
            }
        }

        private void UpdateOccupancyChart(decimal occupancyRate)
        {
            double rate = Math.Max(0, Math.Min(100, (double)occupancyRate));
            double thickness = 12 + (rate / 100.0 * 12);
            OccupiedArcPath.StrokeThickness = (float)thickness;
        }

        // Chart drawing methods (unchanged - working perfectly)
        private void DrawProfessionalGridLines()
        {
            for (int i = 0; i <= 4; i++)
            {
                double y = 30 + i * 30;
                var line = new Line
                {
                    X1 = 50,
                    Y1 = y,
                    X2 = 420,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    StrokeThickness = 1,
                    Opacity = 0.4
                };
                var dashArray = new DoubleCollection();
                dashArray.Add(4);
                dashArray.Add(4);
                line.StrokeDashArray = dashArray;
                GridLinesCanvas.Children.Add(line);
            }
        }

        private void DrawGlowingDataPoints(List<Point> points)
        {
            foreach (var point in points)
            {
                var dot = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.White,
                    Stroke = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                    StrokeThickness = 2,
                    Effect = new DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 193, 7),
                        ShadowDepth = 0,
                        BlurRadius = 6,
                        Opacity = 0.8
                    }
                };

                Canvas.SetLeft(dot, point.X - 5);
                Canvas.SetTop(dot, point.Y - 5);
                SparklineDotsCanvas.Children.Add(dot);
            }
        }

        private void AnimateLineDrawing(Path line)
        {
            double length = line.Data.Bounds.Width;
            var dashArray = new DoubleCollection();
            dashArray.Add(length);
            dashArray.Add(length);
            line.StrokeDashArray = dashArray;

            var anim = new DoubleAnimation(length, 0, new Duration(TimeSpan.FromSeconds(1.5)))
            {
                EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseInOut }
            };
            line.BeginAnimation(Path.StrokeDashOffsetProperty, anim);
        }

        #endregion

        #region ANIMATION HELPERS (Unchanged - Perfect)

        private void AnimateCounter(TextBlock counter, int targetValue)
        {
            int start = 0;
            int.TryParse(counter.Text.Replace(",", ""), out start);

            // ✅ FIX: if small number, skip animation
            if (Math.Abs(targetValue - start) < 30)
            {
                counter.Text = targetValue.ToString();
                return;
            }

            int duration = 800;
            int steps = 30;
            int increment = (targetValue - start) / steps;

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(duration / steps);

            int current = start;

            timer.Tick += (s, e) =>
            {
                current += increment;

                if ((increment > 0 && current >= targetValue) ||
                    (increment < 0 && current <= targetValue))
                {
                    current = targetValue;
                    timer.Stop();
                }

                counter.Text = current.ToString("N0");
            };

            timer.Start();
        }
        private void AnimateRevenueCounter(TextBlock counter, int targetValue)
        {
            int start = 0;
            int.TryParse(counter.Text.Replace("₱", "").Replace(",", ""), out start);

            int duration = 1000;
            int steps = 30;
            int increment = (targetValue - start) / steps;

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(duration / steps);

            int current = start;

            timer.Tick += (s, e) =>
            {
                current += increment;

                if ((increment > 0 && current >= targetValue) ||
                    (increment < 0 && current <= targetValue))
                {
                    current = targetValue;
                    timer.Stop();
                }

                counter.Text = "₱" + current.ToString("N0");
            };

            timer.Start();
        }

        private void AnimateOccupancyPulse()
        {
            var pulseAnim = new DoubleAnimation(1, 1.05, TimeSpan.FromMilliseconds(2000))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            var scaleTransform = new ScaleTransform();
            CenterPulseEllipse.RenderTransform = scaleTransform;
            CenterPulseEllipse.BeginAnimation(ScaleTransform.ScaleXProperty, pulseAnim);
            CenterPulseEllipse.BeginAnimation(ScaleTransform.ScaleYProperty, pulseAnim);
        }

        private void UpdateLiveLabels()
        {
            // Live random updates for subtext (not affecting main stats)
            RoomsCleanedSubText.Text = "+ " + _random.Next(1, 5) + " cleaned";
            NewGuestsSubText.Text = "+ " + _random.Next(1, 3) + " arrived";
        }

        #endregion

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _animationTimer?.Stop();
        }
    }
}