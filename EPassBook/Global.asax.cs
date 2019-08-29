﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using EPassBook.DAL.IService;

namespace EPassBook
{
    public class MvcApplication : System.Web.HttpApplication
    {
        IUserService _userService;
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            UnityConfig.RegisterComponents();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
        protected void Session_End()
        {
            var UserId = "";
            HttpCookie objRequestRead = Request.Cookies["UserId"];
            if (objRequestRead != null)
            {
                UserId = objRequestRead.Value;
            }

            if (UserId != null || UserId != "")
            {
                var um = _userService.Get().Where(u => u.UserId == Convert.ToInt32(UserId)).FirstOrDefault();
                um.IsLoggedIn = false;
                _userService.Update(um);
                _userService.SaveChanges();
            }
        }
    }
}
