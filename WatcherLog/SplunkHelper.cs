using System;
using System.Collections.Generic;
using System.Net;
using Splunk.Client;
using System.IO;
using System.Text;

namespace WatcherLog
{
    public class SplunkHelper
    {
        public SplunkHelper(string host, string userName, string password, string index)
        {
            Host = host;
            UserName = userName;
            Password = password;
            Index = index;
        }

        public static string Host { get; set; }
        public static string UserName { get; set; }
        public static string Password { get; set; }
        public static string Index { get; set; }

        private Service ConnectServiceHost()
        {
            try
            {
                if (Host.Split(':').Length != 2)
                    throw new Exception("Splunk: HostName invalid");

                var host = Host.Split(':')[0];
                var port = 0;
                if (!int.TryParse(Host.Split(':')[1], out port))
                    throw new Exception("Splunk: Port's hostname invalid");

                var context = new Context(Scheme.Https, host, port);
                var service = new Service(context);
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

                service.LogOnAsync(UserName, Password).Wait();

                return service;
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                throw;
            }
        }

        public bool SendMessage(string message)
        {
            try
            {
                var service = ConnectServiceHost();
                service.Transmitter.SendAsync(message, Index).Wait();
                Console.Out.WriteLine($"Sending message: {message}");
                return true;
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.InnerException?.Message);
                throw;
            }
        }

        public bool SendMessages(List<string> messages)
        {
            try
            {
                var service = ConnectServiceHost();
                using (var stream = new MemoryStream())
                {
                    using (var writer = new StreamWriter(stream, Encoding.UTF8, 4096, leaveOpen: true))
                        messages.ForEach(m => writer.WriteLine(m));
                    stream.Seek(0, SeekOrigin.Begin);
                    service.Transmitter.SendAsync(stream, Index).Wait();
                    Console.Out.WriteLine($"Sending stream with {messages.Count} lines");
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.InnerException?.Message);
                throw;
            }
        }
    }
}
