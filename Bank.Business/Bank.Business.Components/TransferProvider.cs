using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bank.Business.Components.Interfaces;
using Bank.Business.Entities;
using BookStore.Business.Entities;
using System.Transactions;
using System.Data;
using System.Data.Entity.Infrastructure;
using Bank.Services.Interfaces;
using System.ServiceModel;

namespace Bank.Business.Components
{
    public class TransferProvider : ITransferProvider
    {


        public void Transfer(double pAmount, int pFromAcctNumber, int pToAcctNumber, string pNotificationAddress, string pReference, string customerEmail)
        {

            /*  IOutcomeNotificationService notify;
               try
               {
                   ChannelFactory<IOutcomeNotificationService> lChannelFactory =
                       new ChannelFactory<IOutcomeNotificationService>(new NetMsmqBinding("NetMsmqBinding_IOperationOutcomeService"), new EndpointAddress(pReference));
                   notify = lChannelFactory.CreateChannel();
               }
               catch (Exception lException)
               {
                   Console.WriteLine("Error occurred:  " + lException.Message);
                   throw;
               }
               */

          try
           {

                // OperationOutcome outcome = new OperationOutcome();
                using (TransactionScope lScope = new TransactionScope())
                using (BankEntityModelContainer lContainer = new BankEntityModelContainer())
                {

                    try
                    {
                        // find the two account entities and add them to the Container
                        Account lFromAcct = lContainer.Accounts.Where(account => pFromAcctNumber == account.AccountNumber).First();
                        Account lToAcct = lContainer.Accounts.Where(account => pToAcctNumber == account.AccountNumber).First();

                        // update the two accounts
                        lFromAcct.Withdraw(pAmount);
                        lToAcct.Deposit(pAmount);

                        // save changed entities and finish the transaction
                        lContainer.SaveChanges();
                       // lScope.Complete();

                       
                        //  outcome.Outcome = OperationOutcome.OperationOutcomeResult.Successful;
                        //  outcome.Message = "Success! Transfer at " + DateTime.Now + ": From account number " + pFromAcctNumber + " to " + pToAcctNumber + " of " + pAmount + " was successful.";

                        //Console.WriteLine("Sucess! Transfer at  " + DateTime.Now + ": From account number "+ pFromAcctNumber + " to " + pToAcctNumber + " of " + pAmount + " was successful.");

                       // Console.WriteLine("Notification address " + pNotificationAddress);
                       // TransferNotificationServiceFactory.GetTransferNotificationService(pNotificationAddress)
                       //      .NotifyTransferSuccess(pReference);

                       
                       ITransferNotificationService lChannel = TransferNotificationServiceFactory.GetTransferNotificationService(pNotificationAddress);
                        lChannel.NotifyTransferSuccess(pReference, customerEmail);

                        Console.WriteLine("Sucess! Transfer at  " + DateTime.Now + ": From account number " + pFromAcctNumber + " to " + pToAcctNumber + " of " + pAmount + " was successful.");
                       

                       // Console.WriteLine("Channel notify success message " + pReference);

                       // Console.WriteLine("Sucess! Transfer at  " + DateTime.Now + ": From account number " + pFromAcctNumber + " to " + pToAcctNumber + " of " + pAmount + " was successful.");
                       // lChannel.NotifyTransferSuccess(pReference);
                      
                        lScope.Complete();
                    }
                    catch (Exception lException)
                    {
                        Console.WriteLine("Error occured while transferring money:  " + lException.Message);
                        //outcome.Outcome = OperationOutcome.OperationOutcomeResult.Failure;
                        //  outcome.Message = "Error occured while transferring money:  " + lException.Message;
                        // outcome.Message = "Failure!! Transfer at " + DateTime.Now + ": From account number " + pFromAcctNumber + " to " + pToAcctNumber + " of " + pAmount + " was unsuccessful.";
                        throw;
                    }
                }

            }

           catch (Exception lException)
            {
                using (TransactionScope lScope = new TransactionScope(TransactionScopeOption.Suppress))
                {

                    Console.WriteLine("Channel failure message " + pReference);
                    TransferNotificationServiceFactory.GetTransferNotificationService(pNotificationAddress)
                        .NotifyTransferFailed(pReference, lException.Message, customerEmail);

                    lScope.Complete();
                }
            }
            

           
           // notify.NotifyOperationOutcome(pReference, outcome);
       }

        private Account GetAccountFromNumber(int pToAcctNumber)
        {
            using (BankEntityModelContainer lContainer = new BankEntityModelContainer())
            {
                return lContainer.Accounts.Where((pAcct) => (pAcct.AccountNumber == pToAcctNumber)).FirstOrDefault();
            }
        }
    }
}
