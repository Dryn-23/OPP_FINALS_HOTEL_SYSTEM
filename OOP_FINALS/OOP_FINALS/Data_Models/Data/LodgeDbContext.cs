//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;

//namespace OOP_FINALS.Data_Models.Data
//{
//    // Data/LodgeDbContext.cs
  

//    public class LodgeDbContext : DbContext
//    {
//        public DbSet<Customer> Customers { get; set; }
//        public DbSet<Reservation> Reservations { get; set; }
//        public DbSet<Payment> Payments { get; set; }
//        public DbSet<Billing> Billings { get; set; }
//        public DbSet<Room> Rooms { get; set; }
//        public DbSet<RoomType> RoomTypes { get; set; }
//        public DbSet<HouseKeeping> HouseKeeping { get; set; }
//        public DbSet<Staff> Staff { get; set; }

//        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//        {
//            optionsBuilder.UseSqlServer("YourConnectionStringHere");
//        }
//    }
//}
