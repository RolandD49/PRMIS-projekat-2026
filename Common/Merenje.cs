using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [Serializable]
    public class Merenje
    {
        private Tip_merenja tip;
        private double trenutna_vrednost;
        private string jedinica_mere;
        private string naziv_stanice;

        public Tip_merenja Tip { get => tip; set => tip = value; }
        public double Trenutna_vrednost { get => trenutna_vrednost; set => trenutna_vrednost = value; }
        public string Jedinica_mere { get => jedinica_mere; set => jedinica_mere = value; }
        public string NazivStanice { get => naziv_stanice; set => naziv_stanice = value; }
    }
}
