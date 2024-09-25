using net.sf.saxon.expr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusKSAValidatorBL.Models
{

    public class Rootobject
    {
        public Xml xml { get; set; }
        public Invoice Invoice { get; set; }
    }

    public class Xml
    {
        public string version { get; set; }
        public string encoding { get; set; }
    }

    public class Invoice
    {
        public string xmlns { get; set; }
        public string xmlnscac { get; set; }
        public string xmlnscbc { get; set; }
        public string xmlnsext { get; set; }
        public ExtUblextensions extUBLExtensions { get; set; }
        public string ProfileID { get; set; }
        public string ID { get; set; }
        public string UUID { get; set; }
        public string IssueDate { get; set; }
        public string IssueTime { get; set; }
        public Invoicetypecode InvoiceTypeCode { get; set; }
        public string DocumentCurrencyCode { get; set; }
        public string TaxCurrencyCode { get; set; }
        public Additionaldocumentreference[] AdditionalDocumentReference { get; set; }
        public Signature Signature { get; set; }
        public Accountingsupplierparty AccountingSupplierParty { get; set; }
        public Accountingcustomerparty AccountingCustomerParty { get; set; }
        public Delivery Delivery { get; set; }
        //public Paymentmeans PaymentMeans { get; set; }
        public Taxtotal[] TaxTotal { get; set; }
        public Legalmonetarytotal LegalMonetaryTotal { get; set; }
        public OrderReference OrderReference { get; set; }
        public BillingReference BillingReference { get; set; }
        public AllowanceCharge AllowanceCharge { get; set; }
    }
    public class AllowanceCharge
    {
        public string MultiplierFactorNumeric { get; set; }
        public string ChargeIndicator { get; set; }
        public Amount Amount { get; set; }
        public Amount BaseAmount { get; set; }
        public Taxcategory TaxCategory { get; set; }
    }
    public class OrderReference
    {
        public string ID { get; set; }
    }
    public class BillingReference
    {
        public InvoiceDocumentReference InvoiceDocumentReference { get; set; }
    }
    public class InvoiceDocumentReference
    {
        public string ID { get; set; }
    }
    public class ExtUblextensions
    {
        public ExtUblextension extUBLExtension { get; set; }
    }

    public class ExtUblextension
    {
        public string extExtensionURI { get; set; }
        public ExtExtensioncontent extExtensionContent { get; set; }
    }

    public class ExtExtensioncontent
    {
        public SigUbldocumentsignatures sigUBLDocumentSignatures { get; set; }
    }

    public class SigUbldocumentsignatures
    {
        public string xmlnssig { get; set; }
        public string xmlnssac { get; set; }
        public string xmlnssbc { get; set; }
        public SacSignatureinformation sacSignatureInformation { get; set; }
    }

    public class SacSignatureinformation
    {
        public string ID { get; set; }
        public string sbcReferencedSignatureID { get; set; }
        public DsSignature dsSignature { get; set; }
    }

    public class DsSignature
    {
        public string Id { get; set; }
        public string xmlnsds { get; set; }
        public DsSignedinfo dsSignedInfo { get; set; }
        public string dsSignatureValue { get; set; }
        public DsKeyinfo dsKeyInfo { get; set; }
        public DsObject dsObject { get; set; }
    }

    public class DsSignedinfo
    {
        public DsCanonicalizationmethod dsCanonicalizationMethod { get; set; }
        public DsSignaturemethod dsSignatureMethod { get; set; }
        public DsReference[] dsReference { get; set; }
    }

    public class DsCanonicalizationmethod
    {
        public string Algorithm { get; set; }
    }

    public class DsSignaturemethod
    {
        public string Algorithm { get; set; }
    }

    public class DsReference
    {
        public string Id { get; set; }
        public string URI { get; set; }
        public DsTransforms dsTransforms { get; set; }
        public DsDigestmethod dsDigestMethod { get; set; }
        public string dsDigestValue { get; set; }
        public string Type { get; set; }
    }

    public class DsTransforms
    {
        public DsTransform[] dsTransform { get; set; }
    }

    public class DsTransform
    {
        public string Algorithm { get; set; }
        public string dsXPath { get; set; }
    }

    public class DsDigestmethod
    {
        public string Algorithm { get; set; }
    }

    public class DsKeyinfo
    {
        public DsX509data dsX509Data { get; set; }
    }

    public class DsX509data
    {
        public string dsX509Certificate { get; set; }
    }

    public class DsObject
    {
        public XadesQualifyingproperties xadesQualifyingProperties { get; set; }
    }

    public class XadesQualifyingproperties
    {
        public string Target { get; set; }
        public string xmlnsxades { get; set; }
        public XadesSignedproperties xadesSignedProperties { get; set; }
    }

    public class XadesSignedproperties
    {
        public string Id { get; set; }
        public XadesSignedsignatureproperties xadesSignedSignatureProperties { get; set; }
    }

    public class XadesSignedsignatureproperties
    {
        public DateTime xadesSigningTime { get; set; }
        public XadesSigningcertificate xadesSigningCertificate { get; set; }
    }

    public class XadesSigningcertificate
    {
        public XadesCert xadesCert { get; set; }
    }

    public class XadesCert
    {
        public XadesCertdigest xadesCertDigest { get; set; }
        public XadesIssuerserial xadesIssuerSerial { get; set; }
    }

    public class XadesCertdigest
    {
        public DsDigestmethod dsDigestMethod { get; set; }
        public string dsDigestValue { get; set; }
    }

    public class XadesIssuerserial
    {
        public string dsX509IssuerName { get; set; }
        public string dsX509SerialNumber { get; set; }
    }

    public class Invoicetypecode
    {
        public string name { get; set; }
        public string text { get; set; }
    }

    public class Signature
    {
        public string ID { get; set; }
        public string SignatureMethod { get; set; }
    }

    public class Accountingsupplierparty
    {
        public Party Party { get; set; }
    }

    public class Party
    {
        public Partyidentification PartyIdentification { get; set; }
        public Postaladdress PostalAddress { get; set; }
        public Partytaxscheme PartyTaxScheme { get; set; }
        public Partylegalentity PartyLegalEntity { get; set; }
    }

    public class Partyidentification
    {
        public ID ID { get; set; }
    }

    public class ID
    {
        public string schemeID { get; set; }
        public string text { get; set; }
    }

    public class Postaladdress
    {
        public string StreetName { get; set; }
        public string AdditionalStreetName { get; set; }
        public string BuildingNumber { get; set; }
        public string PlotIdentification { get; set; }
        public string CitySubdivisionName { get; set; }
        public string CityName { get; set; }
        public string PostalZone { get; set; }
        public string CountrySubentity { get; set; }
        public Country Country { get; set; }
    }

    public class Country
    {
        public string IdentificationCode { get; set; }
    }

    public class Partytaxscheme
    {
        public string CompanyID { get; set; }
        public Taxscheme TaxScheme { get; set; }
    }

    public class Taxscheme
    {
        public string ID { get; set; }
    }

    public class Partylegalentity
    {
        public string RegistrationName { get; set; }
    }

    public class Accountingcustomerparty
    {
        public Party Party { get; set; }
    }

    public class Delivery
    {
        public string ActualDeliveryDate { get; set; }
        public string LatestDeliveryDate { get; set; }
    }

    public class Paymentmeans
    {
        public string PaymentMeansCode { get; set; }
        public string InstructionNote { get; set; }
    }

    public class Legalmonetarytotal
    {
        public Amount LineExtensionAmount { get; set; }
        public Amount TaxExclusiveAmount { get; set; }
        public Amount TaxInclusiveAmount { get; set; }
        public Amount AllowanceTotalAmount { get; set; }
        public Amount ChargeTotalAmount { get; set; }
        public Amount PayableRoundingAmount { get; set; }
        public Amount PayableAmount { get; set; }
        public Amount PrepaidAmount { get; set; }
    }

    public class Invoiceline
    {
        public string ID { get; set; }
        public Invoicedquantity InvoicedQuantity { get; set; }
        public Amount LineExtensionAmount { get; set; }
        public Allowancecharge AllowanceCharge { get; set; }
        public TaxtotalInvLine TaxTotal { get; set; }
        public Item Item { get; set; }
        public Price Price { get; set; }
    }

    public class Invoicedquantity
    {
        public string unitCode { get; set; }
        public string text { get; set; }
    }

    public class Allowancecharge
    {
        public string ChargeIndicator { get; set; }
        public string MultiplierFactorNumeric { get; set; }
        public string AllowanceChargeReasonCode { get; set; }
        public Amount Amount { get; set; }
        public Amount BaseAmount { get; set; }
    }

    public class Amount
    {
        public string currencyID { get; set; }
        public string text { get; set; }
    }

    public class TaxtotalInvLine
    {
        public Amount TaxAmount { get; set; }
        public Amount RoundingAmount { get; set; }
    }

    public class Item
    {
        public string Name { get; set; }
        public Buyersitemidentification BuyersItemIdentification { get; set; }
        public Sellersitemidentification SellersItemIdentification { get; set; }
        public Standarditemidentification StandardItemIdentification { get; set; }
        public Classifiedtaxcategory ClassifiedTaxCategory { get; set; }
    }

    public class Buyersitemidentification
    {
        public string ID { get; set; }
    }

    public class Sellersitemidentification
    {
        public string ID { get; set; }
    }

    public class Standarditemidentification
    {
        public string ID { get; set; }
    }

    public class Classifiedtaxcategory
    {
        public string ID { get; set; }
        public string Percent { get; set; }
        public Taxscheme TaxScheme { get; set; }
    }

    public class Price
    {
        public Amount PriceAmount { get; set; }
        public Allowancecharge AllowanceCharge { get; set; }
        public Invoicedquantity BaseQuantity { get; set; }
    }

    public class Additionaldocumentreference
    {
        public string ID { get; set; }
        public string UUID { get; set; }
        public Attachment Attachment { get; set; }
    }

    public class Attachment
    {
        public Embeddeddocumentbinaryobject EmbeddedDocumentBinaryObject { get; set; }
    }

    public class Embeddeddocumentbinaryobject
    {
        public string mimeCode { get; set; }
        public string text { get; set; }
    }

    public class Taxtotal
    {
        public Amount TaxAmount { get; set; }
    }

    public class Taxsubtotal
    {
        public Amount TaxableAmount { get; set; }
        public Amount TaxAmount { get; set; }
        public Taxcategory TaxCategory { get; set; }
    }

    public class Taxcategory
    {
        public string ID { get; set; }
        public string Percent { get; set; }
        public string TaxExemptionReasonCode { get; set; }
        public Taxscheme TaxScheme { get; set; }
    }

    public class ErrorModel
    {
        public string PropertyName { get; set; }
        public string PropertyValue { get; set; }
        public string errMsg { get; set; }
    }
}
