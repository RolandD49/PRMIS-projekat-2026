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

namespace Merni_uredjaj
{
    internal class Merni_uredjaj
    {
        static void Main(string[] args)
        {
            //Tip_merenja tm = Tip_merenja.Vetar;
            // tip mernog uredjaja
            /*while (true) 
            {
                Console.WriteLine("Odaberite tip mernog uredjaja: 1-temperatura, 2-vlaznost, 3-vetar, 4-padavine");
                int br = int.Parse(Console.ReadKey());

                if (br == 1)
                {
                    Console.WriteLine("Izabran merni uredjaj temperature.");
                    tm = Tip_merenja.Temperatura;
                    break;
                } 
                else if (br == 2) 
                {
                    Console.WriteLine("Izabran merni uredjaj vlaznosti.");
                    tm = Tip_merenja.Vlaznost;
                    break;
                }
                else if (br == 3)
                {
                    Console.WriteLine("Izabran merni uredjaj vetra.");
                    tm = Tip_merenja.Vetar;
                    break;
                }
                else if (br == 2)
                {
                    Console.WriteLine("Izabran merni uredjaj padavine.");
                    tm = Tip_merenja.Padavine;
                    break;
                }
                else
                {
                    Console.WriteLine("Unesen broj treba da bude izmedju 1 i 4.");
                }
            }*/
            Tip_merenja tm = Tip_merenja.Vetar;
            Console.WriteLine("Odaberite tip mernog uredjaja: 1-temperatura, 2-vlaznost, 3-vetar, 4-padavine");
            int br = int.Parse(Console.ReadLine());

            if (br == 1)
            {
                Console.WriteLine("Izabran merni uredjaj temperature.");
                tm = Tip_merenja.Temperatura;

            }
            else if (br == 2)
            {
                Console.WriteLine("Izabran merni uredjaj vlaznosti.");
                tm = Tip_merenja.Vlaznost;

            }
            else if (br == 3)
            {
                Console.WriteLine("Izabran merni uredjaj vetra.");
                tm = Tip_merenja.Vetar;

            }
            else if (br == 4)
            {
                Console.WriteLine("Izabran merni uredjaj padavine.");
                tm = Tip_merenja.Padavine;

            }
            else
            {
                Console.WriteLine("Unesen broj treba da bude izmedju 1 i 4.");
            }

            //Console.WriteLine("Merni uredjaj pocinje sa radom.");

            Console.WriteLine("Unesite port sa kog zelite da komunicirate.");
            int port = int.Parse(Console.ReadLine());
            IPEndPoint destinationEP = new IPEndPoint(IPAddress.Loopback, port);
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            try
            {
                

                //Tip_merenja tm = Tip_merenja.Vetar;
                Console.WriteLine("Merni uredjaj je pokrenut! Za zavrsetak rada pritisnite Escape");
                while (true)
                {
                    //test poruka
                    Console.WriteLine("Za slanje test merenja pritisnite enter");
                    Console.ReadKey();
                    Merenje merenje = new Merenje();
                    merenje.Tip = tm;
                    merenje.Trenutna_vrednost = 12.4;
                    merenje.Jedinica_mere = "test";

                    byte[] buffer;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        bf.Serialize(ms, merenje);
                        buffer = ms.ToArray();
                    }

                    int brBajta = clientSocket.SendTo(buffer, 0, buffer.Length, SocketFlags.None, destinationEP);

                    Console.WriteLine($"Uspesno poslato {brBajta} ka {destinationEP}");

                    if (Console.KeyAvailable)
                    {
                        if (Console.ReadKey().Key == ConsoleKey.Escape)
                        {
                            break;
                        }
                    }
                }

            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Doslo je do greske tokom slanja poruke: \n{ex}");
            }

            Console.WriteLine("Merni uredjaj zavrsava sa radom");
            clientSocket.Close();
            Console.ReadKey();
        }
    }
}
