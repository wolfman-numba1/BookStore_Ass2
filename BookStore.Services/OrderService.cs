﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BookStore.Services.Interfaces;
using BookStore.Business.Components.Interfaces;
using Microsoft.Practices.ServiceLocation;
using BookStore.Services.MessageTypes;

using System.ServiceModel;

namespace BookStore.Services
{
    public class OrderService : IOrderService
    {

        private IOrderProvider OrderProvider
        {
            get
            {
                return ServiceFactory.GetService<IOrderProvider>();
            }
        }

        public Order ConfirmOrder(Order pOrder)
        {
            try
            { 
                var internalResult = OrderProvider.ConfirmOrder(
                    MessageTypeConverter.Instance.Convert<BookStore.Services.MessageTypes.Order,BookStore.Business.Entities.Order>
                    (pOrder));
                var externalResult = MessageTypeConverter.Instance.Convert<BookStore.Business.Entities.Order, BookStore.Services.MessageTypes.Order>
                    (internalResult);
                return externalResult;
            }
            catch(BookStore.Business.Entities.InsufficientStockException ise)
            {
                throw new FaultException<InsufficientStockFault>(
                    new InsufficientStockFault() { ItemName = ise.ItemName });
            }
        }

        public void CancelOrder(int userOrder)
        {
            try
            {
                OrderProvider.CancelOrder(userOrder);
            }
            //fix this later
            catch (BookStore.Business.Entities.InsufficientStockException ise)
            {
                throw new FaultException<InsufficientStockFault>(
                    new InsufficientStockFault() { ItemName = ise.ItemName });
            }
        }
        public void SubmitOrder(Order pOrder)
        {
            try
            {
                OrderProvider.SubmitOrder(
                    MessageTypeConverter.Instance.Convert<
                    BookStore.Services.MessageTypes.Order,
                    BookStore.Business.Entities.Order>(pOrder)
                );
            }
            catch(BookStore.Business.Entities.InsufficientStockException ise)
            {
                throw new FaultException<InsufficientStockFault>(
                    new InsufficientStockFault() { ItemName = ise.ItemName });
            }
        }
    }
}
