using System;
using System.Collections.Generic;

namespace LoginAutomation.Tests.Models
{
    public class OrdersModel
    {
        public Order Order { get; set; }
    }

    public class Order
    {
        public SelectCustomerDetails SelectCustomerDetails { get; set; }
        public OrderDetails OrderDetails { get; set; }
        public Address ShipTo { get; set; }
        public Address BillTo { get; set; }
        public Address ShipFrom { get; set; }
        public List<OrderItem> Items { get; set; }
    }

    public class SelectCustomerDetails
    {
        public string CustomerName { get; set; }
    }

    public class OrderDetails
    {
        public string po_number { get; set; }
        public string customer_reference_number { get; set; }
        public string fulfillment_type { get; set; }
        public string shipping_method { get; set; }
        public string account_number { get; set; }
        public string order_date { get; set; }
        public string due_date { get; set; }
        public string ship_date { get; set; }
        public string reference_1 { get; set; }
        public string reference_2 { get; set; }
        public string customer_message { get; set; }
    }

    public class Address
    {
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string address_1 { get; set; }
        public string address_2 { get; set; }
        public string country { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string zip { get; set; }
        public string notes { get; set; } // optional: present only for ShipTo
    }

    public class OrderItem
    {
        public string ItemName { get; set; }
        public string Warehouse { get; set; }
        public string Price { get; set; }
        public string Quantity { get; set; }
    }
}
