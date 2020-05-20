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
            using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
            {
                // make return matrix to maximum required height
                int max_entries = 0;
                foreach (OrderItem order in pOrder.OrderItems)
                {
                    max_entries += order.Quantity;
                }

                int[,] result = new int[max_entries, 3];

                int index = 0;

                // for each individual order
                foreach (OrderItem order in pOrder.OrderItems)
                {
                    // get the book for the order
                    Book book = order.Book;

                    // look at each of the warehouses that contain the book
                    foreach (Warehouse wh in book.Stock.Warehouses)
                    {
                        // fill the results matrix
                        result[index, 0] = book.Id;
                        result[index, 1] = wh.Id;
                        result[index, 2] = (int)wh.Quantity >= order.Quantity ? order.Quantity : (int)wh.Quantity;

                        // if the warehouse has enough quanity in stock
                        if (wh.Quantity >= order.Quantity)
                        {
                            // reduce the quantity of the warehouse 
                            wh.Quantity -= order.Quantity;

                            // empty the order
                            order.Quantity = 0;

                            // break the loop because the order has been completed 
                            break;
                        } 
                        // if the warehouse does not have enough in stock
                        else
                        {
                            // reduce the quantity of the order
                            order.Quantity -= (int )wh.Quantity;

                            // empty the warehouse of it's stock for this book
                            wh.Quantity = 0;
                        }

                        index++;
                    }

                    // if the if there is quantity left over then the order can't go through
                    if (order.Quantity > 0)
                    {
                        // setup the error matrix
                        int[,] error = new int[1, 1];
                        error[0, 0] = -1;

                        // return the error matrix
                        return error;
                    }
                }

                // save changes to the database
                lContainer.SaveChanges();

                // return the processed orders
                return result;
            }
        }
    }
}
