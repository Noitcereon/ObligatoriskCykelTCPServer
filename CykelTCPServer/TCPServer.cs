using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using ObligatoryClassLibrary;

namespace CykelTCPServer
{
    public class TCPServer
    {
        private static int _incrementableId = 1;
        private static List<Cykel> _bicycles = new List<Cykel>
        {
            new Cykel(BicycleIdGenerator(), "Blå", (decimal)2999.95, 14),
            new Cykel(BicycleIdGenerator(), "Sort", (decimal)6999.95, 32),
            new Cykel(BicycleIdGenerator(), "Sølv", (decimal)1999.95, 12),
            new Cykel(BicycleIdGenerator(), "Grå", (decimal)1195.95, 3),
            new Cykel(BicycleIdGenerator(), "Sølv", (decimal)3999.95, 21),
        };
        public TCPServer()
        {

        }

        public void Start()
        {
            TcpListener server = new TcpListener(IPAddress.Loopback, 4646);
            server.Start();

            Console.WriteLine("Server ready.");
            while (true)
            {
                TcpClient tempSocket = server.AcceptTcpClient();
                Task.Run(() => HandleClient(tempSocket));
            }

        }

        private void HandleClient(TcpClient socket)
        {
            Console.WriteLine("Client connected.");
            // Get the stream from the client
            NetworkStream ns = socket.GetStream();
            // give the writer/reader the stream (slightly faster than using socket.GetStream() on both of them).
            StreamWriter sw = new StreamWriter(ns);
            StreamReader sr = new StreamReader(ns);

            string firstLineInput = sr.ReadLine();
            string secondLineInput;
            string output = "";

            switch (firstLineInput)
            {
                case "HentAlle":
                    foreach (var cykel in _bicycles)
                    {
                        output += $"{JsonSerializer.Serialize(cykel)} {Environment.NewLine}";
                    }
                    break;
                case "Hent":
                    secondLineInput = sr.ReadLine();
                    var id = Convert.ToInt32(secondLineInput);
                    output = JsonSerializer.Serialize(_bicycles.Find(c => c.Id == id));
                    break;
                case "Gem":
                    sw.WriteLine("Indtast en ny cykel i Json format. Eksempel: {\"Id\" : 6, \"Farve\": \"blå\", \"Gear\": 7, \"Pris\" : 100 }");
                    sw.Flush();
                    secondLineInput = sr.ReadLine();
                    int attempts = 1;

                    try
                    {
                        Cykel nyCykel = JsonSerializer.Deserialize<Cykel>(secondLineInput);
                        _bicycles.Add(nyCykel);
                        output = _bicycles.Contains(nyCykel) ? "Ny cykel er gemt." : "Fejlede i at gemme cykelen.";
                    }
                    catch
                    {
                        ReportFailedAttempts(attempts, sw);

                        while (attempts < 3)
                        {
                            try
                            {
                                secondLineInput = sr.ReadLine();
                                Cykel nyCykel = JsonSerializer.Deserialize<Cykel>(secondLineInput);
                                _bicycles.Add(nyCykel);
                                output = _bicycles.Contains(nyCykel)
                                    ? "Ny cykel er gemt."
                                    : "Fejlede i at gemme cykelen.";
                                break;
                            }
                            catch
                            {
                                attempts++;

                                ReportFailedAttempts(attempts, sw);
                        
                                if(attempts >= 3) output = "Afslutter forbindelsen.";
                            }
                        }

                    }

                    break;
                default:
                    output = "Ikke valid input.";
                    break;
            }

            sw.WriteLine(output);
            sw.Flush();

            socket.Close();
        }

        public static int BicycleIdGenerator() => _incrementableId++;

        private void ReportFailedAttempts(int attempts, StreamWriter sw)
        {
            Console.WriteLine($"Attempt {attempts} failed");
            sw.WriteLine($"Forkert format til cyklen, prøv igen ({attempts} af 3 forsøg");
            sw.Flush();
        }

    }
}
