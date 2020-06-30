﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BobBookstore.Models
{
    public class OrderDetail
    {
        [Key]
        public long OrderStatus_Id { get; set; }
        public Order Order { get; set; }
        public Book Book { get; set; }
        public Price Price { get; set; }
        public double price { get; set; }
        public int Quantity { get; set; }
    }
}
