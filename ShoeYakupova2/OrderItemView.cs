using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoeYakupova2
{
    public class OrderItemView
    {
        public string ProductName { get; set; }
        public string SizeText { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal LineSum { get; set; }

        // Ссылка на настоящий OrderItem — пригодится позже
        public OrderItem SourceItem { get; set; }
    }
}