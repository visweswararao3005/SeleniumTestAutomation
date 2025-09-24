using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestAutomation.Models
{
    public class CustomerModel
    {
        public List<Customer> Customers { get; set; }
    }

    public class Customer
    {
        public CustomerDetails CustomerDetails { get; set; }
        public BillingDetails BillingDetails { get; set; }
        public ShippingDetails ShippingDetails { get; set; }
    }

    public class CustomerDetails
    {
        public string CustomerName { get; set; }
        public string CompanyName { get; set; }
        public string CustomerPhoneNumber { get; set; }
        public string CustomerEmailAddress { get; set; }
        public string ContactFirstName { get; set; }
        public string ContactLastName { get; set; }
        public string SalesRep { get; set; }
        public string CustomPackingList { get; set; }
        public string PaymentTerm { get; set; }
        public string FedexAccountNumber { get; set; }
        public string UPSAccountNumber { get; set; }
        public string PT000872 { get; set; }
        public string RetailerId { get; set; }

        public string ShortName { get; set; }
        public string Status { get; set; }
        public string CustomerType { get; set; }
        public string Type { get; set; }
    }

    public class BillingDetails
    {
        public string Name1 { get; set; }
        public string Name2 { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Notes { get; set; }
    }

    public class ShippingDetails
    {
        public string Name1 { get; set; }
        public string Name2 { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Notes { get; set; }
    }

}
