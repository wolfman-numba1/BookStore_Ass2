using BookStore.Services.MessageTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BookStore.WebClient.ViewModels
{
    public class ConfirmOrderViewModel
    {
        public ConfirmOrderViewModel(Order UserOrder)
        {
            CurrentOrder = UserOrder;
        }
        public Order CurrentOrder { get; set; }
    }
}