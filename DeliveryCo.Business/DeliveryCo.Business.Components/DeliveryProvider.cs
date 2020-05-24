using System;
using DeliveryCo.Business.Components.Interfaces;
using System.Transactions;
using DeliveryCo.Business.Entities;
using System.Threading;
using DeliveryCo.Services.Interfaces;
using EmailService.Services.Interfaces;
using BookStore.Business.Components.Interfaces;

namespace DeliveryCo.Business.Components
{
    public class DeliveryProvider : IDeliveryProvider
    {
        public IEmailProvider EmailProvider
        {
            get {
                return EmailService.Services.ServiceFactory.GetService<IEmailProvider>(); 
            }
        }

        public Guid SubmitDelivery(DeliveryCo.Business.Entities.DeliveryInfo pDeliveryInfo, int[][] confirmedOrders)
        {
            using(TransactionScope lScope = new TransactionScope())
            using(DeliveryCoEntityModelContainer lContainer = new DeliveryCoEntityModelContainer())
            {
                pDeliveryInfo.DeliveryIdentifier = Guid.NewGuid();
                pDeliveryInfo.Status = 0;
                lContainer.DeliveryInfo.Add(pDeliveryInfo);
                lContainer.SaveChanges();
                ThreadPool.QueueUserWorkItem(new WaitCallback((pObj) => ScheduleDelivery(pDeliveryInfo, confirmedOrders)));
                lScope.Complete();
            }
            return pDeliveryInfo.DeliveryIdentifier;
        }

        private void ScheduleDelivery(DeliveryInfo pDeliveryInfo, int[][] confirmedOrders)
        {
            Console.WriteLine("Request has been received for at least one delivery to " + pDeliveryInfo.DestinationAddress);
            Thread.Sleep(2000);

            for (int i = 0; i < confirmedOrders.GetLength(0); i++)
            {
                Console.WriteLine(confirmedOrders[i][2] + " Book(s) with ID " + confirmedOrders[i][0] + " have been picked up from warehouse with ID " + confirmedOrders[i][1]);
                Thread.Sleep(1000);
            }

            for (int i = 0; i < confirmedOrders.GetLength(0); i++)
            {
                Console.WriteLine(confirmedOrders[i][2] + " Book(s) with ID " + confirmedOrders[i][0] + " are on a delivery truck");
                Thread.Sleep(1000);
            }

            //notifying of delivery completion
            using (TransactionScope lScope = new TransactionScope())
            using (DeliveryCoEntityModelContainer lContainer = new DeliveryCoEntityModelContainer())
            {
                pDeliveryInfo.Status = 1;
                IDeliveryNotificationService lService = DeliveryNotificationServiceFactory.GetDeliveryNotificationService(pDeliveryInfo.DeliveryNotificationAddress);
                lService.NotifyDeliveryCompletion(pDeliveryInfo.DeliveryIdentifier, DeliveryInfoStatus.Delivered);
            }

            for (int i = 0; i < confirmedOrders.GetLength(0); i++)
            {
                Console.WriteLine(confirmedOrders[i][2] + " Book(s) with ID " + confirmedOrders[i][0] + " have been delivered");
                Thread.Sleep(1000);
            }
        }
    }
}
