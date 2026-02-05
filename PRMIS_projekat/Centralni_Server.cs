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
            try
            {
                // Inicijalizacija dva odvojena TCP soketa za razlicite namene
                Socket measurementSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Socket alarmSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Vezivanje na portove 50001 (Podaci) i 50002 (Alarmi)
                measurementSocket.Bind(new IPEndPoint(IPAddress.Any, 50001));
                alarmSocket.Bind(new IPEndPoint(IPAddress.Any, 50002));

                Console.WriteLine("================ SERVER KONFIGURACIJA ================");
                Console.WriteLine("Unesite broj meteoroloskih stanica koje ocekujete:");

                string input = Console.ReadLine();
                if (!int.TryParse(input, out int maxKlijenata)) maxKlijenata = 1;

                // Konfigurisanje stanica pre starta
                for (int j = 0; j < maxKlijenata; j++)
                {
                    Stanica stanica = new Stanica();
                    Console.WriteLine($"\nPodaci za stanicu br. {j + 1}:");
                    Console.Write("Naziv: "); stanica.Naziv = Console.ReadLine();
                    Console.Write("Broj mernih uredjaja: "); stanica.Br_mernih_uredjaja = int.Parse(Console.ReadLine());
                    Console.Write("Geografska sirina: "); stanica.Sirina = double.Parse(Console.ReadLine());
                    Console.Write("Geografska duzina: "); stanica.Duzina = double.Parse(Console.ReadLine());
                    inicijalizovaneStanice.Add(stanica);
                }

                measurementSocket.Listen(maxKlijenata);
                alarmSocket.Listen(maxKlijenata);

                List<Socket> klijentiMerenja = new List<Socket>();
                List<Socket> klijentiAlarmi = new List<Socket>();

                Console.WriteLine("\nCekam da se stanice povezu na OBA kanala (50001 i 50002)...");

                for (int i = 0; i < maxKlijenata; i++)
                {
                    // Svaka stanica mora da uspostavi dve konekcije
                    Socket mClient = measurementSocket.Accept();
                    Socket aClient = alarmSocket.Accept();

                    // Slanje inicijalizacionih podataka stanici preko mernog kanala
                    using (MemoryStream ms = new MemoryStream())
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        bf.Serialize(ms, inicijalizovaneStanice[i]);
                        // Generisemo jedinstveni pocetni UDP port za merna uredjaje te stanice
                        bf.Serialize(ms, $"{50000 + (i + 1) * 100}");
                        mClient.Send(ms.ToArray());
                    }

                    mClient.Blocking = false;
                    aClient.Blocking = false;
                    klijentiMerenja.Add(mClient);
                    klijentiAlarmi.Add(aClient);
                    Console.WriteLine($"Stanica '{inicijalizovaneStanice[i].Naziv}' povezana.");
                }

                byte[] buffer = new byte[65535];
                Console.WriteLine("Monitoring pokrenut. Pritisnite ESC za kraj.");

                while (true)
                {
                    List<Socket> checkRead = new List<Socket>();
                    checkRead.AddRange(klijentiMerenja);
                    checkRead.AddRange(klijentiAlarmi);

                    if (checkRead.Count > 0)
                    {
                        // Nadgledanje svih aktivnih soketa istovremeno
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

                                    // LOGIKA RAZDVAJANJA: Proveravamo na kom soketu je stigao paket
                                    if (obj is List<Merenje> nova && klijentiMerenja.Contains(s))
                                    {
                                        svaMerenja.AddRange(nova);
                                    }
                                    else if (obj is Alarm a && klijentiAlarmi.Contains(s))
                                    {
                                        aktivniAlarmi.Insert(0, a);
                                        if (aktivniAlarmi.Count > 5) aktivniAlarmi.RemoveAt(5);
                                    }
                                    IscrtajTabelu(inicijalizovaneStanice, svaMerenja, aktivniAlarmi);
                                }
                            }
                            catch { /* Handling disconnects */ }
                        }
                    }
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape) break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nDOŠLO JE DO GREŠKE:");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                Console.WriteLine("\nPritisnite bilo koji taster za izlaz...");
                Console.ReadKey();
            }
        }

        static void IscrtajTabelu(List<Stanica> stanice, List<Merenje> merenja, List<Alarm> alarmi)
        {
            Console.Clear();
            Console.WriteLine("================ MONITORING CENTRALNI SERVER (MULTI-SOCKET) ================");
            Console.WriteLine($"{"Stanica",-15} | {"Temp",-8} | {"Vlaga",-8} | {"Vetar",-10} | {"Status",-10}");
            Console.WriteLine("----------------------------------------------------------------------------");

            foreach (var st in stanice)
            {
                var mSt = merenja.Where(m => m.NazivStanice == st.Naziv).ToList();
                var poslednjeT = mSt.LastOrDefault(x => x.Tip == Tip_merenja.Temperatura);
                var poslednjeW = mSt.LastOrDefault(x => x.Tip == Tip_merenja.Vetar);
                var poslednjeV = mSt.LastOrDefault(x => x.Tip == Tip_merenja.Vlaznost);

                string status = "OK";
                if ((poslednjeT?.Trenutna_vrednost > 40) || (poslednjeW?.Trenutna_vrednost > 100))
                    status = "!!! ALARM !!!";

                Console.WriteLine($"{st.Naziv,-15} | {poslednjeT?.Trenutna_vrednost.ToString() ?? "--",-8} | {poslednjeV?.Trenutna_vrednost.ToString() ?? "--",-8} | {poslednjeW?.Trenutna_vrednost.ToString() ?? "--",-10} | {status,-10}");
            }

            if (alarmi.Any())
            {
                Console.WriteLine("\nPOSLEDNJI ALARMI (Stigli preko posebnog TCP kanala 50002):");
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var a in alarmi) Console.WriteLine($"[!] {a.Uzrok} -> {a.Trenutna_vrednost}");
                Console.ResetColor();
            }
        }
    }
}
