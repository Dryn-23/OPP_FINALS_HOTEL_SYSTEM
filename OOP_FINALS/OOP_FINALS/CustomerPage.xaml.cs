using MailKit.Search;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CustomerDashboard
{
    public partial class CustomerPage : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<Customer> Customers { get; set; } = new ObservableCollection<Customer>();
        public int CustomerCount => Customers.Count;
        private void Notify(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public CustomerPage()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += CustomerPage_Loaded;
        }
        private void SaveEdit_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCustomer != null)
            {
                selectedCustomer.FirstName = EditFirstName.Text;
                selectedCustomer.LastName = EditLastName.Text;
                selectedCustomer.ContactNumber = EditContact.Text;
                selectedCustomer.Email = EditEmail.Text;
                selectedCustomer.ValidIDType = EditIDType.Text;
                selectedCustomer.ValidIDNumber = EditIDNumber.Text;

                // ✅ CALL DATABASE UPDATE HERE
                UpdateCustomerInDatabase(selectedCustomer);

                CustomersDataGrid.Items.Refresh();
                EditPanel.Visibility = Visibility.Collapsed;
            }
        }

        // 🔻 PUT YOUR METHOD HERE (same class)
        private void UpdateCustomerInDatabase(Customer customer)
        {
            string connStr = ConfigurationManager.ConnectionStrings["MyDBConnectionString"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                string query = @"UPDATE Customers SET
                                FirstName=@FirstName,
                                LastName=@LastName,
                                ContactNumber=@Contact,
                                Email=@Email,i
                                ValidIDType=@IDType,
                                ValidIDNumber=@IDNumber
                                WHERE CustomerID=@ID";

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@FirstName", customer.FirstName);
                cmd.Parameters.AddWithValue("@LastName", customer.LastName);
                cmd.Parameters.AddWithValue("@Contact", customer.ContactNumber);
                cmd.Parameters.AddWithValue("@Email", customer.Email);
                cmd.Parameters.AddWithValue("@IDType", customer.ValidIDType);
                cmd.Parameters.AddWithValue("@IDNumber", customer.ValidIDNumber);
                cmd.Parameters.AddWithValue("@ID", customer.CustomerID);

                cmd.ExecuteNonQuery();
            }
        }
        private async void CustomerPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadCustomersAsync();
            CustomerCountText.Text = $"Customers ({Customers.Count})";
        }

        private async System.Threading.Tasks.Task LoadCustomersAsync()

        {
            IsLoading = true;
            UpdateLoadingUI();

            await System.Threading.Tasks.Task.Delay(800); // simulate loading

            Customers.Clear();

            string connectionString = ConfigurationManager.ConnectionStrings["MyDBConnectionString"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                string query = "SELECT * FROM Customers";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Customers.Add(new Customer
                        {
                            CustomerID = reader.GetInt32(0),
                            FirstName = reader.GetString(1),
                            LastName = reader.GetString(2),
                            ContactNumber = reader.GetString(3),
                            Email = reader.GetString(4),
                            ValidIDType = reader.GetString(5),
                            ValidIDNumber = reader.GetString(6)
                        });
                    }
                }
            }
            IsLoading = false;
            UpdateLoadingUI();
            CustomersDataGrid.ItemsSource = Customers;
        }

        private void CustomersDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CustomersDataGrid.SelectedItem is Customer selectedCustomer)
            {
                ShowCustomerInfo(selectedCustomer);
            }
        }

        private void ShowCustomerInfo(Customer customer)
        {
            InfoNameText.Text = customer.FullName;
            InfoCustomerIdText.Text = $"ID: {customer.CustomerID}";
            ContactValueText.Text = customer.ContactNumber;
            EmailValueText.Text = customer.Email;
            ValidIdTypeText.Text = customer.ValidIDType;
            ValidIdNumberText.Text = customer.ValidIDNumber;

            CustomerInfoCard.Visibility = Visibility.Visible;
        }

        private void CloseInfoButton_Click(object sender, RoutedEventArgs e)
        {
            CustomerInfoCard.Visibility = Visibility.Collapsed;
        }
        public string SearchText { get; set; } = "";
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchText = SearchTextBox.Text?.ToLower() ?? "";

            var filtered = Customers.Where(c =>
                c.FullName.ToLower().Contains(SearchText) ||
                c.Email.ToLower().Contains(SearchText) ||
                c.ContactNumber.Contains(SearchText) ||
                c.ValidIDNumber.Contains(SearchText)
            ).ToList();

            CustomersDataGrid.ItemsSource = filtered;
            CustomersDataGrid.Items.Refresh();
        }
        public bool IsLoading { get; set; } = true;

     

        private void UpdateLoadingUI()
        {
            SkeletonLoader.Visibility = IsLoading ? Visibility.Visible : Visibility.Collapsed;
            CustomersDataGrid.Visibility = IsLoading ? Visibility.Collapsed : Visibility.Visible;
        }
        private void SkeletonLoader_Loaded(object sender, RoutedEventArgs e)
        {
            var animation = new DoubleAnimation
            {
                From = -150,
                To = 500,
                Duration = TimeSpan.FromSeconds(1.5),
                RepeatBehavior = RepeatBehavior.Forever
            };

            Shimmer.RenderTransform.BeginAnimation(TranslateTransform.XProperty, animation);
        }
        private Customer selectedCustomer;

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).DataContext is Customer customer)
            {
                selectedCustomer = customer;

                EditFirstName.Text = customer.FirstName;
                EditLastName.Text = customer.LastName;
                EditContact.Text = customer.ContactNumber;
                EditEmail.Text = customer.Email;
                EditIDType.Text = customer.ValidIDType;
                EditIDNumber.Text = customer.ValidIDNumber;

                EditPanel.Visibility = Visibility.Visible;
            }
        }
      

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            EditPanel.Visibility = Visibility.Collapsed;
        }
    }
}