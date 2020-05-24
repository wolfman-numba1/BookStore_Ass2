﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BookStore.Services.MessageTypes;

namespace BookStore.WebClient.ClientModels
{
    public class Cart
    {
        private List<OrderItem> mOrderItems = new List<OrderItem>();
        public IList<OrderItem> OrderItems { get { return mOrderItems.AsReadOnly(); } }

        public void AddItem(Book pBook, int pQuantity)
        {
            var lItem = mOrderItems.FirstOrDefault(oi => oi.Book.Id == pBook.Id);
            if (lItem == null)
            {
                mOrderItems.Add(new OrderItem() { Book = pBook, Quantity = pQuantity });
            }
            else
            {
                lItem.Quantity += pQuantity;
            }
        }

        public double ComputeTotalValue()
        {
            return mOrderItems.Sum(oi => oi.Book.Price * oi.Quantity);
        }

        public void Clear()
        {
            mOrderItems.Clear();
        }
        public Order ConfirmOrder(UserCache pUserCache)
        {
            Order lOrder = new Order
            {
                OrderDate = DateTime.Now,
                Customer = pUserCache.Model,
                Status = 0
            };
            foreach (OrderItem lItem in mOrderItems)
            {
                lOrder.OrderItems.Add(lItem);
            }
            lOrder.Total = Convert.ToDouble(ComputeTotalValue());
            Order UserOrder = ServiceFactory.Instance.OrderService.ConfirmOrder(lOrder);

            return UserOrder;
        }

        public void CancelOrder(int UserOrder, UserCache pUserCache)
        {
            ServiceFactory.Instance.OrderService.CancelOrder(UserOrder);
        }

        //submit delivery and clear cart 
        public void SubmitOrderAndClearCart(Order UserOrder, UserCache pUserCache)
        {
            ServiceFactory.Instance.OrderService.SubmitOrder(UserOrder);
            pUserCache.UpdateUserCache();
            Clear();
        }

        public void RemoveLine(Book pBook)
        {
            mOrderItems.RemoveAll(oi => oi.Book.Id == pBook.Id);
        }
    }
}