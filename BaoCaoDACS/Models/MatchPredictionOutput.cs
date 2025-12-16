using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaoCaoDACS.Models
{
    public class MatchPredictionOutput
    {
        public bool PredictedLabel { get; set; } 
        public float Probability { get; set; }   
        public float Score { get; set; }
    }


}
