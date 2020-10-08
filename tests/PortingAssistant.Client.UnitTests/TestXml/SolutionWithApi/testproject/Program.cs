using System;
using Newtonsoft.Json;

namespace Test2
{
    class Program
    {
        static void print()
        {
            var macbook = new Computer
            {
                Vendor = "apple Inc",
                produceDate = new DateTime(),
                price = "1200$"
            };

            string json = JsonConvert.SerializeObject(macbook);

            Console.WriteLine("my new computer is {0}", json);
        }

        public class Computer
        {
            public string Vendor { get; set; }
            public DateTime produceDate { get; set; }
            public string price { get; set; }

        }
    }
}
