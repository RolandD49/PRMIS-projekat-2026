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
                Console.WriteLine("Merni uredjaj je pokrenut! Za zavrsetak rada pritisnite Escape");
                Random r = new Random();

                while (true)
                {
                    Console.WriteLine("\nPritisnite bilo koji taster za slanje novog merenja (Escape za izlaz)...");
                    var taster = Console.ReadKey(true);
                    if (taster.Key == ConsoleKey.Escape) break;

                    Merenje merenje = new Merenje();
                    merenje.Tip = tm;
                    merenje.NazivStanice = ""; 

                    switch (tm)
                    {
                        case Tip_merenja.Temperatura:
                            merenje.Trenutna_vrednost = Math.Round(r.NextDouble() * 60 - 10, 2);
                            merenje.Jedinica_mere = "°C";
                            break;
                        case Tip_merenja.Vlaznost:
                            merenje.Trenutna_vrednost = Math.Round(r.NextDouble() * 100, 2);
                            merenje.Jedinica_mere = "%";
                            break;
                        case Tip_merenja.Vetar:
                            merenje.Trenutna_vrednost = Math.Round(r.NextDouble() * 120, 2);
                            merenje.Jedinica_mere = "km/h";
                            break;
                        case Tip_merenja.Padavine:
                            merenje.Trenutna_vrednost = Math.Round(r.NextDouble() * 200, 2);
                            merenje.Jedinica_mere = "mm";
                            break;
                    }

                    // --- SLANJE MERENJA ---
                    using (MemoryStream ms = new MemoryStream())
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        bf.Serialize(ms, merenje);
                        byte[] buffer = ms.ToArray();
                        clientSocket.SendTo(buffer, 0, buffer.Length, SocketFlags.None, destinationEP);
                    }
                    Console.WriteLine($"[POSLATO] {merenje.Tip}: {merenje.Trenutna_vrednost} {merenje.Jedinica_mere}");

                    // --- LOGIKA ALARMA ---
                    bool isAlarm = false;
                    string uzrok = "";

                    if (tm == Tip_merenja.Temperatura && merenje.Trenutna_vrednost > 40)
                    {
                        isAlarm = true;
                        uzrok = "Ekstremna vrućina!";
                    }
                    else if (tm == Tip_merenja.Vetar && merenje.Trenutna_vrednost > 100)
                    {
                        isAlarm = true;
                        uzrok = "Olujni vetar!";
                    }

                    if (isAlarm)
                    {
                        Alarm alarm = new Alarm
                        {
                            // Ovde koristimo tvoj enum Tip_alarma
                            Tip = (tm == Tip_merenja.Temperatura) ? Tip_alarma.Ekstremna_temperatura : Tip_alarma.Zagadjenje,
                            Trenutna_vrednost = merenje.Trenutna_vrednost,
                            Uzrok = uzrok
                        };

                        using (MemoryStream ms = new MemoryStream())
                        {
                            BinaryFormatter bf = new BinaryFormatter();
                            bf.Serialize(ms, alarm);
                            byte[] alarmBuffer = ms.ToArray();
                            clientSocket.SendTo(alarmBuffer, 0, alarmBuffer.Length, SocketFlags.None, destinationEP);
                        }

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[ALARM POSLAT] {uzrok} ({merenje.Trenutna_vrednost})");
                        Console.ResetColor();
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
