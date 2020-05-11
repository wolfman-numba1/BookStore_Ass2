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
        List<(Warehouse, Book)> ConfirmOrder(Order pOrder)
        {
            // warehouse choosing algorithm (multicover or set cover problem depending on implementation)

            // return warehouse book tuples
        }

        int GetStockLevelForBook(int warehouseId, int bookId)
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
