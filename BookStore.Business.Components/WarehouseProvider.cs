using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BookStore.Business.Components.Interfaces;
using BookStore.Business.Entities;
using System.Transactions;
using Microsoft.Practices.ServiceLocation;
using DeliveryCo.MessageTypes;

namespace BookStore.Business.Components
{
    public class WarehouseProvider : IWarehouseProvider
    {
        public int[,] ProcessOrder(Order pOrder)
        {
            // return matrix [0, 0, 0] to indicate error
            // otherwise [book, warehouse, quantity]
            int[,] matrix = new int[1, 1];

            return matrix;
        }
    }
}
