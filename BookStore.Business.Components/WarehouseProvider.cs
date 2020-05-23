﻿using System;
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
        public int[][] ProcessOrder(Order pOrder)
        {
            using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
            {
                // make return matrix to maximum required height
                int max_entries = 0;
                foreach (OrderItem order in pOrder.OrderItems)
                {
                    max_entries += order.Quantity;
                }

                int[][] result = new int[max_entries][];

                for (int i = 0; i < max_entries; i++)
                {
                    result[i] = new int[] { 0, 0, 0 };
                }

                int index = 0;
                int orderQuantity = 0;

                // for each individual order
                foreach (OrderItem order in pOrder.OrderItems)
                {
                    orderQuantity = order.Quantity;

                    // get the book for the order
                    Book book = order.Book;

                    // look at each of the warehouses that contain the book
                    foreach (Warehouse wh in book.Stock.Warehouses)
                    {
                        // fill the results matrix
                        result[index][0] = book.Id;
                        result[index][1] = wh.Id;
                        result[index][2] = (int)wh.Quantity >= orderQuantity ? orderQuantity : (int)wh.Quantity;

                        // if the warehouse has enough quanity in stock
                        if (wh.Quantity >= orderQuantity)
                        {
                            // reduce the quantity of the warehouse 
                            wh.Quantity -= orderQuantity;

                            // empty the order
                            orderQuantity = 0;

                            // break the loop because the order has been completed 
                            break;
                        } 
                        // if the warehouse does not have enough in stock
                        else
                        {
                            // reduce the quantity of the order
                            orderQuantity -= (int )wh.Quantity;

                            // empty the warehouse of it's stock for this book
                            wh.Quantity = 0;
                        }

                        index++;
                    }

                    // if the if there is quantity left over then the order can't go through
                    if (orderQuantity > 0)
                    {
                        // setup the error matrix
                        result[0][0] = -1;

                        // return the error matrix
                        return result;
                    }
                }

                // save changes to the database
                lContainer.SaveChanges();

                // return the processed orders
                return result;
            }
        }

        public void resetStockLevels(int[][] confirmedOrders)
        {
            using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
            {
                // for each warehouse involved in the confirmed orders
                for (int i = 0; i < confirmedOrders.GetLength(0); i++)
                {
                    // get the warehouse
                    Warehouse wh = lContainer.Warehouses.Find(confirmedOrders[i][1]);

                    // reset the quantity for the warehouse 
                    wh.Quantity += confirmedOrders[i][2];
                }

                // save changes to the database
                lContainer.SaveChanges();
            }
        }
    }
}
