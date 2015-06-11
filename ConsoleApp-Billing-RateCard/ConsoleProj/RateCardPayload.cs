using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARMAPI_Test
{
    public class RateCardPayload
    {
        public List<object> OfferTerms { get; set; }
        public List<Resource> Meters { get; set; }
        public string Currency { get; set; }
        public string Locale { get; set; }
        public string RatingDate { get; set; }
        public bool IsTaxIncluded { get; set; }
    }
    
}
