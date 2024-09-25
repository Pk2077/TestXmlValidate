using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusKSAValidatorBL.Models
{
    public class InvoiceLineMulti
    {
        public Invoiceline InvoiceLine { get; set; }
    }
    public class TaxsubtotalMulti
    {
        public Taxsubtotal TaxSubtotal { get; set; }
    }
    public class PartyIdentificationMulti
    {
        public Partyidentification PartyIdentification { get; set; }
    }
    public class PaymentmeansMulti
    {
        public Paymentmeans PaymentMeans { get; set; }
    }
}
