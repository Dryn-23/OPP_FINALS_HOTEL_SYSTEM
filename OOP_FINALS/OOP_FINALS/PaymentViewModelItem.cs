using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OOP_FINALS
{
    public class PaymentViewModelItem : INotifyPropertyChanged
    {
        public int PaymentID { get; set; }
        public int ReservationID { get; set; }
        public string CustomerName { get; set; }
        public string RoomNumber { get; set; }
        public decimal AmountPaid { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime PaymentDate { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
