using System.Data;
using System.Data.SqlClient;
using System.Windows.Controls;

namespace OOP_FINALS
{
    public partial class HousekeepingPage : Page
    {
        private readonly string connectionString =
            @"Server=DESKTOP-51HVDT7;Database=JohnCis_HotelManagement_System;Trusted_Connection=True;TrustServerCertificate=True;";

        public HousekeepingPage()
        {
            InitializeComponent();
            LoadHousekeepingData();
        }

        private void LoadHousekeepingData()
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                SqlDataAdapter da = new SqlDataAdapter(@"
           SELECT   
               h.HousekeepingID,
               s.FullName AS StaffName, 
               h.RoomID,  
               
               h.CleaningStatus,  
               h.CleaningDate 
           FROM [dbo].[Housekeeping] h 
           LEFT JOIN [dbo].[Staff] s 
           ON h.StaffID = s.StaffID", con); 
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgHousekeeping.ItemsSource = dt.DefaultView;
            }
        }
    }
}