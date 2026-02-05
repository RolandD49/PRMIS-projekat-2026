using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Meteoroloska_Stanica
{
    internal class Meteoroloska_Stanica
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== METEOROLOSKA STANICA (KLIJENT) ===");

            Socket merniSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket alarmniSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Stanica ova_st = null;
            int br_soketa_start = 0;
            List<Socket> udp_sockets = new List<Socket>();
            List<Merenje> merenjaZaSlanje = new List<Merenje>();

            try
            {
                // RETRY LOGIKA: Pokusava povezivanje dok god server ne odgovori
                bool povezan = false;
                while (!povezan)
                {
                    try
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Pokusavam povezivanje na Server (50001 i 50002)...");

                        // Pokusaj povezivanja na oba porta
                        merniSocket.Connect(new IPEndPoint(IPAddress.Loopback, 50001));
                        alarmniSocket.Connect(new IPEndPoint(IPAddress.Loopback, 50002));

                        povezan = true;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Uspesno povezana oba TCP kanala!");
                        Console.ResetColor();
                    }
                    catch (SocketException)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Server nije spreman (verovatno jos unosite podatke). Ponovni pokusaj za 2 sekunde...");
                        Console.ResetColor();
                        Thread.Sleep(2000); // Pauza pre sledeceg pokusaja
                    }
                }

                // PRIMANJE INICIJALIZACIJE (Nakon sto se uspesno poveze)
                byte[] initBuffer = new byte[4096];
                int bytesRead = merniSocket.Receive(initBuffer);

                using (MemoryStream ms = new MemoryStream(initBuffer, 0, bytesRead))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    ova_st = (Stanica)bf.Deserialize(ms);
                    string portRaw = (string)bf.Deserialize(ms);
                    br_soketa_start = int.Parse(portRaw);
                }

                Console.WriteLine($"\n--- STANICA INICIJALIZOVANA ---");
                Console.WriteLine($"Naziv: {ova_st.Naziv}");
                Console.WriteLine($"UDP portovi za uredjaje: {br_soketa_start} do {br_soketa_start + ova_st.Br_mernih_uredjaja - 1}");

                // OTVARANJE UDP SOKETA ZA MERNE UREDJAJE
                for (int i = 0; i < ova_st.Br_mernih_uredjaja; i++)
                {
                    Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    s.Bind(new IPEndPoint(IPAddress.Any, br_soketa_start + i));
                    udp_sockets.Add(s);
                }

                Console.WriteLine("\nSlusam mjerne uredjaje na UDP portovima...");
                EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] udpBuffer = new byte[2048];

                while (true)
                {
                    List<Socket> checkRead = new List<Socket>(udp_sockets);
                    if (checkRead.Count > 0)
                    {
                        // Socket.Select prati da li je nesto stiglo na UDP portove
                        Socket.Select(checkRead, null, null, 100000);

                        foreach (Socket s in checkRead)
                        {
                            int bytesRec = s.ReceiveFrom(udpBuffer, ref remoteEP);
                            using (MemoryStream ms = new MemoryStream(udpBuffer, 0, bytesRec))
                            {
                                BinaryFormatter bf = new BinaryFormatter();
                                object obj = bf.Deserialize(ms);

                                if (obj is Merenje m)
                                {
                                    m.NazivStanice = ova_st.Naziv;
                                    merenjaZaSlanje.Add(m);
                                    Console.WriteLine($"[UDP] Stiglo merenje: {m.Tip} = {m.Trenutna_vrednost}");
                                }
                                else if (obj is Alarm al)
                                {
                                    al.Uzrok = $"[{ova_st.Naziv}] {al.Uzrok}";
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine($"[!!!] ALARM DETEKTOVAN! Saljem na poseban TCP kanal (Port 50002)...");
                                    Console.ResetColor();

                                    using (MemoryStream msA = new MemoryStream())
                                    {
                                        bf.Serialize(msA, al);
                                        alarmniSocket.Send(msA.ToArray());
                                    }
                                }
                            }
                        }
                    }

                    // PERIODICNO SLANJE MERENJA NA SERVER (Port 50001)
                    if (merenjaZaSlanje.Count > 0)
                    {
                        try
                        {
                            using (MemoryStream msM = new MemoryStream())
                            {
                                new BinaryFormatter().Serialize(msM, merenjaZaSlanje);
                                merniSocket.Send(msM.ToArray());
                                merenjaZaSlanje.Clear();
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Greska pri slanju merenja na server.");
                        }
                    }

                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape) break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nKRITICNA GRESKA U RADU STANICE:");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                Console.WriteLine("\nPritisnite bilo koji taster za izlaz...");
                Console.ReadKey();
            }
            finally
            {
                merniSocket.Close();
                alarmniSocket.Close();
                foreach (var s in udp_sockets) s.Close();
            }
        }
    }
}
