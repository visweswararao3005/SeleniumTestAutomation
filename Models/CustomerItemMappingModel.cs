using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestAutomation.Models
{
    public class CustomerItemMappingModel
    {
        public List<Mapping> Mappings { get; set; }
    }
    public class Mapping
    {
        public string CustomerName { get; set; }
        public List<Items> Items { get; set; }
    }
    public class Items
    {
        public string Name { get; set; }
        public string UPC { get; set; }
        public string Quantity { get; set; }
    }
}
