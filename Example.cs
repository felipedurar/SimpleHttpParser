using Durar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestParserHehe
{
    /// <summary>
    /// Sample SimpleHttpParser Program
    /// Coded By: Felipe Durar
    /// https://github.com/felipedurar/SimpleHttpParser
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            string fileToOpen = "sampleHttpTraffic.bin";
            if (args.Length > 0)
                fileToOpen = args[0];

            byte[] fileContent = File.ReadAllBytes(fileToOpen);

            SimpleHttpParser simpleHttpParser = new SimpleHttpParser();
            // Just push any data here coming from a TCP Stream
            simpleHttpParser.PushData(fileContent);

            // Any parsed message is available at ParsedMessages property
            while (simpleHttpParser.ParsedMessages.Count > 0)
            {
                HttpMessage httpMessage = simpleHttpParser.ParsedMessages.Dequeue();

                if (httpMessage.MessageType == HttpMessageType.Request)
                {
                    Console.WriteLine(httpMessage.MessageType.ToString() + " - " + httpMessage.Method.ToString() + " - " + httpMessage.Route);
                    if (httpMessage.Content != null)
                    {
                        if (httpMessage.Content.Length > 0)
                            Console.WriteLine("Content: " + httpMessage.GetContentAsString());
                    }
                }
                else if (httpMessage.MessageType == HttpMessageType.Response)
                {
                    Console.WriteLine(httpMessage.MessageType.ToString() + " - " + httpMessage.StatusText + " - " + httpMessage.StatusCode);
                    if (httpMessage.Content != null)
                    {
                        if (httpMessage.Content.Length > 0)
                            Console.WriteLine("Content: " + httpMessage.GetContentAsString());
                    }
                }
                Console.WriteLine();
            }

            Console.ReadLine();
        }
    }
}
