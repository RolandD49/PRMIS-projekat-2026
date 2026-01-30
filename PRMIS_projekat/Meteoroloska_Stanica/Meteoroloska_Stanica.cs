using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Common;

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
                /*string poruka = "Poruka Meteoroloske Stanice: uspesna konekcija sa centralnim serverom.";
                int brBajta = clientSocket.Send(Encoding.UTF8.GetBytes(poruka));*/
                int brBajta = 0;

                brBajta = clientSocket.Receive(buffer);

                if (brBajta == 0)
                {
                    Console.WriteLine("Centralni server je zavrsio sa radom");

                }

                using (MemoryStream ms = new MemoryStream(buffer, 0, brBajta))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    ova_st = bf.Deserialize(ms) as Stanica;
                    Console.WriteLine($"Naziv stanice: {ova_st.Naziv} \n Koordinate: {ova_st.Sirina}, {ova_st.Duzina}\n Broj mernih uredjaja {ova_st.Br_mernih_uredjaja}");

                }

                brBajta = 0;
                brBajta = clientSocket.Receive(buffer);
                br_soketa = int.Parse(Encoding.UTF8.GetString(buffer, 0, brBajta));
                Console.WriteLine($"Dostupni portovi za komunikaciju sa mernim uredjajima su od {br_soketa} do {br_soketa + ova_st.Br_mernih_uredjaja - 1}.");
                //Console.WriteLine(odgovor);


            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Doslo je do greske tokom slanja:\n{ex}");
                
            }
            //UDP komunikacija

            //postavljanje UDP soketa za merne uredjaje
            for(int i=0; i<(ova_st.Br_mernih_uredjaja); i++)
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
                        //Console.WriteLine($"Desilo se {checkRead.Count} dogadjaja\n");
                        foreach (Socket s in checkRead)
                        {

                            try
                            {
                                int bytesRec = s.ReceiveFrom(prijemniBafer, ref posiljaocEP);
                                using (MemoryStream ms = new MemoryStream(prijemniBafer, 0, bytesRec))
                                {
                                    BinaryFormatter bf = new BinaryFormatter();
                                    Merenje mer = bf.Deserialize(ms) as Merenje;
                                    Console.WriteLine($"Dobijeno merenje: {mer.Trenutna_vrednost} {mer.Jedinica_mere}, od Socket posiljaoca: {posiljaocEP} Socket primaoca: {s.LocalEndPoint}");
                                    merenja.Add(mer);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Greska: {ex.Message}");
                            }

                            if (Console.KeyAvailable)
                            {
                                if (Console.ReadKey().Key == ConsoleKey.Escape)
                                {
                                    break;
                                }
                            }
                        }
                        //slanje agregiranih podataka prema Centralnom serveru
                        byte[] buffer1 = new byte[1024];
                        using (MemoryStream ms = new MemoryStream())
                        {
                            BinaryFormatter bf = new BinaryFormatter();
                            bf.Serialize(ms, merenja);
                            buffer1 = ms.ToArray();
                        }
                        if (merenja.Count() > 0)
                        {
                            clientSocket.Send(buffer1);
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
