﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fededim.Extensions.Configuration.Protected.ConsoleTest
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


    public class Logging
    {
        public Dictionary<String,String> LogLevel { get; set; }
    }

    public class AppSettings
    {
        // settings defined inside JSON files
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

        // settings defined inside XML file
        public String SecretXmlKey {  get; set; }
        public Dictionary<String, String> TransientFaultHandlingOptions { get; set; }
        public Logging Logging { get; set; }


        // settings defined in InMemoryCollection
        public String SecretKey { get; set; }


        // settings defined in EnvironmentVariables
        public String SecretEnvironmentPassword { get; set; }


    }
}
