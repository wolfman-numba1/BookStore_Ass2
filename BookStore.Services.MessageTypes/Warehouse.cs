using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.Services.MessageTypes
{
    class Warehouse : MessageType
    {
        Dictionary<string, string> Stocks { get; set; }
    }
}
