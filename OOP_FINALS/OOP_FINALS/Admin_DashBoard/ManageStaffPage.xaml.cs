using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace OOP_FINALS
{
    public partial class ManageStaffPage : Page
    {
        private readonly string connectionString =
            @"Server=DESKTOP-51HVDT7;Database=JohnCis_HotelManagement_System;Trusted_Connection=True;";

        private ObservableCollection<Staff> staffList;
        private List<Role> rolesList;

        public ManageStaffPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            LoadStaffData();
            LoadRolesData();
        }

        private void LoadStaffData()
        {
            staffList = new ObservableCollection<Staff>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
            SELECT s.StaffID, s.RoleID, s.FullName, s.Username, 
                   s.ContactNumber, s.Email, s.Status, r.RoleName
            FROM Staff s 
            LEFT JOIN Roles r ON s.RoleID = r.RoleID
            ORDER BY s.StaffID";

                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    staffList.Add(new Staff
                    {
                        StaffID = reader.GetInt32(0),
                        RoleID = reader["RoleID"].ToString(),  // ✅ SAFE CAST - works for INT or STRING
                        FullName = reader.GetString(2),
                        Username = reader.GetString(3),
                        ContactNumber = reader.GetString(4),
                        Email = reader.GetString(5),
                        Status = reader.GetString(6),
                        RoleName = reader.IsDBNull(7) ? "" : reader.GetString(7)
                    });
                }
            }

            StaffDataGrid.ItemsSource = staffList;
        }

        private void LoadRolesData()
        {
            rolesList = new List<Role>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT RoleID, RoleName FROM Roles ORDER BY RoleID";
                SqlCommand cmd = new SqlCommand(query, conn);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rolesList.Add(new Role
                        {
                            RoleID = reader["RoleID"]?.ToString() ?? "",
                            RoleName = reader["RoleName"]?.ToString() ?? ""
                        });
                    }
                }
            }

            // Set ComboBox AFTER data is loaded
            if (RoleComboColumn != null)
            {
                RoleComboColumn.ItemsSource = rolesList;
            }
        }

        private void AddStaffButton_Click(object sender, RoutedEventArgs e)
        {
            var newStaff = new Staff
            {
                StaffID = 0,
                EmployeeID = 0,
                Username = "",
                FullName = "",
                ContactNumber = "",
                Email = "",
                RoleID = "1",
                Status = "Active"
            };

            staffList.Add(newStaff);
            StaffDataGrid.SelectedItem = newStaff;
            StaffDataGrid.ScrollIntoView(newStaff);
            ShowStatus("New staff added. Fill details and click Save Changes.", true);
        }

        private void DeleteStaffButton_Click(object sender, RoutedEventArgs e)
        {
            if (StaffDataGrid.SelectedItem is Staff selectedStaff)
            {
                var result = MessageBox.Show($"Delete staff '{selectedStaff.FullName}' (ID: {selectedStaff.StaffID})?",
                                           "Confirm Delete",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    DeleteStaffFromDatabase(selectedStaff.StaffID);
                    staffList.Remove(selectedStaff);
                    ShowStatus("Staff deleted successfully.", true);
                }
            }
            else
            {
                ShowStatus("Please select a staff member to delete.", false);
            }
        }

        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        foreach (Staff staff in staffList.Where(s => s.StaffID == 0 ||
                            !string.IsNullOrEmpty(s.Username) && !string.IsNullOrEmpty(s.FullName)))
                        {
                            if (staff.StaffID == 0)
                            {
                                InsertStaff(conn, transaction, staff);
                            }
                            else
                            {
                                UpdateStaff(conn, transaction, staff);
                            }
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }

                LoadData();
                ShowStatus("All changes saved successfully!", true);
            }
            catch (Exception ex)
            {
                ShowStatus($"Error: {ex.Message}", false);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            ShowStatus("Data refreshed from database.", true);
        }

        private void InsertStaff(SqlConnection conn, SqlTransaction transaction, Staff staff)
        {
            int nextEmpID = GetNextEmployeeID(conn);

            string query = @"
                INSERT INTO Staff (EmployeeID, Username, FullName, Phone, Email, RoleID, Status)
                OUTPUT INSERTED.StaffID, INSERTED.EmployeeID
                VALUES (@EmployeeID, @Username, @FullName, @Phone, @Email, @RoleID, @Status)";

            using (SqlCommand cmd = new SqlCommand(query, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@EmployeeID", nextEmpID);
                cmd.Parameters.AddWithValue("@Username", staff.Username ?? "");
                cmd.Parameters.AddWithValue("@FullName", staff.FullName ?? "");
                cmd.Parameters.AddWithValue("@Phone", staff.ContactNumber ?? "");
                cmd.Parameters.AddWithValue("@Email", staff.Email ?? "");
                cmd.Parameters.AddWithValue("@RoleID", staff.RoleID ?? "1");
                cmd.Parameters.AddWithValue("@Status", staff.Status ?? "Active");

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        staff.StaffID = reader.GetInt32(0);
                        staff.EmployeeID = reader.GetInt32(1);
                    }
                }
            }
        }

        private void UpdateStaff(SqlConnection conn, SqlTransaction transaction, Staff staff)
        {
            string query = @"
                UPDATE Staff 
                SET Username = @Username, FullName = @FullName, Phone = @Phone, 
                    Email = @Email, RoleID = @RoleID, Status = @Status
                WHERE StaffID = @StaffID";

            using (SqlCommand cmd = new SqlCommand(query, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@StaffID", staff.StaffID);
                cmd.Parameters.AddWithValue("@Username", staff.Username ?? "");
                cmd.Parameters.AddWithValue("@FullName", staff.FullName ?? "");
                cmd.Parameters.AddWithValue("@Phone", staff.ContactNumber ?? "");
                cmd.Parameters.AddWithValue("@Email", staff.Email ?? "");
                cmd.Parameters.AddWithValue("@RoleID", staff.RoleID ?? "1");
                cmd.Parameters.AddWithValue("@Status", staff.Status ?? "Active");

                cmd.ExecuteNonQuery();
            }
        }

        private int GetNextEmployeeID(SqlConnection conn)
        {
            string query = "SELECT ISNULL(MAX(EmployeeID), 0) + 1 FROM Staff";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private void DeleteStaffFromDatabase(int staffId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string query = "UPDATE Staff SET Status = 'Inactive' WHERE StaffID = @StaffID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@StaffID", staffId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        private void ShowStatus(string message, bool isSuccess)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = isSuccess ?
                System.Windows.Media.Brushes.LightGreen :
                System.Windows.Media.Brushes.OrangeRed;
        }

        private void StaffDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }

    // FIXED Staff model - REMOVED Address field to match your DB
    public class Staff
    {
        public int StaffID { get; set; }
        public int EmployeeID { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string ContactNumber { get; set; }
        public string Email { get; set; }
        public string RoleID { get; set; }
        public string Status { get; set; }
        public string RoleName { get; set; }
    }

    public class Role
    {
        public string RoleID { get; set; }
        public string RoleName { get; set; }
    }
}