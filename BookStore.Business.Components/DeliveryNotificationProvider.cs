﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BookStore.Business.Components.Interfaces;
using BookStore.Business.Entities;
using Microsoft.Practices.ServiceLocation;
using System.Transactions;
using System.ServiceModel;
using System.Diagnostics;

namespace BookStore.Business.Components
{
    public class DeliveryNotificationProvider : IDeliveryNotificationProvider
    {
        public IEmailProvider EmailProvider
        {
            get { return ServiceLocator.Current.GetInstance<IEmailProvider>(); }
        }

        public void NotifyDeliveryCompletion(Guid pDeliveryId, Entities.DeliveryStatus status)
        {
            Order lAffectedOrder = RetrieveDeliveryOrder(pDeliveryId);
            UpdateDeliveryStatus(pDeliveryId, status);
            if (status == Entities.DeliveryStatus.Delivered)
            {
                try
                {
                    EmailProvider.SendMessage(new EmailMessage()
                    {
                        ToAddress = lAffectedOrder.Customer.Email,
                        Message = "Our records show that your order" + lAffectedOrder.OrderNumber + " has been delivered. Thank you for shopping at video store"
                    });
                }
                catch(EndpointNotFoundException)
                {
                    Debug.WriteLine("There seems to be a problem with your email process. try turning it on to get the full email message.");
                }

            }
            if (status == Entities.DeliveryStatus.Failed)
            {
                try
                {
                    EmailProvider.SendMessage(new EmailMessage()
                    {
                        ToAddress = lAffectedOrder.Customer.Email,
                        Message = "Our records show that there was a problem" + lAffectedOrder.OrderNumber + " delivering your order. Please contact Book Store"
                    });
                }
                catch (EndpointNotFoundException)
                {
                    Debug.WriteLine("There seems to be a problem with your email process. try turning it on to get the full email message.");
                }
            }
        }

        private void UpdateDeliveryStatus(Guid pDeliveryId, DeliveryStatus status)
        {
            using (TransactionScope lScope = new TransactionScope())
            using (BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
            {
                Delivery lDelivery = lContainer.Deliveries.Where((pDel) => pDel.ExternalDeliveryIdentifier == pDeliveryId).FirstOrDefault();
                if (lDelivery != null)
                {
                    lDelivery.DeliveryStatus = status;
                    lContainer.SaveChanges();
                }
                lScope.Complete();
            }
        }

        private Order RetrieveDeliveryOrder(Guid pDeliveryId)
        {
 	        using(BookStoreEntityModelContainer lContainer = new BookStoreEntityModelContainer())
            {
                Delivery lDelivery =  lContainer.Deliveries.Include("Order.Customer").Where((pDel) => pDel.ExternalDeliveryIdentifier == pDeliveryId).FirstOrDefault();
                return lDelivery.Order;
            }
        }
    }


}
