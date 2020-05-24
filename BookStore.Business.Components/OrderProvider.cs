﻿using System;
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
    public class OrderProvider : IOrderProvider
    {
        public IEmailProvider EmailProvider
        {
            get { return ServiceLocator.Current.GetInstance<IEmailProvider>(); }
        }

        public IUserProvider UserProvider
        {
            get { return ServiceLocator.Current.GetInstance<IUserProvider>(); }
        }

        private IWarehouseProvider WarehouseProvider
        {
            get { return ServiceLocator.Current.GetInstance<IWarehouseProvider>(); }
        }

        public int[][] WarehouseMatrix;

        public Order ConfirmOrder(Entities.Order pOrder)
        {
            using (TransactionScope lScope = new TransactionScope())
            {
                //LoadBookStocks(pOrder);
                //MarkAppropriateUnchangedAssociations(pOrder);

                using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
                {
                    try
                    {
                        pOrder.OrderNumber = Guid.NewGuid();
                        pOrder.Store = "OnLine";

                        // Book objects in pOrder are missing the link to their Stock tuple (and the Stock GUID field)
                        // so fix up the 'books' in the order with well-formed 'books' with 1:1 links to Stock tuples
                        foreach (OrderItem lOrderItem in pOrder.OrderItems)
                        {
                            int bookId = lOrderItem.Book.Id;
                            lOrderItem.Book = lContainer.Books.Where(book => bookId == book.Id).First();
                            System.Guid stockId = lOrderItem.Book.Stock.Id;
                            lOrderItem.Book.Stock = lContainer.Stocks.Where(stock => stockId == stock.Id).First();
                        }

                        // confirm the order can be completed and from which warehouses 
                        int[][] confirmedOrders = ConfirmOrderWarehouseLogic(pOrder);

                        // an error has occured when confirming the order
                        if (confirmedOrders[0][0] == -1)
                        {
                            SendOrderFailedConfirmation(pOrder);
                            throw new Exception("There is not enough stock in the warehouses to complete the order.");
                        }

                        // and update the stock levels
                        pOrder.UpdateStockLevels();

                        // add the modified Order tree to the Container (in Changed state)
                        lContainer.Orders.Add(pOrder);

                        // ask the Bank service to transfer fundss
                        TransferFundsFromCustomer(UserProvider.ReadUserById(pOrder.Customer.Id).BankAccountNumber, pOrder.Total ?? 0.0);

                        // and save the order
                        lContainer.SaveChanges();
                        lScope.Complete();
                    }
                    catch (Exception lException)
                    {
                        SendOrderErrorMessage(pOrder, lException);
                        IEnumerable<System.Data.Entity.Infrastructure.DbEntityEntry> entries = lContainer.ChangeTracker.Entries();
                        throw;
                    }
                }
            }
            return pOrder;
        }
        public void CancelOrder(int UserOrderID)
        {
            using (TransactionScope lScope = new TransactionScope())
            {
                using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
                {
                    Order UserOrder = lContainer.Orders.Find(UserOrderID); 

                    //re-add the stock quantities from the order back to the stock 
                    UserOrder.ResetStockLevels();

                    //give the customer their money back
                    TransferFundsToCustomer(UserProvider.ReadUserById(UserOrder.Customer.Id).BankAccountNumber, UserOrder.Total ?? 0.0);

                    //delete order from order table
                    string SQL = "DELETE FROM [dbo].Orders WHERE OrderNumber = {0}";
                    lContainer.Database.ExecuteSqlCommand(SQL, UserOrder.OrderNumber);

                    lContainer.SaveChanges();
                    lScope.Complete();
                }
            }
        }
        public void SubmitOrder(Entities.Order pOrder)
        {      
            using (TransactionScope lScope = new TransactionScope())
            {
                //LoadBookStocks(pOrder);
                //MarkAppropriateUnchangedAssociations(pOrder);

                using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
                {
                    try
                    {
                        //get the warehouses again for logging when doing the delivery 
                        int[][] confirmedOrders = ConfirmOrderWarehouseLogicSave(pOrder);

                        // ask the delivery service to organise delivery
                        PlaceDeliveryForOrder(pOrder, confirmedOrders);

                        // and save the order
                        lContainer.SaveChanges();
                        lScope.Complete();
                    }
                    catch (Exception lException)
                    {
                        SendOrderErrorMessage(pOrder, lException);
                        IEnumerable<System.Data.Entity.Infrastructure.DbEntityEntry> entries = lContainer.ChangeTracker.Entries();
                        throw;
                    }
                }
            }
            SendOrderPlacedConfirmation(pOrder);
        }

        //private void MarkAppropriateUnchangedAssociations(Order pOrder)
        //{
        //    pOrder.Customer.MarkAsUnchanged();
        //    pOrder.Customer.LoginCredential.MarkAsUnchanged();
        //    foreach (OrderItem lOrder in pOrder.OrderItems)
        //    {
        //        lOrder.Book.Stock.MarkAsUnchanged();
        //        lOrder.Book.MarkAsUnchanged();
        //    }
        //}

        private void LoadBookStocks(Order pOrder)
        {
            using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
            {
                foreach (OrderItem lOrderItem in pOrder.OrderItems)
                {
                    lOrderItem.Book.Stock = lContainer.Stocks.Where((pStock) => pStock.Book.Id == lOrderItem.Book.Id).FirstOrDefault();
                }
            }
        }

        private void SendOrderErrorMessage(Order pOrder, Exception pException)
        {
            EmailProvider.SendMessage(new EmailMessage()
            {
                ToAddress = pOrder.Customer.Email,
                Message = "There was an error in processsing your order " + pOrder.OrderNumber + ": " + pException.Message + ". Please contact Book Store"
            });
        }

        private void SendOrderPlacedConfirmation(Order pOrder)
        {
            EmailProvider.SendMessage(new EmailMessage()
            {
                ToAddress = pOrder.Customer.Email,
                Message = "Your order " + pOrder.OrderNumber + " has been placed"
            });
        }

        private void SendOrderFailedConfirmation(Order pOrder)
        {
            EmailProvider.SendMessage(new EmailMessage()
            {
                ToAddress = pOrder.Customer.Email,
                Message = "Your order " + pOrder.OrderNumber + " has failed because there is not enough stock."
            });
        }

        private void PlaceDeliveryForOrder(Order pOrder, int[][] confirmedOrders)
        {
            Delivery lDelivery = new Delivery() { DeliveryStatus = DeliveryStatus.Submitted, SourceAddress = "Book Store Address", DestinationAddress = pOrder.Customer.Address, Order = pOrder };

            Guid lDeliveryIdentifier = ExternalServiceFactory.Instance.DeliveryService.SubmitDelivery(new DeliveryInfo()
            { 
                OrderNumber = lDelivery.Order.OrderNumber.ToString(),  
                SourceAddress = lDelivery.SourceAddress,
                DestinationAddress = lDelivery.DestinationAddress,
                DeliveryNotificationAddress = "net.tcp://localhost:9010/DeliveryNotificationService"
            },
            confirmedOrders
            );

            lDelivery.ExternalDeliveryIdentifier = lDeliveryIdentifier;
            pOrder.Delivery = lDelivery;   
        }

        private void TransferFundsFromCustomer(int pCustomerAccountNumber, double pTotal)
        {
            try
            {
                ExternalServiceFactory.Instance.TransferService.Transfer(pTotal, pCustomerAccountNumber, RetrieveBookStoreAccountNumber());
            }
            catch
            {
                throw new Exception("Error when transferring funds for order.");
            }
        }
        private void TransferFundsToCustomer(int pCustomerAccountNumber, double pTotal)
        {
            try
            {
                ExternalServiceFactory.Instance.TransferService.Transfer(pTotal, RetrieveBookStoreAccountNumber(), pCustomerAccountNumber);
            }
            catch
            {
                throw new Exception("Error when transferring funds for cancelled order.");
            }
        }

        private int[][] ConfirmOrderWarehouseLogic(Order pOrder)
        {
            return WarehouseProvider.ProcessOrder(pOrder, 0);
        }

        private int[][] ConfirmOrderWarehouseLogicSave(Order pOrder)
        {
            return WarehouseProvider.ProcessOrder(pOrder, 1);
        }

        private int RetrieveBookStoreAccountNumber()
        {
            return 123;
        }
    }
}
