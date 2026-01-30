using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace PRMIS_projekat
{
    internal class Centralni_Server
    {
        static List<Merenje> svaMerenja = new List<Merenje>();
        static List<Alarm> aktivniAlarmi = new List<Alarm>();
        static List<Stanica> inicijalizovaneStanice = new List<Stanica>();
        static void Main(string[] args)
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 50001);
            List<Socket> klijenti = new List<Socket>();

            serverSocket.Bind(serverEP);
            Console.WriteLine("Unesite broj meteoroloskih stanica:");
            int maxKlijenata = int.Parse(Console.ReadLine());

            for (int j = 0; j < maxKlijenata; j++)
            {
                Stanica stanica = new Stanica();
                Console.WriteLine($"Podaci za {j + 1}. stanicu - Naziv:");
                stanica.Naziv = Console.ReadLine();
                Console.WriteLine("Broj mernih uredjaja:");
                stanica.Br_mernih_uredjaja = int.Parse(Console.ReadLine());
                Console.WriteLine("Unesite geografsku sirinu:");
                stanica.Sirina = double.Parse(Console.ReadLine());
                Console.WriteLine("Unesite geografsku duzinu:");
                stanica.Duzina = double.Parse(Console.ReadLine());

                inicijalizovaneStanice.Add(stanica);
            }

            serverSocket.Listen(maxKlijenata);
            for (int i = 0; i < maxKlijenata; i++)
            {
                Socket client = serverSocket.Accept();
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, inicijalizovaneStanice[i]);
                    bf.Serialize(ms, $"{50000 + (i + 1) * 100}");
                    client.Send(ms.ToArray());
                }
                client.Blocking = false;
                klijenti.Add(client);
            }

            byte[] buffer = new byte[65535]; 
            while (true)
            {
                List<Socket> checkRead = new List<Socket>(klijenti);
                if (checkRead.Count > 0)
                {
                    Socket.Select(checkRead, null, null, 1000);
                    foreach (Socket s in checkRead)
                    {
                        try
                        {
                            int brBajta = s.Receive(buffer);
                            if (brBajta <= 0) continue;

                            using (MemoryStream ms = new MemoryStream(buffer, 0, brBajta))
                            {
                                BinaryFormatter bf = new BinaryFormatter();
                                object obj = bf.Deserialize(ms);

                                if (obj is List<Merenje> nova)
                                {
                                    svaMerenja.AddRange(nova);
                                }
                                else if (obj is Alarm a)
                                {
                                    aktivniAlarmi.Insert(0, a); 
                                    if (aktivniAlarmi.Count > 5) aktivniAlarmi.RemoveAt(5);
                                }
                                IscrtajTabelu(inicijalizovaneStanice, svaMerenja, aktivniAlarmi);
                            }
                        }
                        catch { }
                    }
                }
                if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Escape) break;
            }
        }

        static void IscrtajTabelu(List<Stanica> stanice, List<Merenje> merenja, List<Alarm> alarmi)
        {
            Console.Clear();
            Console.WriteLine("================ MONITORING CENTRALNI SERVER ================");
            Console.WriteLine($"{"Stanica",-15} | {"Temp",-8} | {"Vlaga",-8} | {"Vetar",-10} | {"Status",-10}");
            Console.WriteLine("-------------------------------------------------------------");

            foreach (var st in stanice)
            {
                var mSt = merenja.Where(m => m.NazivStanice == st.Naziv).ToList();

                // Poslednje vrednosti
                var poslednjeT = mSt.LastOrDefault(x => x.Tip == Tip_merenja.Temperatura);
                var poslednjeW = mSt.LastOrDefault(x => x.Tip == Tip_merenja.Vetar);

                var tVal = poslednjeT?.Trenutna_vrednost.ToString() ?? "--";
                var vVal = mSt.LastOrDefault(x => x.Tip == Tip_merenja.Vlaznost)?.Trenutna_vrednost.ToString() ?? "--";
                var wVal = poslednjeW?.Trenutna_vrednost.ToString() ?? "--";

                string status = "OK";

                if ((poslednjeT != null && poslednjeT.Trenutna_vrednost > 40) ||
                    (poslednjeW != null && poslednjeW.Trenutna_vrednost > 100))
                {
                    status = "!!! ALARM !!!";
                }

                Console.WriteLine($"{st.Naziv,-15} | {tVal,-8} | {vVal,-8} | {wVal,-10} | {status,-10}");
            }

            if (alarmi.Any())
            {
                Console.WriteLine("\nISTORIJA ALARMA (Poslednjih 5):");
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var a in alarmi)
                    Console.WriteLine($"[!] {a.Uzrok} -> {a.Trenutna_vrednost}");
                Console.ResetColor();
            }
        }
    }
}
