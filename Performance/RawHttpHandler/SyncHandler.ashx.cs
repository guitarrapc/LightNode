﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace RawHttpHandler
{
    public class SyncHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var name = context.Request.QueryString.Get("name");
            var x = int.Parse(context.Request.QueryString.Get("x"));
            var y = int.Parse(context.Request.QueryString.Get("y"));
            var e = Enum.Parse(typeof(MyEnum), context.Request.QueryString.Get("e"));

            var mc = new MyClass { Name = name, Sum = (x + y) * (int)e };

            context.Response.ContentType = "application/json";

            var json = JsonConvert.SerializeObject(mc);
            var enc = System.Text.Encoding.UTF8.GetBytes(json);
            context.Response.ContentType = "application/json";
            context.Response.OutputStream.Write(enc, 0, enc.Length);
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }


    public class MyClass
    {
        public string Name { get; set; }
        public int Sum { get; set; }
    }

    public enum MyEnum
    {
        A = 2,
        B = 3,
        C = 4
    }

}