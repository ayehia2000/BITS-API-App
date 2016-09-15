using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Security.Claims;
using System.IdentityModel.Tokens;
using System.Diagnostics;
using BITSBusTimesDataAPI.Models;
using System.Configuration;

namespace BITSBusTimesDataAPI.Controllers
{
    public class BITSBusTimesController : ApiController
    {
        // Uncomment following lines for service principal authentication
        //private static string trustedCallerClientId = ConfigurationManager.AppSettings["todo:TrustedCallerClientId"];
        //private static string trustedCallerServicePrincipalId = ConfigurationManager.AppSettings["todo:TrustedCallerServicePrincipalId"];

        private static Dictionary<int, BITSBusTime> mockData = new Dictionary<int, BITSBusTime>();

        static BITSBusTimesController()
        {
            mockData.Add(0, new BITSBusTime { OriginID = 0, Owner = "*", Description = "feed the dog" });
            mockData.Add(1, new BITSBusTime { OriginID = 1, Owner = "*", Description = "take the dog on a walk" });
        }

        private static void CheckCallerId()
        {
            // Uncomment following lines for service principal authentication
            //string currentCallerClientId = ClaimsPrincipal.Current.FindFirst("appid").Value;
            //string currentCallerServicePrincipalId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            //if (currentCallerClientId != trustedCallerClientId || currentCallerServicePrincipalId != trustedCallerServicePrincipalId)
            //{
            //    throw new HttpResponseException(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized, ReasonPhrase = "The appID or service principal ID is not the expected value." });
            //}
        }

        // GET: api/ToDoItemList
        public IEnumerable<BITSBusTime> Get(string owner)
        {
            CheckCallerId();

            return mockData.Values.Where(m => m.Owner == owner || owner == "*");
        }

        // GET: api/ToDoItemList/5
        public BITSBusTime GetById(string owner, int id)
        {
            CheckCallerId();

            return mockData.Values.Where(m => (m.Owner == owner || owner == "*" ) && m.OriginID == id).First();
        }

        // POST: api/ToDoItemList
        public void Post(BITSBusTime BusTime)
        {
            CheckCallerId();

            BusTime.OriginID = mockData.Count > 0 ? mockData.Keys.Max() + 1 : 1;
            mockData.Add(BusTime.OriginID, BusTime);
        }

        public void Put(BITSBusTime BusTime)
        {
            CheckCallerId();

            BITSBusTime xBusTime = mockData.Values.First(a => (a.Owner == BusTime.Owner || BusTime.Owner == "*") && a.OriginID == BusTime.OriginID);
            if (BusTime != null && xBusTime != null)
            {
                xBusTime.Description = BusTime.Description;
            }
        }

        // DELETE: api/ToDoItemList/5
        public void Delete(string owner, int id)
        {
            CheckCallerId();

            BITSBusTime BusTime = mockData.Values.First(a => (a.Owner == owner || owner == "*") && a.OriginID == id);
            if (BusTime != null)
            {
                mockData.Remove(BusTime.OriginID);
            }
        }
    }
}

