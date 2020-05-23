using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BookStore.Business.Entities
{
    public partial class Order
    {
        public void UpdateStockLevels()
        {
            foreach (OrderItem lItem in this.OrderItems)
            {
                if (lItem.Book.Stock.Quantity - lItem.Quantity >= 0)
                {
                    lItem.Book.Stock.Quantity -= lItem.Quantity;
                }
                else
                {
                    throw new Exception("Cannot place an order - This book is out of stock");
                }
            }
        }

        public void ResetStockLevels()
        {
            foreach (OrderItem lItem in this.OrderItems)
            {
                lItem.Book.Stock.Quantity += lItem.Quantity;
            }
        }
    }
}
