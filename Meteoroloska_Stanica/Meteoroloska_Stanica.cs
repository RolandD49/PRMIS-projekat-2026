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

namespace Meteoroloska_Stanica
{
    internal class Meteoroloska_Stanica
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Meteoroloska Stanica pocinje sa radom.");
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Stanica ova_st = new Stanica();
            int br_soketa = 0; //prvi soket za UDP komunikaciju sa mernim uredjajima, dobija se od servera
            List<Socket> udp_sockets = new List<Socket>(); //lista gde ce biti smesteni UDP soketi za merne uredjaje
            List<IPEndPoint> udp_endpoints = new List<IPEndPoint>();
            List<Socket> checkRead = new List<Socket>();
            List<Socket> checkError = new List<Socket>();
            List<Merenje> merenja = new List<Merenje>();

            IPEndPoint serverEP = new IPEndPoint(IPAddress.Loopback, 50001);
            byte[] buffer = new byte[1024];

            Console.WriteLine("Klijent je spreman za povezivanje sa serverom, kliknite enter");
            Console.ReadKey();
            clientSocket.Connect(serverEP);
            Console.WriteLine("Klijent je uspesno povezan sa serverom!");

            //TCP komunikacija sa Centralnim serverom: inicijalizacija Meteoroloske stanice
            try
            {
                // Primamo podatke od servera
                int brBajta = clientSocket.Receive(buffer);

                if (brBajta == 0)
                {
                    Console.WriteLine("Centralni server je zatvorio vezu.");
                    return;
                }

                // Ključno: Koristimo jedan MemoryStream za oba objekta (Stanica + Port)
                // jer ih je server poslao jedan za drugim
                using (MemoryStream ms = new MemoryStream(buffer, 0, brBajta))
                {
                    BinaryFormatter bf = new BinaryFormatter();

                    // 1. Deserijalizujemo objekat Stanica
                    ova_st = (Stanica)bf.Deserialize(ms);
                    Console.WriteLine($"\n--- Inicijalizacija ---");
                    Console.WriteLine($"Naziv stanice: {ova_st.Naziv}");
                    Console.WriteLine($"Koordinate: {ova_st.Sirina}, {ova_st.Duzina}");
                    Console.WriteLine($"Broj mernih uredjaja: {ova_st.Br_mernih_uredjaja}");

                    // 2. Deserijalizujemo string za port (koji je server poslao bf.Serialize(ms, br_porta))
                    // Napomena: U prethodnom koraku smo na serveru dodali bf.Serialize za port
                    string portRaw = (string)bf.Deserialize(ms);
                    br_soketa = int.Parse(portRaw);

                    Console.WriteLine($"Dostupni portovi za UDP: {br_soketa} do {br_soketa + ova_st.Br_mernih_uredjaja - 1}");
                    Console.WriteLine("-----------------------\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Doslo je do greske tokom inicijalizacije:\n{ex.Message}");
            }
            //UDP komunikacija

            //postavljanje UDP soketa za merne uredjaje
            for (int i = 0; i < (ova_st.Br_mernih_uredjaja); i++)
            {
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint udp_ep = new IPEndPoint(IPAddress.Any, br_soketa + i);
                s.Bind(udp_ep);
                udp_sockets.Add(s);
                udp_endpoints.Add(udp_ep);
            }

            EndPoint posiljaocEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] prijemniBafer = new byte[1024];

            try
            {
                Console.WriteLine("Meteoroloska stanica je pokrenut! Za zavrsetak rada pritisnite Escape");
                while (true)
                {
                    foreach (Socket s in udp_sockets)
                    {
                        checkRead.Add(s);
                        checkError.Add(s);
                    }
                    Socket.Select(checkRead, null, checkError, 1000);

                    if (checkRead.Count > 0)
                    {
                        foreach (Socket s in checkRead)
                        {
                            try
                            {
                                int bytesRec = s.ReceiveFrom(prijemniBafer, ref posiljaocEP);
                                using (MemoryStream ms = new MemoryStream(prijemniBafer, 0, bytesRec))
                                {
                                    BinaryFormatter bf = new BinaryFormatter();
                                    object obj = bf.Deserialize(ms);

                                    if (obj is Merenje mer)
                                    {
                                        mer.NazivStanice = ova_st.Naziv; 
                                        merenja.Add(mer);
                                        Console.WriteLine($"Merenje primljeno: {mer.Tip} {mer.Trenutna_vrednost}");
                                    }
                                    else if (obj is Alarm al)
                                    {
                                        al.Uzrok = $"[{ova_st.Naziv}] {al.Uzrok}"; 
                                        Console.WriteLine("DETEKTOVAN ALARM! Prosledjujem serveru...");
                                        using (MemoryStream msA = new MemoryStream())
                                        {
                                            bf.Serialize(msA, al);
                                            clientSocket.Send(msA.ToArray());
                                        }
                                    }
                                }
                            }
                            catch (Exception ex) { Console.WriteLine(ex.Message); }
                        }

                        // Slanje agregiranih merenja
                        if (merenja.Count > 0)
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                new BinaryFormatter().Serialize(ms, merenja);
                                clientSocket.Send(ms.ToArray());
                                merenja.Clear(); // Obavezno prazni listu!
                            }
                        }
                    }
                    if (checkError.Count > 0)
                    {
                        Console.WriteLine($"Desilo se {checkError.Count} gresaka\n");

                        foreach (Socket s in checkError)
                        {
                            Console.WriteLine($"Greska na socketu: {s.LocalEndPoint}");

                            Console.WriteLine("Zatvaram socket zbog greske...");
                            s.Close();

                        }
                    }


                    //merenja.Clear();

                    checkError.Clear();
                    checkRead.Clear();
                }

            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Doslo je do greske: {ex}");
            }


            Console.WriteLine("Stanica zavrsava sa radom");
            foreach (Socket s in udp_sockets)
            {
                s.Close();
            }
            Console.ReadKey();

        }
    }
}
