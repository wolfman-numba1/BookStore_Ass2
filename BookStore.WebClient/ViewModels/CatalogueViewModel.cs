using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BookStore.Services.Interfaces;
using BookStore.Services.MessageTypes;

namespace BookStore.WebClient.ViewModels
{
    public class CatalogueViewModel
    {

        private ICatalogueService CatalogueService
        {
            get
            {
                return  ServiceFactory.Instance.CatalogueService;
            }
        }

        public List<Book> Items
        {
            get
            {
                try
                {
                    return CatalogueService.GetBook(0, Int32.MaxValue);
                }
                catch (System.ServiceModel.EndpointNotFoundException)
                {
                    System.Diagnostics.Debug.WriteLine(HttpContext.Current.Server.MapPath("."));
                    System.Diagnostics.Process.Start(HttpContext.Current.Server.MapPath(".") + "\\..\\..\\BookStore.Process\\bin\\Debug\\BookStore.Process.exe");
                    return CatalogueService.GetBook(0, Int32.MaxValue);
                }
            }
        }
    }
}