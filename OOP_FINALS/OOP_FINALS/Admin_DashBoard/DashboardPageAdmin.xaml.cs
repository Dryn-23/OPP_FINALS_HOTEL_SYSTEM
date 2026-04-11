using System.Windows;
using System.Windows.Controls;

namespace OOP_FINALS
{
    public partial class DashboardPageAdmin : Page
    {
        public DashboardPageAdmin(DashboardData data)
        {
            InitializeComponent();
            DataContext = data;

            // Safe binding
            if (lstRecentActivity != null && data?.RecentActivities != null)
            {
                lstRecentActivity.ItemsSource = data.RecentActivities;
            }
        }

        private void ViewAllActivities_Click(object sender, RoutedEventArgs e)
        {
            var data = DataContext as DashboardData;
            MessageBox.Show($"📋 Activity Summary\n\n" +
                           $"• Total Recent Activities: {data?.RecentActivities?.Count ?? 0}\n" +
                           $"• Today's Check-ins: {data?.CheckIns}\n" +
                           $"• Occupancy: {data?.OccupancyRate}\n\n" +
                           $"Full activity dashboard coming soon!",
                           "Delisas Hotel - Activities",
                           MessageBoxButton.OK,
                           MessageBoxImage.Information);
        }
    }
}