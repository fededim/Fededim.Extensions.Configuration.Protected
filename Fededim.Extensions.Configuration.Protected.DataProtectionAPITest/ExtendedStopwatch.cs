using System;
using System.Diagnostics;
using Xunit.Abstractions;

namespace Fededim.Extensions.Configuration.Protected.DataProtectionAPITest
{
    public class ExtendedStopwatch : Stopwatch
    {
        private static Func<String> EmptyDebugInfo = () => String.Empty;

        protected Func<String> DebugInfo { get; set; }
        protected ITestOutputHelper TestOutputHelper { get; set; }

        public ExtendedStopwatch() : base()
        {
        }


        public ExtendedStopwatch(bool start=false,Func<String> debugInfo = null, ITestOutputHelper testOutputHelper = null) : this()
        {
            DebugInfo = debugInfo;
            TestOutputHelper = testOutputHelper;

            if (start)
                Start();
        }


        public void Step(String stepName)
        {

            Stop();

            var message = $"{DateTime.Now}: {stepName} duration {(ElapsedMilliseconds<10000?$"{ElapsedMilliseconds}ms":Elapsed.ToString())} {((DebugInfo != null) ? $"({DebugInfo()})" : String.Empty)}";

            if (TestOutputHelper != null)
                TestOutputHelper.WriteLine(message);
            else
                Debug.WriteLine(message);

            Restart();
        }




        public void Lap(String stepName)
        {

            Stop();

            var message = $"{DateTime.Now}: {stepName} duration {Elapsed} ({((DebugInfo != null) ? DebugInfo() : String.Empty)})";

            if (TestOutputHelper != null)
                TestOutputHelper.WriteLine(message);
            else
                Debug.WriteLine(message);

            Start();
        }



    }
}
