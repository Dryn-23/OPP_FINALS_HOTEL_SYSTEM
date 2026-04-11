using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace OOP_FINALS
{
    public partial class StaffPage : Page
    {
        private string connectionString = @"Server=DESKTOP-51HVDT7;Database=JohnCis_HotelManagement_System;Trusted_Connection=True;";
        private DataTable staffTable = new DataTable();

        public StaffPage()
        {
            InitializeComponent();
            LoadStaff();
        }

        // 🔍 SEARCH
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadStaff(SearchBox.Text);
        }

        // ➕ ADD STAFF - MODAL DIALOG
        private void AddStaffBtn_Click(object sender, RoutedEventArgs e)
        {
            new AddStaffWindow().ShowDialog();
            LoadStaff(SearchBox.Text); // Refresh after close
        }

        // ✏️ EDIT STAFF
        private void EditStaff_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DataRowView rowView)
            {
                try
                {
                    int staffID = Convert.ToInt32(rowView["staffID"]);
                    string fullname = rowView["Fullname"]?.ToString() ?? "";
                    string username = rowView["Username"]?.ToString() ?? "";
                    string contact = rowView["ContactNumber"]?.ToString() ?? "";

                    // Simple edit dialog
                    string newFullname = Microsoft.VisualBasic.Interaction.InputBox("Edit Full Name:", "Edit Staff", fullname);
                    if (string.IsNullOrEmpty(newFullname)) return;

                    string newUsername = Microsoft.VisualBasic.Interaction.InputBox("Edit Username:", "Edit Staff", username);
                    if (string.IsNullOrEmpty(newUsername)) return;

                    string newContact = Microsoft.VisualBasic.Interaction.InputBox("Edit Contact:", "Edit Staff", contact);

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();
                        string query = @"UPDATE Staff SET Fullname = @Fullname, Username = @Username, ContactNumber = @ContactNumber 
                                       WHERE staffID = @staffID";
                        SqlCommand cmd = new SqlCommand(query, con);
                        cmd.Parameters.AddWithValue("@Fullname", newFullname);
                        cmd.Parameters.AddWithValue("@Username", newUsername);
                        cmd.Parameters.AddWithValue("@ContactNumber", newContact ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@staffID", staffID);
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Staff updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadStaff(SearchBox.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error editing staff: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 🗑 DELETE STAFF
        private void DeleteStaff_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DataRowView rowView)
            {
                int staffID = Convert.ToInt32(rowView["staffID"]);
                string fullname = rowView["Fullname"]?.ToString() ?? "this staff";

                var result = MessageBox.Show($"Are you sure you want to delete {fullname}?",
                                           "Confirm Delete",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection con = new SqlConnection(connectionString))
                        {
                            con.Open();
                            string query = "DELETE FROM Staff WHERE staffID = @staffID";
                            SqlCommand cmd = new SqlCommand(query, con);
                            cmd.Parameters.AddWithValue("@staffID", staffID);
                            cmd.ExecuteNonQuery();
                        }

                        MessageBox.Show("Staff deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadStaff(SearchBox.Text);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting staff: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void LoadStaff(string search = "")
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
    SELECT s.staffID, s.Fullname, r.RoleName AS Department, s.Username, 
           s.ContactNumber, s.Email, s.Status
    FROM Staff s
    INNER JOIN Roles r ON s.RoleID = r.RoleID
    WHERE (@search = '' OR s.Fullname LIKE @search OR s.Username LIKE @search)";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, con);
                    adapter.SelectCommand.Parameters.AddWithValue("@search", "%" + search + "%");

                    staffTable.Clear();
                    adapter.Fill(staffTable);

                    StaffDataGrid.ItemsSource = staffTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading staff: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}