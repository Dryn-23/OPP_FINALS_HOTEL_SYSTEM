using System.ComponentModel;

namespace OOP_FINALS
{
    public class RoomModel : INotifyPropertyChanged
    {
        public int RoomID { get; set; }
        public string RoomNumber { get; set; }
        public int RoomTypeID { get; set; }
        public string TypeName { get; set; }
        public decimal Price { get; set; }

        private string status;
        public string Status
        {
            get => status;
            set
            {
                status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public string ImagePath { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}