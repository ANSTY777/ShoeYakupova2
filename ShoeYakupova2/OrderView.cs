using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShoeYakupova2
{
    public class OrderView
    {
        public int OrderID { get; set; }
        public DateTime OrderDate { get; set; }
        public string ClientFIO { get; set; }
        public decimal TotalSum { get; set; }
        public Order SourceOrder { get; set; }
    }
}