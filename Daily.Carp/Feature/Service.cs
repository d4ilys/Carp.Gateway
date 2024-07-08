using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Daily.Carp.Feature
{
    public class Service
    {

        public string? Host { get; set; }

        public int Port { get; set; }

        public string Protocol { get; set; } = "http";

        public override string ToString()
        {
            var result = "";
            if (!Host.ToLower().StartsWith(Protocol))
                result += $"{Protocol}://{Host}";
            else
                result += $"{Host}";
            if (Port != 0)
                result += $":{Port}";
            return result;
        }
    }
}