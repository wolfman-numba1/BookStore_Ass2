using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BookStore.Business.Components.Interfaces;
using BookStore.Business.Entities;
using Bank.Business.Components;
using System.Transactions;
using Microsoft.Practices.ServiceLocation;
using DeliveryCo.MessageTypes;
using System.Configuration;
using System.Messaging;
using Bank.Business.Entities;




namespace BookStore.Business.Components
{
    public class OrderProvider : IOrderProvider, ITransferNotificationProvider
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
                        // and update the stock levels
                        pOrder.UpdateStockLevels();

                        // add the modified Order tree to the Container (in Changed state)
                        lContainer.Orders.Add(pOrder);

                        // confirm the order can be completed and from which warehouses via confirmOrder()

                        // ask the Bank service to transfer fundss
                        TransferFundsFromCustomer(UserProvider.ReadUserById(pOrder.Customer.Id).BankAccountNumber, pOrder.Total ?? 0.0, pOrder.OrderNumber.ToString());

                        // ask the delivery service to organise delivery
                        PlaceDeliveryForOrder(pOrder);

                        // and save the order
                        lContainer.SaveChanges();
                        lScope.Complete();                    
                    }
                    catch (Exception lException)
                    {
                        SendOrderErrorMessage(pOrder, lException);
                        IEnumerable<System.Data.Entity.Infrastructure.DbEntityEntry> entries =  lContainer.ChangeTracker.Entries();
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
                Message = "There was an error in processsing your order " + pOrder.OrderNumber + ": "+ pException.Message + ". Please contact Book Store"
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

        private void PlaceDeliveryForOrder(Order pOrder)
        {
            Delivery lDelivery = new Delivery() { DeliveryStatus = DeliveryStatus.Submitted, SourceAddress = "Book Store Address", DestinationAddress = pOrder.Customer.Address, Order = pOrder };

            Guid lDeliveryIdentifier = ExternalServiceFactory.Instance.DeliveryService.SubmitDelivery(new DeliveryInfo()
            { 
                OrderNumber = lDelivery.Order.OrderNumber.ToString(),  
                SourceAddress = lDelivery.SourceAddress,
                DestinationAddress = lDelivery.DestinationAddress,
                DeliveryNotificationAddress = "net.tcp://localhost:9010/DeliveryNotificationService"
            });

            lDelivery.ExternalDeliveryIdentifier = lDeliveryIdentifier;
            pOrder.Delivery = lDelivery;   
        }

        private void TransferFundsFromCustomer(int pCustomerAccountNumber, double pTotal, String reference)
        {
            try
            {
                //   TransferServiceClient lClient = new TransferServiceClient();
                //  String orderServiceAddress = "net.msmq://localhost/private/TransferNotificationQueueTransacted";


               //string queueName = ".\\private$\\BankTransferTransacted";
               string queueName = ".\\private$\\TransferNotifyMessageQueue";
               string queueBank = ".\\private$\\BankTransferTransacted";
               string queueNotifyReference = "net.msmq://localhost/private/TransferNotifyMessageQueue";
               string queueReference = "net.msmq://localhost/private/BankTransferTransacted";

                EnsureQueueExists(queueName);

                ExternalServiceFactory.Instance.TransferService.Transfer(pTotal, pCustomerAccountNumber, RetrieveBookStoreAccountNumber(), queueNotifyReference, reference);

            }
            catch
            {
                throw new Exception("Error when transferring funds for order.");
            }
        }

        private void ConfirmOrder(Order pOrder)
        {
           // to do
        }

        private int RetrieveBookStoreAccountNumber()
        {
            return 123;
        }

        private static void EnsureQueueExists(string queueName)
        {
            // Create the transacted MSMQ queue if necessary.
            if (!MessageQueue.Exists(queueName))
                MessageQueue.Create(queueName, true);

            OperationOutcome outcome = new OperationOutcome();
            outcome.Outcome = OperationOutcome.OperationOutcomeResult.Successful;
            outcome.Message = "HAD TO CREATE A NEW QUEUE called" + queueName ;
        }


        public void NotifyTransferSuccess(string pOrderNumber)
        {
            using (var lScope = new TransactionScope())
            {
                var orderNumber = Guid.Parse(pOrderNumber);
                using (var lContainer = new BookStoreEntityModelContainer())
                {
                    // Books, Stocks
                   // Include("OrderItems")
                    var order = lContainer.Orders.Include("Customer").Include("OrderItems.Books.Stocks").FirstOrDefault(pOrder => pOrder.OrderNumber == orderNumber);
                    try
                    {
                        if (order != null)
                        {
                            order.UpdateStockLevels();

                            PlaceDeliveryForOrder(order);
                           // lContainer.Orders.ApplyChanges(order);
                          

                            lContainer.SaveChanges();
                            lScope.Complete();
                        }
                    }
                    catch (Exception lException)
                    {
                        SendOrderErrorMessage(order, lException);
                        throw;
                    }
                }
            }
        }

        public void NotifyTransferFailed(string pOrderNumber, string reason)
        {
            using (var lScope = new TransactionScope())
            {
                var orderNumber = Guid.Parse(pOrderNumber);
                using (var lContainer = new BookStoreEntityModelContainer())
                {
                    var order = lContainer.Orders.Include("Customer").FirstOrDefault(pOrder => pOrder.OrderNumber == orderNumber);
                    try
                    {
                        if (order != null)
                        {
                            EmailProvider.SendMessage(new EmailMessage()
                            {
                                ToAddress = order.Customer.Email,
                                Message = "There was an error in processsing your order " + order.OrderNumber + ": " + reason + ". Please contact Video Store"
                            });

                           lScope.Complete();
                        }
                    }
                    catch (Exception lException)
                    {
                        SendOrderErrorMessage(order, lException);
                        throw;
                    }
                }
            }
        }

    }
}
