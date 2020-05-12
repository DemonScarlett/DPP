﻿using MilSpace.Tools.Sentinel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MilSpace.Web
{
    class Program
    {
        static void Main(string[] args)
        {

            string text;
            WebRequest myWebRequest = WebRequest.Create("https://scihub.copernicus.eu/dhus/odata/v1/Products?$format=json");
        //            WebRequest myWebRequest = WebRequest.Create("https://scihub.copernicus.eu/dhus/odata/v1/Products('ad83ef2b-4f12-4bca-8df9-f7026c63e90c')$format=json");

        https://scihub.copernicus.eu/dhus/api/stub/products?filter=(footprint:%22Intersects(POLYGON((20.0%2044.0,43.0%2044.0,43.0%2054.0,20.0%2054.0,20.0%2044.0)))%22%20)%20AND%20(%20beginPosition:[2020-03-27T00:00:00.000Z%20TO%202020-04-08T23:59:59.999Z]%20AND%20endPosition:[2020-03-27T00:00:00.000Z%20TO%202020-04-08T23:59:59.999Z]%20)%20AND%20(%20%20(platformname:Sentinel-1%20AND%20filename:S1A_*%20AND%20producttype:SLC%20AND%20sensoroperationalmode:IW))&offset=0&limit=150&sortedby=ingestiondate&order=desc

            myWebRequest.Credentials = new NetworkCredential("spaero", "spaero3404558");

            // Send the 'WebRequest' and wait for response.
            using (WebResponse myWebResponse = myWebRequest.GetResponse())
            {
                using (var reader = new StreamReader(myWebResponse.GetResponseStream()))
                {
                    text = reader.ReadToEnd();
                }

                myWebResponse.Close();
            }

            ImportManager.ReadJson(text);

        }
    }
}