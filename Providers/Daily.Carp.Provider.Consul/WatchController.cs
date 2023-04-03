using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Daily.Carp
{
    [Route("ConsulWatch")]
    public class WatchController:Controller
    {
        [HttpPost("Watch")]
        public string Watch([FromBody] Hashtable ht)
        {
            foreach (DictionaryEntry o in ht)
            {
                Console.WriteLine($"{o.Key},{JsonConvert.SerializeObject(o.Value)}");
            }
            Console.WriteLine(JsonConvert.SerializeObject(ht));
            return ";";
        }
    }
}
