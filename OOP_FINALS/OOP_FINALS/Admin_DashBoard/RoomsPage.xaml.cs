using System.Collections.Generic;
using System.Data;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;

namespace OOP_FINALS
{
    public partial class RoomsPage : Page
    {
        public List<RoomModel> Rooms { get; set; } = new List<RoomModel>();

        public RoomsPage()
        {
            InitializeComponent();
            LoadRooms();
           // RoomsItemsControl.ItemsSource = Rooms; // Bind to ItemsControl
        }

        private void LoadRooms()
        {
            DatabaseHelper db = new DatabaseHelper();
            //load rooms with status
            string query = @"
               SELECT 
                r.RoomID,
                r.RoomNumber,
                CASE 
                WHEN EXISTS 
               (
               SELECT 
                1 
               FROM Reservations 
               WHERE RoomID = r.RoomID 
               AND GETDATE() BETWEEN CheckInDate AND CheckOutDate
               )
                THEN 'Occupied'
                ELSE 'Available'
               END AS Status,
                rt.TypeName, rt.PricePerNight
               FROM Rooms r
               JOIN RoomTypes rt ON r.RoomTypeID = rt.RoomTypeID";

            DataTable dt = db.ExecuteQuery(query);
            Rooms.Clear();

            foreach (DataRow row in dt.Rows)
            {
                Rooms.Add(new RoomModel
                {
                    RoomID = (int)row["RoomID"],
                    RoomNumber = row["RoomNumber"].ToString(),
                    TypeName = row["TypeName"].ToString(),
                    Price = (decimal)row["PricePerNight"],
                    Status = row["Status"].ToString(),
                    ImagePath = GetImageByType(row["TypeName"].ToString())
                });
            }
        }

        private string GetImageByType(string type)
        {
            switch (type.ToLower())
            {
                case "deluxe":
                    return "deluxe-room.jpg";
                case "suite":
                    return "suite-room.jpg";
                default:
                    return "standard-room.jpg";
            }
        }

        private void AddRoomButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to Add Room page or open dialog
            LoadRooms(); // Refresh list
        }

        private void BookButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var room = button.Tag as RoomModel;
            if (room != null)
            {
                MessageBox.Show($"Book Room {room.RoomNumber}");
                // Navigate to booking page
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var room = button.Tag as RoomModel;
            if (room != null)
            {
                MessageBox.Show($"Edit Room {room.RoomNumber}");
                // Navigate to edit page
            }
        }

    }
}