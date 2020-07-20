using System;
using System.Collections.Generic;

namespace ChatUIXForms.Models
{
    public class Intents
    {
        public string Intent { get; set; }
        public double Score { get; set; }
    }

    public class IntentsResult {
        public string Query { get; set; }
        public Intents topScoringIntent { get; set; }
    }
}
