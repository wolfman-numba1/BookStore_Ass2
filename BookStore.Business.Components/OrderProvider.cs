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
using System.ServiceModel;
using System.Diagnostics;

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

        public int[][] WarehouseMatrix;

        public Order ConfirmOrder(Entities.Order pOrder)

            //checks if the order is possible
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
                        pOrder.ProcessStatus = 0;

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
                        Debug.WriteLine(pOrder.ProcessStatus);
                        // an error has occured when confirming the order
                        if (confirmedOrders[0][0] == -1)
                        {
                            SendOrderFailedConfirmation(pOrder);
                            pOrder.ProcessStatus = 1;
                            Debug.WriteLine(pOrder.ProcessStatus);
                            return pOrder;
                        }
                        Debug.WriteLine(pOrder.ProcessStatus);
                        // and update the stock levels
                        try
                        {
                            pOrder.UpdateStockLevels();
                        }
                        catch (Exception)
                        {
                            pOrder.ProcessStatus = 1;
                            return pOrder;
                        }

                        // add the modified Order tree to the Container (in Changed state)
                        lContainer.Orders.Add(pOrder);

                        // ask the Bank service to transfer fundss
                        lContainer.SaveChanges();

                        //TransferFundsFromCustomer(UserProvider.ReadUserById(pOrder.Customer.Id).BankAccountNumber, pOrder.Total ?? 0.0, pOrder.OrderNumber.ToString());
                        try
                        {
                            TransferFundsFromCustomer(UserProvider.ReadUserById(pOrder.Customer.Id).BankAccountNumber, pOrder.Total ?? 0.0, pOrder.Id.ToString(), pOrder.Customer.Email);
                        }
                        catch (EndpointNotFoundException)
                        {
                            pOrder.ProcessStatus = 2;
                            return pOrder;
                        }

                        // and save the order
                        lContainer.SaveChanges();
                        lScope.Complete();
                    }
                    catch (Exception lException)
                    {
                        try
                        {
                            SendOrderErrorMessage(pOrder, lException);
                        }
                        catch
                        {
                            Debug.WriteLine("Email process is off. Switch on please in order to maintain communication with BookStore");
                        }
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

                    //make use of the message queues here
                    try
                    {
                        //give the customer their money back
                        TransferFundsToCustomer(UserProvider.ReadUserById(UserOrder.Customer.Id).BankAccountNumber, UserOrder.Total ?? 0.0, (UserOrder.Id).ToString(), UserOrder.Customer.Email);
                    }
                    catch (EndpointNotFoundException)
                    {
                        Debug.WriteLine("Bank process not found please switch on for a full refund.");
                    }
                    //soft delete order from order table
                    UserOrder.Deleted = true;

                    //save changes
                    lContainer.SaveChanges();
                    lScope.Complete();
                }
            }
        }
        public void SubmitOrder(int UserOrderID)

            //automatic one
        {
            using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
            {
                Order pOrder = lContainer.Orders.Find(UserOrderID);
                using (TransactionScope lScope = new TransactionScope())
            {
                //LoadBookStocks(pOrder);
                //MarkAppropriateUnchangedAssociations(pOrder);
                    try
                    {
                        //get the warehouses again for logging when doing the delivery 
                        int[][] confirmedOrders = ConfirmOrderWarehouseLogicSave(pOrder);

                        // ask the delivery service to organise delivery
                        try
                        {
                            PlaceDeliveryForOrder(pOrder, confirmedOrders);
                        }
                        catch(EndpointNotFoundException)
                        {
                            Debug.WriteLine("DeliveryCo process is offline. Please switch back on for your delivery to be submitted.");
                            return;
                        }

                        // and save the order
                        lContainer.SaveChanges();
                        lScope.Complete();

                    }
                    catch (Exception lException)
                    {
                        try
                        {
                            SendOrderErrorMessage(pOrder, lException);
                        }
                        catch
                        {
                            Debug.WriteLine("An error has occurred but your email process is switched off. Please switch back on the email process to find out more.");
                        }
                    }
                }
                try
                {
                    SendOrderPlacedConfirmation(pOrder);
                }
                catch
                {
                    Debug.WriteLine("Email process is off. Switch on please in order to maintain communication with BookStore");
                }
            }
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

        private void TransferFundsFromCustomer(int pCustomerAccountNumber, double pTotal, string pReference, string customerEmail)
        {
            //String reference

                //   TransferServiceClient lClient = new TransferServiceClient();
                //  String orderServiceAddress = "net.msmq://localhost/private/TransferNotificationQueueTransacted";


               //string queueName = ".\\private$\\BankTransferTransacted";
               string queueName = ".\\private$\\TransferNotifyMessageQueue";
               string queueBank = ".\\private$\\BankTransferTransacted";
               string queueNotifyReference = "net.msmq://localhost/private/TransferNotifyMessageQueue";
               string queueReference = "net.msmq://localhost/private/BankTransferTransacted";

                EnsureQueueExists(queueName);
            try
            {
                ExternalServiceFactory.Instance.TransferService.Transfer(pTotal, pCustomerAccountNumber, RetrieveBookStoreAccountNumber(), queueNotifyReference, pReference, customerEmail);
            }
            catch
            {
                throw new EndpointNotFoundException("Bank process does not seem to be running. Please turn on and try again.");
            }
        }
        private void TransferFundsToCustomer(int pCustomerAccountNumber, double pTotal, string pReference, string customerEmail)
        {
            try
            {

                //string queueName = ".\\private$\\BankTransferTransacted";
                string queueName = ".\\private$\\TransferNotifyMessageQueue";
                string queueBank = ".\\private$\\BankTransferTransacted";
                string queueNotifyReference = "net.msmq://localhost/private/TransferNotifyMessageQueue";
                string queueReference = "net.msmq://localhost/private/BankTransferTransacted";

                EnsureQueueExists(queueName);
                ExternalServiceFactory.Instance.TransferService.Transfer(pTotal, RetrieveBookStoreAccountNumber(), pCustomerAccountNumber, queueNotifyReference, pReference, customerEmail);
            }
            catch (EndpointNotFoundException)
            {
                throw new EndpointNotFoundException("Bank process does not seem to be running. Please turn on and try again.");
            }
            catch
            {
                throw new Exception("Error when transferring funds for cancelled order.");
            }
        }

        private int[][] ConfirmOrderWarehouseLogic(Order pOrder)
        {
            return WarehouseProvider.ProcessOrder(pOrder);
        }

        private int[][] ConfirmOrderWarehouseLogicSave(Order pOrder)
        {
            return WarehouseProvider.ProcessOrderSave(pOrder);
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


        public void NotifyTransferSuccess(string pOrderNumber, string customerEmail)
        {
            using (var lScope = new TransactionScope())
            {


                using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
                {
                   Order pOrder = lContainer.Orders.Find(Int32.Parse(pOrderNumber));
                   

                    //    var order = lContainer.Orders.Include("Customer").FirstOrDefault(pOrder => pOrder.OrderNumber == orderNumber);
                   // try
                   // {
                     //   if (pOrder != null)
                     //   {
                        
                            EmailProvider.SendMessage(new EmailMessage()
                            {
                                //  ToAddress = pOrder.Customer.Email,
                                ToAddress = customerEmail,
                               // Message = "Transaction was successful and your money has been transfered for your order number " + (pOrder.OrderNumber).ToString()
                                Message = "Transaction was successful and your money has been transfered for your order " + (pOrder.Id).ToString()
                            });

                            lScope.Complete();
                        /*}
                    }
                    catch (Exception lException)
                    {
                        SendOrderErrorMessage(pOrder, lException);
                        throw;
                    }
                    */
                }
            }
        }

        public void NotifyTransferFailed(string pOrderNumber, string reason, string customerEmail)
        {
           using (var lScope = new TransactionScope())
            {
                /*var orderNumber = Guid.Parse(pOrderNumber);
                using (var lContainer = new BookStoreEntityModelContainer())
                {
                */

                 using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
                {

                    //NEED TO RE-ADD back the reset stock level
                    Order pOrder = lContainer.Orders.Find(Int32.Parse(pOrderNumber));
                    pOrder.ResetStockLevels();

                    //    var order = lContainer.Orders.Include("Customer").FirstOrDefault(pOrder => pOrder.OrderNumber == orderNumber);

                    EmailProvider.SendMessage(new EmailMessage()
                    {
                        ToAddress = customerEmail,
                        Message = "There was an error in processsing your order " + pOrderNumber + " " + reason

                                
                        // Message = "There was an error in processsing your order, get in contact with the BookStore team so we can help you out!"
                    }) ;

                    lScope.Complete();
                       
                }
               
              
            }
        }

    }
}
