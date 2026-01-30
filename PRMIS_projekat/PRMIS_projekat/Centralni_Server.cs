using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace PRMIS_projekat
{
    internal class Centralni_Server
    {
        static void Main(string[] args)
        {

            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 50001);
            List<Socket> klijenti = new List<Socket>();
            List<Merenje> merenja = new List<Merenje>();
            List<Stanica> stanice = new List<Stanica>();

            byte[] buffer = new byte[1024];

            serverSocket.Bind(serverEP);

            //unos meteoroloskih stanica
            Console.WriteLine("Unesite broj meteoroloskih stanica koje zelite da inicijalizujete.");
            int maxKlijenata = int.Parse(Console.ReadLine());

            for (int j=0; j<maxKlijenata; j++)
            {
                Stanica stanica = new Stanica();
                Console.WriteLine($"Podaci za {j+1}. stanicu");
                Console.WriteLine("Unesite naziv  stanice.");
                stanica.Naziv = Console.ReadLine();

                Console.WriteLine("Unesite broj mernih uredjaja stanice.");
                stanica.Br_mernih_uredjaja = int.Parse(Console.ReadLine());

                Console.WriteLine("Unesite koordinate");
                Console.WriteLine("sirina:");
                stanica.Sirina = double.Parse(Console.ReadLine());
                Console.WriteLine("duzina:");
                stanica.Duzina = double.Parse(Console.ReadLine());
                stanice.Add(stanica);
            }

            serverSocket.Listen(maxKlijenata);

            for (int i = 0; i < maxKlijenata; i++)
            {
                Socket client = serverSocket.Accept();

                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, stanice[i]);
                    buffer = ms.ToArray();
                }
                int brBajta = client.Send(buffer);
                if (brBajta > 0)
                {
                    Console.WriteLine($"Podaci uspesno poslati ka {i+1}. stanici.");
                }

                //slanje broj prvog porta za UDP komunikaciju sa mernim uredjajima
                string br_porta = $"{50000 + (i+1)*100}";
                brBajta = client.Send(Encoding.UTF8.GetBytes(br_porta));

                client.Blocking = false;
                klijenti.Add(client);
                Console.WriteLine($"Meteoroloska stanica se povezao sa {client.RemoteEndPoint}");
            }

            //serverSocket.Blocking = false;




            Console.WriteLine($"Centralni server je stavljen u stanje osluskivanja i ocekuje komunikaciju na {serverEP}");
            try
            {
                Console.WriteLine("Centralni server je pokrenut! Za zavrsetak rada pritisnite Escape");
                while (true)
                {
                    List<Socket> checkRead = new List<Socket>();
                    List<Socket> checkError = new List<Socket>();

                    foreach (Socket s in klijenti)
                    {
                        checkRead.Add(s);
                        checkError.Add(s);
                    }


                    Socket.Select(checkRead, null, checkError, 1000);


                    if (checkRead.Count > 0)
                    {
                        Console.WriteLine($"Broj dogadjaja je: {checkRead.Count}");
                        foreach (Socket s in checkRead)
                        {
                            
                            int brBajta = s.Receive(buffer);
                            if (brBajta == 0)
                            {
                                Console.WriteLine("Stanica je prekinuo komunikaciju");
                                s.Close();
                                klijenti.Remove(s);

                                continue;
                            }
                            else
                            {
                                using (MemoryStream ms = new MemoryStream(buffer, 0, brBajta))
                                {
                                    BinaryFormatter bf = new BinaryFormatter();
                                    //List<Merenje> recv_merenja = bf.Deserialize(ms) as List<Merenje>;
                                    //List<Merenje> recv_merenja = (List<Merenje>)bf.Deserialize(ms);
                                    Merenje[] recv_merenja = (Merenje[])bf.Deserialize(ms);
                                    foreach (Merenje m2 in recv_merenja)
                                    {
                                        merenja.Add(m2);
                                    }

                                }
                            }
                            

                            if (Console.KeyAvailable)
                            {
                                if (Console.ReadKey().Key == ConsoleKey.Escape)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    checkRead.Clear();
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Doslo je do greske {ex}");
            }


            foreach (Socket s in klijenti)
            {
                //s.Send(Encoding.UTF8.GetBytes("Server je zavrsio sa radom"));
                s.Close();
            }

            Console.WriteLine("Server zavrsava sa radom");
            Console.ReadKey();
            serverSocket.Close();

        }
    }
}
