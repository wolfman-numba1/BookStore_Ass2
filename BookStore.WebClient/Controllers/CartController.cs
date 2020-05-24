using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Web;
using System.Web.Mvc;
using BookStore.Services.MessageTypes;
using BookStore.WebClient.ClientModels;
using BookStore.WebClient.ViewModels;

namespace BookStore.WebClient.Controllers
{
    public class CartController : Controller
    {
        public ViewResult Index(Cart pCart, string pReturnUrl)
        {
            ViewData["returnUrl"] = pReturnUrl;
            ViewData["CurrentCategory"] = "Cart";
            return View(pCart);
        }

        public RedirectToRouteResult AddToCart(Cart pCart, int pBookId, string pReturnUrl)
        {
            pCart.AddItem(FetchBookById(pBookId), 1);
            return RedirectToAction("Index", new { pReturnUrl });
        }


        public RedirectToRouteResult RemoveFromCart(Cart pCart, int pBookId, string pReturnUrl)
        {
            pCart.RemoveLine(FetchBookById(pBookId));
            return RedirectToAction("Index", new { pReturnUrl });
        }

        //new method to take user to a webpage to potentially cancel their order
        //here is where the method will do warehouse logic and bank transfers on the backend
        public ActionResult ConfirmOrder(Cart pCart, UserCache pUser)
        {
            try
            {
                Order UserOrder = pCart.ConfirmOrder(pUser);
                return View(new ConfirmOrderViewModel(UserOrder));
            }
            catch
            {
                pCart.Clear();
                pUser.UpdateUserCache();
                return RedirectToAction("ErrorPage");
            }
        }

        //new method to help a user cancel their order
        public ActionResult CancelOrder(int UserOrder, Cart pCart, UserCache pUser)
        {
            try
            {
                pCart.CancelOrder(UserOrder, pUser);
            }
            catch
            {
                pCart.Clear();
                pUser.UpdateUserCache();
                return RedirectToAction("ErrorPage");
            }
            return View();
        }
        //fix this submitOrder method below
        public ActionResult SubmitOrder(Order UserOrder, Cart pCart, UserCache pUser)
        {
            try
            {
                pCart.SubmitOrderAndClearCart(UserOrder, pUser);
            }
            catch
            {
                pCart.Clear();
                pUser.UpdateUserCache();
                return RedirectToAction("ErrorPage");
            }
            return View(new CheckOutViewModel(pUser.Model));
        }

        public ActionResult ContinueShopping()
        {
            return View(new CatalogueViewModel());
        }

        public ActionResult ErrorPage()
        {
            return View();
        }

        public ViewResult Summary(Cart pCart)
        {
            return View(pCart);
        }

        public ActionResult InsufficientStock(String pItem)
        {
            return View(new InsufficientStockViewModel(pItem));
        }

        private Book FetchBookById(int pId)
        {
            return ServiceFactory.Instance.CatalogueService.GetBookById(pId);
        }
    }
}