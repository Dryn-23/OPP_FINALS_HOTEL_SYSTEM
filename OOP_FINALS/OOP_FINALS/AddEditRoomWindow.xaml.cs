using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OOP_FINALS
{
    /// <summary>
    /// Interaction logic for AddEditRoomWindow.xaml
    /// </summary>
    public partial class AddEditRoomWindow : Window
    {
        private RoomModel room;
        private DatabaseHelper db = new DatabaseHelper();

        public AddEditRoomWindow(RoomModel existingRoom = null)
        {
            InitializeComponent();
            room = existingRoom;

            if (room != null)
            {
                RoomNumberBox.Text = room.RoomNumber;
                StatusBox.Text = room.Status;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (room == null)
            {
                // INSERT
                string query = $"INSERT INTO Rooms (RoomNumber, RoomTypeID, Status) VALUES ('{RoomNumberBox.Text}', 1, '{StatusBox.Text}')";
                db.ExecuteQuery(query);
            }
            else
            {
                // UPDATE
                string query = $"UPDATE Rooms SET RoomNumber='{RoomNumberBox.Text}', Status='{StatusBox.Text}' WHERE RoomID={room.RoomID}";
                db.ExecuteQuery(query);
            }

            this.Close();
        }
    }
}
