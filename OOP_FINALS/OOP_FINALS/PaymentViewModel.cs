using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace OOP_FINALS
{
    // 🔥 ADD THIS MISSING PaymentItem CLASS
    public class PaymentItem
    {
        public int PaymentID { get; set; }
        public int ReservationID { get; set; }
        public string CustomerName { get; set; } = "";
        public string RoomNumber { get; set; } = "";
        public decimal AmountPaid { get; set; }
        public string PaymentMethod { get; set; } = "";
        public string PaymentStatus { get; set; } = "";
        public DateTime PaymentDate { get; set; }
    }

    public class PaymentViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<PaymentItem> _allPayments;
        private CollectionViewSource _filteredPaymentsSource;
        private string _searchText = "";
        private int _selectedStatusIndex = 0;

        public ObservableCollection<PaymentItem> AllPayments
        {
            get => _allPayments;
            set
            {
                _allPayments = value;
                OnPropertyChanged();
            }
        }

        public CollectionViewSource FilteredPaymentsSource
        {
            get => _filteredPaymentsSource;
            set
            {
                _filteredPaymentsSource = value;
                OnPropertyChanged();
            }
        }

        public ICollectionView FilteredPaymentsView => FilteredPaymentsSource?.View;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilteredPaymentsSource.View.Refresh();
            }
        }

        public int SelectedStatusIndex
        {
            get => _selectedStatusIndex;
            set
            {
                _selectedStatusIndex = value;
                OnPropertyChanged();
                FilteredPaymentsSource.View.Refresh();
            }
        }

        public int RecordCount => FilteredPaymentsView?.Cast<PaymentItem>().Count() ?? 0;
        public decimal TotalPayments => GetTotalPayments();
        public decimal TotalToday => GetTotalToday();
        public decimal TotalMonth => GetTotalMonth();

        public ICommand NewPaymentCommand { get; set; }
        public ICommand SearchCommand { get; set; }
        public ICommand ViewBillingCommand { get; set; }
        public ICommand EditPaymentCommand { get; set; }
        public ICommand RefreshCommand { get; set; }

        public PaymentViewModel()
        {
            AllPayments = new ObservableCollection<PaymentItem>();
            FilteredPaymentsSource = new CollectionViewSource { Source = AllPayments };
            FilteredPaymentsSource.Filter += FilterPayments;

            LoadPayments();

            // 🔥 FIXED: Use your existing RelayCommand
            NewPaymentCommand = new RelayCommand(o => NewPayment());
            SearchCommand = new RelayCommand(o => RefreshFilter());
            ViewBillingCommand = new RelayCommand(o => ViewBilling(o as PaymentItem));
            EditPaymentCommand = new RelayCommand(o => EditPayment(o as PaymentItem));
            RefreshCommand = new RelayCommand(o => LoadPayments());
        }

        private void LoadPayments()
        {
            try
            {
                DatabaseHelper db = new DatabaseHelper();
                string query = @"
            SELECT p.PaymentID, p.ReservationID, p.AmountPaid, p.PaymentMethod, 
                   p.PaymentStatus, p.PaymentDate,
                   ISNULL(c.FirstName + ' ' + c.LastName, 'Walk-in') AS CustomerName,
                   ISNULL(r.RoomNumber, 'N/A') AS RoomNumber
            FROM Payment p
            LEFT JOIN Reservations res ON p.ReservationID = res.ReservationID
            LEFT JOIN Customers c ON res.CustomerID = c.CustomerID
            LEFT JOIN Rooms r ON res.RoomID = r.RoomID
            ORDER BY p.PaymentDate DESC";

                DataTable dt = db.ExecuteQuery(query);

                AllPayments.Clear();

                // 🔥 ONLY Add if data exists
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        AllPayments.Add(new PaymentItem
                        {
                            PaymentID = Convert.ToInt32(row["PaymentID"]),
                            ReservationID = Convert.ToInt32(row["ReservationID"]),
                            CustomerName = row["CustomerName"]?.ToString() ?? "Unknown",
                            RoomNumber = row["RoomNumber"]?.ToString() ?? "N/A",
                            AmountPaid = Convert.ToDecimal(row["AmountPaid"]),
                            PaymentMethod = row["PaymentMethod"]?.ToString() ?? "Cash",
                            PaymentStatus = row["PaymentStatus"]?.ToString() ?? "Pending",
                            PaymentDate = Convert.ToDateTime(row["PaymentDate"])
                        });
                    }
                }
                else
                {
                    MessageBox.Show("No payments found in database!", "Info");
                }

                FilteredPaymentsSource.View.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database Error: {ex.Message}", "Error");
                AllPayments.Clear(); // Empty if DB fails
            }
        }

        private void LoadSampleData()
        {
            AllPayments.Clear();
            AllPayments.Add(new PaymentItem
            {
                PaymentID = 1,
                CustomerName = "John Doe",
                RoomNumber = "101",
                AmountPaid = 2500m,
                PaymentStatus = "Paid",
                PaymentDate = DateTime.Now.AddDays(-1),
                PaymentMethod = "Cash"
            });
            AllPayments.Add(new PaymentItem
            {
                PaymentID = 2,
                CustomerName = "Jane Smith",
                RoomNumber = "202",
                AmountPaid = 4500m,
                PaymentStatus = "Pending",
                PaymentDate = DateTime.Now,
                PaymentMethod = "GCash"
            });
            AllPayments.Add(new PaymentItem
            {
                PaymentID = 3,
                CustomerName = "Mike Johnson",
                RoomNumber = "303",
                AmountPaid = 3200m,
                PaymentStatus = "Paid",
                PaymentDate = DateTime.Now.AddDays(-2),
                PaymentMethod = "Card"
            });
            AllPayments.Add(new PaymentItem
            {
                PaymentID = 4,
                CustomerName = "Sarah Wilson",
                RoomNumber = "404",
                AmountPaid = 1800m,
                PaymentStatus = "Failed",
                PaymentDate = DateTime.Now.AddDays(-5),
                PaymentMethod = "Bank Transfer"
            });
            FilteredPaymentsSource.View.Refresh();
        }

        private void FilterPayments(object sender, FilterEventArgs e)
        {
            if (e.Item is PaymentItem payment)
            {
                // Status filter
                bool statusMatch = SelectedStatusIndex == 0 ||
                                  (SelectedStatusIndex == 1 && payment.PaymentStatus == "Completed") ||
                                  (SelectedStatusIndex == 2 && payment.PaymentStatus == "Partial") ||
                                  (SelectedStatusIndex == 3 && payment.PaymentStatus == "Cancelled");

                // 🔥 FIXED: Use IndexOf instead of Contains with StringComparison
                bool searchMatch = string.IsNullOrWhiteSpace(SearchText) ||
                                  payment.CustomerName.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  payment.PaymentID.ToString().IndexOf(SearchText) >= 0 ||
                                  payment.RoomNumber.IndexOf(SearchText) >= 0;

                e.Accepted = statusMatch && searchMatch;
            }
        }

        private decimal GetTotalPayments() => FilteredPaymentsView?.Cast<PaymentItem>().Sum(p => p.AmountPaid) ?? 0m;
        private decimal GetTotalToday() => FilteredPaymentsView?.Cast<PaymentItem>().Where(p => p.PaymentDate.Date == DateTime.Today).Sum(p => p.AmountPaid) ?? 0m;
        private decimal GetTotalMonth() => FilteredPaymentsView?.Cast<PaymentItem>().Where(p => p.PaymentDate.Month == DateTime.Now.Month && p.PaymentDate.Year == DateTime.Now.Year).Sum(p => p.AmountPaid) ?? 0m;

        private void RefreshFilter()
        {
            FilteredPaymentsSource.View.Refresh();
        }

        private void NewPayment() => MessageBox.Show("New Payment - Open window here", "New Payment");
        private void ViewBilling(PaymentItem payment) => MessageBox.Show($"Payment #{payment?.PaymentID}\n{payment?.CustomerName}\n₱{payment?.AmountPaid:N0}");
        private void EditPayment(PaymentItem payment) => MessageBox.Show($"Edit Payment #{payment?.PaymentID}");

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}