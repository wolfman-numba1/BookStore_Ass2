using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BookStore.Business.Components.Interfaces;
using BookStore.Business.Entities;
using System.Transactions;
using Microsoft.Practices.ServiceLocation;
using DeliveryCo.MessageTypes;
using System.Runtime.CompilerServices;

namespace BookStore.Business.Components
{
    public class WarehouseProvider : IWarehouseProvider
    {
        public int[][] ConfirmOrder(Order pOrder)
        {
            // warehouse choosing algorithm (multicover or set cover problem depending on implementation)
            return [1][1];
            // return warehouse book tuples
        }

        public int GetStockLevelForBook(int warehouseId, int bookId)
        {
            // make a new database context
            using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
            {
                Warehouse targetWarehouse = lContainer.Warehouses.Where(warehouse => warehouse.Id == warehouseId).FirstOrDefault();

                // pseudocode for now 
                // basically plug the book id into the warehouse's stock hashmap
                return targetWarehouse.stock[bookId];
            }
    }
}
