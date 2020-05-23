﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace Bank.Services.Interfaces
{

    [ServiceContract]
    public interface ITransferNotificationService
    {
        [OperationContract(IsOneWay = true)]
        void NotifyTransferSuccess(String reference);

        [OperationContract(IsOneWay = true)]
        void NotifyTransferFailed(String reference, String reason);
    }
}
