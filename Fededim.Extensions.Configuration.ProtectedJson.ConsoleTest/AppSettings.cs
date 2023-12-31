﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fededim.Extensions.Configuration.ProtectedJson.ConsoleTest
{
    public class NullableSettings
    {
        public Int32? Int { get; set; }
        public bool? Bool { get; set; }
        public Double? Double { get; set; }
        public DateTime? DateTime { get; set; }
        public Double[] DoubleArray { get; set; }
        public NullableSettings()
        {
            DoubleArray = new Double[0];
        }

    }



    public class AppSettings
    {
        public Int32 Int { get; set; }
        public bool Bool { get; set; }
        public Double Double { get; set; }
        public DateTime DateTime { get; set; }
        public Int32[] IntArray { get; set; }

        public NullableSettings Nullable { get; set; }

        public Dictionary<String, String> ConnectionStrings { get; set; }

        public AppSettings()
        {
            IntArray = new Int32[0];
            Nullable = new NullableSettings();
            ConnectionStrings = new Dictionary<String, String>();
        }
    }
}
