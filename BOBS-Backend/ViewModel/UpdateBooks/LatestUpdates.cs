﻿using BOBS_Backend.Models.Book;
using BOBS_Backend.Models.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BOBS_Backend.ViewModel.UpdateBooks
{
    public class LatestUpdates
    {
        public List<Price> UserBooks { get; set; }
        public List<Price> NotUserBooks { get; set; }

        public List<FilterOrders> ImpOrders { get; set; }
    }
}
