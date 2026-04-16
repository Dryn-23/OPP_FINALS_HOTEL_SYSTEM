using CustomerDashboard;
using MahApps.Metro.IconPacks;
using System;

using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace OOP_FINALS
{
    public partial class MainDashboard : Window
    {
        // ✅ YOUR CONNECTION STRING
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["MyDBConnectionString"].ConnectionString;

        private bool isSidebarCollapsed = false;
        private bool _isMaximized = false;

        public MainDashboard()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                StartSkeletonAnimation();  // ✅ EXISTS
                StartSidebarLoadingAnimation();
            };

            AdjustWindowToScreen();
            _ = LoadInitialDashboardAsync();
        }

        // ========== SIMPLIFIED DATABASE (ADO.NET ONLY) ==========
        private async Task<int> GetCustomerCountAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Customers", connection))
                    {
                        return (int)await cmd.ExecuteScalarAsync();
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private async Task<int> GetActiveReservationsCountAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var cmd = new SqlCommand(@"
                        SELECT COUNT(*) FROM Reservations 
                        WHERE ReservationStatus IN ('Confirmed', 'CheckedIn')", connection))
                    {
                        return (int)await cmd.ExecuteScalarAsync();
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private async Task<decimal> GetTotalRevenueAsync()
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var cmd = new SqlCommand(@"
                        SELECT ISNULL(SUM(FinalAmount), 0) FROM Billing 
                        WHERE BillingStatus = 'Paid'", connection))
                    {
                        return (decimal)await cmd.ExecuteScalarAsync();
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        // ========== YOUR EXISTING ANIMATIONS (UNCHANGED) ==========
        private void StartSkeletonAnimation()
        {
            CreateShimmerEffect();
            StartShimmerAnimation();

            //AnimateBar(HeaderBar, 0.1);
            //AnimateBar(StatBar1, 0.3);
            //AnimateBar(StatBar2, 0.45);
            //AnimateBar(StatBar3, 0.6);
            //AnimateBar(ActivityBar, 0.8);
        }

        private void CreateShimmerEffect()
        {
            var shimmerBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0.5),
                EndPoint = new Point(1, 0.5)
            };
            shimmerBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 0));
            shimmerBrush.GradientStops.Add(new GradientStop(Color.FromArgb(100, 255, 255, 255), 0.4));
            shimmerBrush.GradientStops.Add(new GradientStop(Color.FromArgb(150, 255, 255, 255), 0.5));
            shimmerBrush.GradientStops.Add(new GradientStop(Color.FromArgb(100, 255, 255, 255), 0.6));
            shimmerBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 1));

            //ShimmerBackground.Fill = shimmerBrush;
        }

        private void StartShimmerAnimation()
        {
            var shimmerAnim = new DoubleAnimation(-350, 600, TimeSpan.FromSeconds(2));
            shimmerAnim.RepeatBehavior = RepeatBehavior.Forever;
            //ShimmerTranslate.BeginAnimation(TranslateTransform.XProperty, shimmerAnim);
        }

        private void AnimateBar(FrameworkElement bar, double delay)
        {
            if (bar == null) return;

            var slideAnim = new DoubleAnimation(-80, 0, new Duration(TimeSpan.FromSeconds(0.6)))
            {
                BeginTime = TimeSpan.FromSeconds(delay)
            };
            var fadeAnim = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.4)))
            {
                BeginTime = TimeSpan.FromSeconds(delay)
            };

            bar.RenderTransform = new TranslateTransform(-80, 0);
            bar.RenderTransformOrigin = new Point(0.5, 0.5);

            ((TranslateTransform)bar.RenderTransform).BeginAnimation(TranslateTransform.XProperty, slideAnim);
            bar.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
        }

        // ========== SIDEBAR LOADING (UNCHANGED) ==========
        private void StartSidebarLoadingAnimation()
        {
            var pulseAnim = new DoubleAnimation(0.7, 1.2, TimeSpan.FromSeconds(1.5));
            pulseAnim.RepeatBehavior = RepeatBehavior.Forever;
            pulseAnim.AutoReverse = true;
            SidebarShimmerScale.BeginAnimation(ScaleTransform.ScaleXProperty, pulseAnim);
            SidebarShimmerScale.BeginAnimation(ScaleTransform.ScaleYProperty, pulseAnim);

            AnimateLoadingDots();

            SidebarLoadingText.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.8));
            SidebarLoadingText.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void AnimateLoadingDots()
        {
            var delay = 0.2;
            var anim1 = new DoubleAnimation(1, 0.2, TimeSpan.FromSeconds(0.8)) { RepeatBehavior = RepeatBehavior.Forever, BeginTime = TimeSpan.FromSeconds(delay * 0) };
            var anim2 = new DoubleAnimation(1, 0.2, TimeSpan.FromSeconds(0.8)) { RepeatBehavior = RepeatBehavior.Forever, BeginTime = TimeSpan.FromSeconds(delay * 1) };
            var anim3 = new DoubleAnimation(1, 0.2, TimeSpan.FromSeconds(0.8)) { RepeatBehavior = RepeatBehavior.Forever, BeginTime = TimeSpan.FromSeconds(delay * 2) };

            Dot1.BeginAnimation(OpacityProperty, anim1);
            Dot2.BeginAnimation(OpacityProperty, anim2);
            Dot3.BeginAnimation(OpacityProperty, anim3);
        }

        // ========== LOADING SEQUENCE WITH DB ==========
        private async Task LoadInitialDashboardAsync()
        {
            try
            {
                SidebarLoadingText.Text = "Connecting to database...";
                await Task.Delay(800);

                // Load real stats
                var customerCount = await GetCustomerCountAsync();
                var reservationCount = await GetActiveReservationsCountAsync();
                var revenue = await GetTotalRevenueAsync();

                SidebarLoadingText.Text = "Loading dashboard...";
                await Task.Delay(1200);

                await HideSidebarLoadingAsync();
                await HideLoadingOverlayAsync();

                // ✅ Use DEFAULT constructor (no parameters)
                MainFrame.Navigate(new AdminMainDashboard.DashboardPage());
                MainFrame.Visibility = Visibility.Visible;

                SetActiveMenuButton(IconDashboard, TextDashboard);
            }
            catch
            {
                // Fallback
                await Task.Delay(1500);
                await HideSidebarLoadingAsync();
                await HideLoadingOverlayAsync();
                MainFrame.Navigate(new AdminMainDashboard.DashboardPage());
                SetActiveMenuButton(IconDashboard, TextDashboard);
            }
        }

        private async Task HideSidebarLoadingAsync()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.4));
            fadeOut.Completed += (s, e) =>
            {
                SidebarLoadingOverlay.Visibility = Visibility.Collapsed;
                SidebarContent.Visibility = Visibility.Visible;
                ShowSidebarContentWithAnimation();
            };
            SidebarLoadingOverlay.BeginAnimation(OpacityProperty, fadeOut);
            await Task.Delay(400);
        }

        private void ShowSidebarContentWithAnimation()
        {
            SidebarContent.Opacity = 0;
            SidebarContent.Margin = new Thickness(0, -30, 0, 0);
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.6));
            var slideIn = new ThicknessAnimation(new Thickness(0, -30, 0, 0), new Thickness(0, 20, 0, 0), TimeSpan.FromSeconds(0.6));
            SidebarContent.BeginAnimation(OpacityProperty, fadeIn);
            SidebarContent.BeginAnimation(MarginProperty, slideIn);
        }

        // ========== ALL YOUR NAVIGATION (DEFAULT CONSTRUCTORS) ==========
        private async void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenuButton(IconDashboard, TextDashboard);
            await ShowPageLoadingAsync("Loading Dashboard...");
            MainFrame.Navigate(new AdminMainDashboard.DashboardPage());  // ✅ DEFAULT constructor
            await HideLoadingOverlayAsync();
        }

        private async void Customers_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenuButton(IconCustomers, TextCustomers);
            await ShowPageLoadingAsync("Loading Customers...");
            MainFrame.Navigate(new CustomerPage());  // ✅ DEFAULT constructor
            await HideLoadingOverlayAsync();
        }

        private async void Bookings_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenuButton(IconBookings, TextBookings);
            await ShowPageLoadingAsync("Loading Bookings...");
            MainFrame.Navigate(new BookingsPage());  // ✅ DEFAULT constructor
            await HideLoadingOverlayAsync();
        }

        private async void Payment_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenuButton(IconPayment, TextPayment);
            await ShowPageLoadingAsync("Loading Payments...");
            MainFrame.Navigate(new PaymentPage());
            await HideLoadingOverlayAsync();
        }

        // ... (keep all other navigation methods the same)

        // ========== ALL YOUR EXISTING METHODS (WINDOW CONTROLS, ANIMATIONS) ==========
        private async Task HideLoadingOverlayAsync()
        {
            var fadeOut = new DoubleAnimation(1.0, 0.0, TimeSpan.FromSeconds(0.5));
            LoadingOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            fadeOut.Completed += (s, e) => { LoadingOverlay.Visibility = Visibility.Collapsed; LoadingOverlay.Opacity = 1.0; };
            await Task.Delay(500);
        }

        private async Task ShowPageLoadingAsync(string loadingText = "Loading...")
        {
            //LoadingText.Text = loadingText;
            LoadingOverlay.Visibility = Visibility.Visible;
            LoadingOverlay.Opacity = 1.0;
            await Task.Delay(800);
        }

        // Window controls, sidebar toggle, SetActiveMenuButton, ResetMenuButtons, etc.
        // (ALL YOUR EXISTING CODE - NO CHANGES NEEDED)
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) ToggleMaximize();
            else if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void MaximizeButton_Click(object sender, RoutedEventArgs e) => ToggleMaximize();
        private void CloseButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void ToggleMaximize()
        {
            _isMaximized = !_isMaximized;
            WindowState = _isMaximized ? WindowState.Maximized : WindowState.Normal;
            MaximizeIcon.Kind = _isMaximized ? PackIconMaterialKind.WindowRestore : PackIconMaterialKind.WindowMaximize;
            MainBorder.CornerRadius = _isMaximized ? new CornerRadius(0) : new CornerRadius(30);
            if (!_isMaximized) AdjustWindowToScreen();
        }

        private void AdjustWindowToScreen()
        {
            var screen = SystemParameters.WorkArea;
            Width = Math.Max(1080, screen.Width * 0.8);
            Height = Math.Max(720, screen.Height * 0.8);
            MinWidth = Math.Max(900, screen.Width * 0.6);
            MinHeight = Math.Max(600, screen.Height * 0.6);
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && !_isMaximized) DragMove();
        }

        private void Button_Click(object sender, RoutedEventArgs e) => AnimateSidebarToggle();

        private void AnimateSidebarToggle()
        {
            double targetWidth = isSidebarCollapsed ? 250 : 70;
            double targetOpacity = isSidebarCollapsed ? 1 : 0;

            SidebarWrapper.BeginAnimation(WidthProperty, new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            });

            if (SidebarContent.Visibility == Visibility.Visible)
            {
                AnimateElementOpacity(TextDashboard, targetOpacity);
                AnimateElementOpacity(TextBookings, targetOpacity);
                AnimateElementOpacity(TextRooms, targetOpacity);
                AnimateElementOpacity(TextCustomers, targetOpacity);
                AnimateElementOpacity(TextPayment, targetOpacity);
                AnimateElementOpacity(TextStaff, targetOpacity);
                AnimateElementOpacity(TextReport, targetOpacity);
                AnimateElementOpacity(TextSettings, targetOpacity);
                AnimateElementOpacity(TextLogout, targetOpacity);
            }
            isSidebarCollapsed = !isSidebarCollapsed;
        }

        private void AnimateElementOpacity(FrameworkElement element, double targetOpacity)
        {
            if (element != null)
                element.BeginAnimation(OpacityProperty, new DoubleAnimation
                {
                    To = targetOpacity,
                    Duration = TimeSpan.FromSeconds(0.25),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                });
        }

        private void SetActiveMenuButton(PackIconMaterial icon, TextBlock text)
        {
            ResetMenuButtons();
            if (icon != null)
            {
                icon.Foreground = Brushes.White;
                icon.Effect = new DropShadowEffect { Color = Colors.White, ShadowDepth = 0, BlurRadius = 8, Opacity = 0.8 };
            }
            if (text != null) { text.Foreground = Brushes.White; text.FontWeight = FontWeights.Bold; }
        }

        private void ResetMenuButtons()
        {
            var icons = new[] { IconDashboard, IconBookings, IconRooms, IconCustomers, IconPayment, IconStaff, IconReport, IconSettings, IconLogout };
            var texts = new[] { TextDashboard, TextBookings, TextRooms, TextCustomers, TextPayment, TextStaff, TextReport, TextSettings, TextLogout };

            foreach (var icon in icons) if (icon != null) { icon.Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)); icon.Effect = null; }
            foreach (var text in texts) if (text != null) { text.Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)); text.FontWeight = FontWeights.Normal; }
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (WindowState == WindowState.Maximized)
            {
                _isMaximized = true;
                MaximizeIcon.Kind = PackIconMaterialKind.WindowRestore;
                MainBorder.CornerRadius = new CornerRadius(0);
            }
            else if (WindowState == WindowState.Normal)
            {
                _isMaximized = false;
                MaximizeIcon.Kind = PackIconMaterialKind.WindowMaximize;
                MainBorder.CornerRadius = new CornerRadius(30);
            }
        }

        // Add missing navigation methods
        private async void Rooms_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenuButton(IconRooms, TextRooms);
            await ShowPageLoadingAsync("Loading Rooms...");
            MainFrame.Navigate(new RoomsPage());
            await HideLoadingOverlayAsync();
        }

        private async void Staff_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenuButton(IconStaff, TextStaff);
            await ShowPageLoadingAsync("Loading Staff...");
            MainFrame.Navigate(new StaffPage());
            await HideLoadingOverlayAsync();
        }

        private async void Report_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenuButton(IconReport, TextReport);
            await ShowPageLoadingAsync("Generating Report...");
            await Task.Delay(1200);
            await HideLoadingOverlayAsync();
        }

        private async void Settings_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenuButton(IconSettings, TextSettings);
            await ShowPageLoadingAsync("Loading Settings...");
            await Task.Delay(800);
            await HideLoadingOverlayAsync();
        }
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenuButton(IconLogout, TextLogout);
            var result = MessageBox.Show("Are you sure you want to logout?", "Logout",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Close current window and show login
                new MainWindow().Show();
                this.Close();
            }
        }
    }
}