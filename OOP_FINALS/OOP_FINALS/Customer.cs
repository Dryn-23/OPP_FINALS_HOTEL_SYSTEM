using System;

namespace CustomerDashboard
{
    public class Customer
    {
        public int CustomerID { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string ContactNumber { get; set; }
        public string Email { get; set; }

        public string ValidIDType { get; set; }
        public string ValidIDNumber { get; set; }

        // Computed property
        public string FullName => $"{FirstName} {LastName}";

        public string Initials =>
            $"{FirstName?[0]}{LastName?[0]}".ToUpper();
    }
}