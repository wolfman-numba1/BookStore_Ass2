﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace DeliveryCo.Services.Interfaces
{
    public enum DeliveryInfoStatus { Submitted, Delivered, Failed }

    [ServiceContract]
    public interface IDeliveryNotificationService
    {
        [OperationContract(IsOneWay = true)]
        void NotifyDeliverySubmitted(String orderNnmber, Guid pDeliveryId, DeliveryInfoStatus status, String errorMsg);


        [OperationContract(IsOneWay = true)]
        void NotifyDeliveryCompletion(Guid pDeliveryId, DeliveryInfoStatus status);
    }
}
