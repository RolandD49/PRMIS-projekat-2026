using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [Serializable]
    public class Stanica
    {
        private string naziv;
        private double sirina;
        private double duzina;
        private int br_mernih_uredjaja;

        public string Naziv { get => naziv; set => naziv = value; }
        public double Sirina { get => sirina; set => sirina = value; }
        public double Duzina { get => duzina; set => duzina = value; }
        public int Br_mernih_uredjaja { get => br_mernih_uredjaja; set => br_mernih_uredjaja = value; }
    }
}
