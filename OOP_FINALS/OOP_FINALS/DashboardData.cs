using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OOP_FINALS
{
    public class DashboardData : INotifyPropertyChanged
    {
        private string _totalStaff = "0";
        private string _totalRooms = "0";
        private string _totalBookings = "0";
        private string _checkIns = "0";
        private string _checkOuts = "0";
        private string _totalRevenue = "₱0";
        private double _revenueProgress = 75;
        private string _revenueTrend = "+12.5%";
        private double _occupancyProgress = 78;
        private string _occupancyRate = "78%";
        private string _occupancyTrend = "+3.2%";
        private string _activityCount = "4";
        private ObservableCollection<ActivityItem> _recentActivities = new ObservableCollection<ActivityItem>();

        // Existing properties...
        public string TotalStaff { get => _totalStaff; set => SetProperty(ref _totalStaff, value); }
        public string TotalRooms { get => _totalRooms; set => SetProperty(ref _totalRooms, value); }
        public string TotalBookings { get => _totalBookings; set => SetProperty(ref _totalBookings, value); }
        public string CheckIns { get => _checkIns; set => SetProperty(ref _checkIns, value); }
        public string CheckOuts { get => _checkOuts; set => SetProperty(ref _checkOuts, value); }
        public string TotalRevenue { get => _totalRevenue; set => SetProperty(ref _totalRevenue, value); }

        // NEW Dynamic Properties
        public double RevenueProgress { get => _revenueProgress; set => SetProperty(ref _revenueProgress, value); }
        public string RevenueTrend { get => _revenueTrend; set => SetProperty(ref _revenueTrend, value); }
        public double OccupancyProgress { get => _occupancyProgress; set => SetProperty(ref _occupancyProgress, value); }
        public string OccupancyRate { get => _occupancyRate; set => SetProperty(ref _occupancyRate, value); }
        public string OccupancyTrend { get => _occupancyTrend; set => SetProperty(ref _occupancyTrend, value); }
        public string ActivityCount { get => _activityCount; set => SetProperty(ref _activityCount, value); }
        public ObservableCollection<ActivityItem> RecentActivities
        {
            get => _recentActivities;
            set => SetProperty(ref _recentActivities, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    // Activity Model
    public class ActivityItem
    {
        public string ActivityText { get; set; }
        public DateTime ActivityTime { get; set; }
    }
}