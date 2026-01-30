using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Common
{
    [Serializable]
    public class Alarm
    {
        /*private Tip_alarma tip;
        private double trenutna_vrednost;
        private string uzrok;

        public Tip_alarma Tip { get => tip; set => tip = value; }
        public double Trenutna_vrednost { get => trenutna_vrednost; set => trenutna_vrednost = value; }
        public string Uzrok { get => uzrok; set => uzrok = value; }*/
        public Tip_alarma Tip { get; set; }
        public double Trenutna_vrednost { get; set; }
        public string Uzrok { get; set; }

       
    }
}
