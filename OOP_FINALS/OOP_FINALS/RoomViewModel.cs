using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.ComponentModel;
using System.Windows.Input;

namespace OOP_FINALS
{
    public class RoomViewModel
    {
        public ObservableCollection<RoomModel> Rooms { get; set; }

        public ICommand LoadRoomsCommand { get; set; }
        public ICommand RoomClickCommand { get; set; }
        public ICommand AddRoomCommand { get; set; }
        public ICommand EditRoomCommand { get; set; }

        private DatabaseHelper db;

        public RoomViewModel()
        {
            Rooms = new ObservableCollection<RoomModel>();

            // Avoid running runtime-only logic (like DB connections) while the XAML designer
            // instantiates this view model. Designer may not be able to load DB dependencies
            // which makes the type appear missing to XAML (XDG0008).
            bool isInDesignMode = DesignerProperties.GetIsInDesignMode(new DependencyObject());

            if (!isInDesignMode)
            {
                try
                {
                    db = new DatabaseHelper();
                    LoadRooms();
                }
                catch
                {
                    MessageBox.Show("⚠️ Database not connected.");
                }
            }

            // Commands can be constructed regardless of design/runtime so bindings exist in XAML
            RoomClickCommand = new RelayCommand(room => OpenBooking(room as RoomModel));
            AddRoomCommand = new RelayCommand(_ => OpenAddRoom());
            EditRoomCommand = new RelayCommand(room => OpenEditRoom(room as RoomModel));
        }

        private void LoadRooms()
        {
            if (db == null) return;

            Rooms.Clear();

            string query = @"
            SELECT r.RoomID, r.RoomNumber, r.Status,
                   rt.RoomTypeID, rt.TypeName, rt.PricePerNight
            FROM Rooms r
            INNER JOIN RoomTypes rt ON r.RoomTypeID = rt.RoomTypeID";

            DataTable dt = db.ExecuteQuery(query);

            foreach (DataRow row in dt.Rows)
            {
                Rooms.Add(new RoomModel
                {
                    RoomID = (int)row["RoomID"],
                    RoomNumber = row["RoomNumber"].ToString(),
                    RoomTypeID = (int)row["RoomTypeID"],
                    TypeName = row["TypeName"].ToString(),
                    Price = (decimal)row["PricePerNight"],
                    Status = row["Status"].ToString(),
                    ImagePath = GetImage(row["TypeName"].ToString())
                });
            }
        }

        private string GetImage(string type)
        {
            switch (type.ToLower())
            {
                case "suite": return "images/suite.jpg";
                case "deluxe": return "images/deluxe.jpg";
                default: return "images/standard.jpg";
            }
        }

        private void OpenBooking(RoomModel room)
        {
            if (room == null) return;

            MessageBox.Show($"📅 Booking Room {room.RoomNumber}");
        }

        private void OpenAddRoom()
        {
            AddEditRoomWindow win = new AddEditRoomWindow();
            win.ShowDialog();
            LoadRooms();
        }

        private void OpenEditRoom(RoomModel room)
        {
            if (room == null) return;

            AddEditRoomWindow win = new AddEditRoomWindow(room);
            win.ShowDialog();
            LoadRooms();
        }
    }
}