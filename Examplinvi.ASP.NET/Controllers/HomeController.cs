﻿using System.Web.Mvc;
using Tweetinvi;
using Tweetinvi.Models;

namespace Examplinvi.ASP.NET.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult TwitterAuth()
        {
            var appCreds = new ConsumerCredentials(MyCredentials.CONSUMER_KEY, MyCredentials.CONSUMER_SECRET);
            var redirectURL = "http://" + Request.Url?.Authority + "/Home/ValidateTwitterAuth";
            var authenticationContext = AuthFlow.InitAuthentication(appCreds, redirectURL).Result;

            return new RedirectResult(authenticationContext.AuthorizationURL);
        }

        public ActionResult ValidateTwitterAuth()
        {
            var verifierCode = Request.Params.Get("oauth_verifier");
            var authorizationId = Request.Params.Get("authorization_id");

            if (verifierCode != null)
            {
                var userCreds = AuthFlow.CreateCredentialsFromVerifierCode(verifierCode, authorizationId).Result;

                var client = new TwitterClient(userCreds);
                var user = client.Account.GetAuthenticatedUser().Result;

                ViewBag.User = user;
            }

            return View();
        }
    }
}