using FocusKSAValidatorBL.Extensions;
using FocusKSAValidatorBL.Models;
using net.sf.saxon.om;
using Newtonsoft.Json;
using org.apache.xerces.xni;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text.RegularExpressions;
using System.Xml;
using static net.sf.saxon.functions.ConstantFunction;

namespace FocusKSAValidatorBL
{
    public class KSAXMLValidator
    {
        public List<ErrorModel> Validate(string xmlString)
        {
            string errorMsg = string.Empty;
            Rootobject BaseXmls = new Rootobject();
            List<ErrorModel> _ValidationErrors = new List<ErrorModel>();
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlString);
                string jsonText = JsonConvert.SerializeXmlNode(xmlDoc);
                jsonText = new ValidatorExtensions().JsonHandler(jsonText);
                BaseXmls = JsonConvert.DeserializeObject<Rootobject>(jsonText);
            }
            catch (Exception ex)
            {
                ErrorModel ErrorObj = new ErrorModel();
                ErrorObj.errMsg = "XML format error " + ex.Message;
                _ValidationErrors.Add(ErrorObj);
                return _ValidationErrors;
            }

            Invoice Invoice = BaseXmls.Invoice;
            List<char> TaxTypes = new List<char>() { 'S', 'Z', 'E', 'O' };

            try
            {
                if (Invoice != null)
                {
                    #region Headervalidation
                    List<ErrorModel> _Headervalidation = new List<ErrorModel>();
                    if (!string.IsNullOrWhiteSpace(Invoice.ProfileID) && Invoice.ProfileID != "reporting:1.0")
                    {
                        _Headervalidation.Add(new ErrorModel()
                        {
                            errMsg = "ProfileID must be 'reporting:1.0'",
                            PropertyName = nameof(Invoice.ProfileID),
                            PropertyValue = Invoice.ProfileID
                        });
                    }
                    if (string.IsNullOrWhiteSpace(Invoice.ID))
                    {
                        _Headervalidation.Add(new ErrorModel()
                        {
                            errMsg = "Document No. is mandatory",
                            PropertyName = "Document No.",
                            PropertyValue = "Document No."
                        });
                    }
                    if (!string.IsNullOrWhiteSpace(Invoice.UUID))
                    {
                        _Headervalidation.Add(new ValidatorExtensions().ValidateSpecialStrings(Invoice.UUID, nameof(Invoice.UUID), "-"));
                    }
                    if (!string.IsNullOrWhiteSpace(Invoice.IssueDate))
                    {
                        _Headervalidation.Add(new ValidatorExtensions().ValidateDateProperty(Invoice.IssueDate, nameof(Invoice.IssueDate)));
                        DateTime isValid;
                        DateTime.TryParse(Invoice.IssueDate, out isValid);
                        if (isValid > DateTime.Now)
                        {
                            _Headervalidation.Add(new ErrorModel
                            {
                                errMsg = $"'Invoice/IssueDate' should not be greater than today",
                                PropertyName = "Invoice/IssueDate",
                                PropertyValue = Invoice.IssueDate
                            });
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(Invoice.IssueTime))
                    {
                        _Headervalidation.Add(new ValidatorExtensions().ValidateTimeProperty(Invoice.IssueTime, nameof(Invoice.IssueTime)));
                    }
                    if (Invoice.InvoiceTypeCode != null)
                    {
                        if (!string.IsNullOrWhiteSpace(Invoice.InvoiceTypeCode.text))
                        {
                            _Headervalidation.Add(new ValidatorExtensions().ValidateDocumentTypeCode(Invoice.InvoiceTypeCode.text, nameof(Invoice.InvoiceTypeCode)));
                        }
                        if (!string.IsNullOrWhiteSpace(Invoice.InvoiceTypeCode.name))
                        {
                            List<ErrorModel> _InvoiceTypeCodeErrors = new ValidatorExtensions().ValidateTransactionCode(Invoice.InvoiceTypeCode.name, "Invoice transaction code");
                            if (_InvoiceTypeCodeErrors.Count > 0)
                            {
                                foreach (var item in _InvoiceTypeCodeErrors)
                                {
                                    _Headervalidation.Add(item);
                                }
                            }
                        }
                        if (Invoice.InvoiceTypeCode.text == "381" || Invoice.InvoiceTypeCode.text == "383")//credit 381 , debit 383
                        {
                            if (Invoice.BillingReference != null && Invoice.BillingReference.InvoiceDocumentReference != null)
                            {
                                if (string.IsNullOrWhiteSpace(Invoice.BillingReference.InvoiceDocumentReference.ID))
                                {
                                    var errorModel = new ErrorModel();
                                    errorModel.errMsg = $"Billing reference ID is mandatory.";
                                    errorModel.PropertyName = nameof(Invoice.BillingReference);
                                    errorModel.PropertyValue = "null";
                                    _Headervalidation.Add(errorModel);
                                }
                            }
                            else
                            {
                                var errorModel = new ErrorModel();
                                errorModel.errMsg = $"Billing reference ID is mandatory.";
                                errorModel.PropertyName = nameof(Invoice.BillingReference);
                                errorModel.PropertyValue = "null";
                                _Headervalidation.Add(errorModel);
                            }
                        }
                    }
                    else
                    {
                        var errorModel = new ErrorModel();
                        errorModel.errMsg = $"An Invoice shall have an Invoice type code";
                        errorModel.PropertyName = nameof(Invoice.InvoiceTypeCode);
                        errorModel.PropertyValue = "null";
                        _Headervalidation.Add(errorModel);
                    }
                    if (!string.IsNullOrWhiteSpace(Invoice.DocumentCurrencyCode))
                    {
                        _Headervalidation.Add(new ValidatorExtensions().ValidateCurrencyCode(Invoice.DocumentCurrencyCode, nameof(Invoice.DocumentCurrencyCode)));
                    }
                    if (!string.IsNullOrWhiteSpace(Invoice.TaxCurrencyCode))
                    {
                        _Headervalidation.Add(new ValidatorExtensions().ValidateCurrencyCode(Invoice.TaxCurrencyCode, nameof(Invoice.TaxCurrencyCode)));
                        if (Invoice.TaxCurrencyCode != "SAR")
                        {
                            var errorModel = new ErrorModel();
                            errorModel.errMsg = $"VAT accounting currency code must be 'SAR'";
                            errorModel.PropertyName = nameof(Invoice.TaxCurrencyCode);
                            errorModel.PropertyValue = Invoice.TaxCurrencyCode;
                            _Headervalidation.Add(errorModel);
                        }
                    }
                    if (Invoice.AdditionalDocumentReference != null && Invoice.AdditionalDocumentReference.Count() > 0)
                    {
                        foreach (var item in Invoice.AdditionalDocumentReference)
                        {
                            if (item.ID == "ICV")
                            {
                                _Headervalidation.Add(new ValidatorExtensions().ValidateOnlyNumeric(item.UUID, nameof(Invoice.AdditionalDocumentReference)));
                            }
                            //if (item.ID == "PIH")
                            //{
                            //    if (item.Attachment != null && item.Attachment.EmbeddedDocumentBinaryObject != null && !string.IsNullOrWhiteSpace(item.Attachment.EmbeddedDocumentBinaryObject.text))
                            //    {
                            //        _Headervalidation.Add(new ValidatorExtensions().ValidatePreviousInvoiceHash(xmlString, item.Attachment.EmbeddedDocumentBinaryObject.text, nameof(Invoice.AdditionalDocumentReference)));
                            //    }
                            //}
                        }
                        if (Invoice.AdditionalDocumentReference.Where(x => x.ID == "QR").ToList().Count <= 0)
                        {
                            _Headervalidation.Add(new ErrorModel
                            {
                                errMsg = "The document must contain a QR code",
                                PropertyName = nameof(Invoice.AdditionalDocumentReference),
                                PropertyValue = ""
                            });
                        }
                    }
                    //if (Invoice.Signature != null && !string.IsNullOrWhiteSpace(Invoice.Signature.ID))
                    //{
                    //    List<ErrorModel> _InvoiceSignErrors = new ValidatorExtensions().ValidateCryptographicStamp(xmlString, nameof(Invoice.Signature));
                    //    if (_InvoiceSignErrors.Count > 0)
                    //    {
                    //        foreach (var item in _InvoiceSignErrors)
                    //        {
                    //            _Headervalidation.Add(item);
                    //        }
                    //    }
                    //}
                    if (_Headervalidation.Count > 0)
                    {
                        foreach (var item in _Headervalidation)
                        {
                            if (!string.IsNullOrWhiteSpace(item.errMsg))
                                _ValidationErrors.Add(item);
                        }
                    }
                    #endregion
                    #region AccountingSupplierParty
                    if (Invoice.AccountingSupplierParty != null)
                    {
                        List<ErrorModel> _AccountingSupplierPartyErrors = new List<ErrorModel>();
                        var _AccountingSupplierPartyIdentification = new ValidatorExtensions().GetMultiElements<PartyIdentificationMulti>(xmlString, "PartyIdentification", "AccountingSupplierParty", "Party", true, ref errorMsg);
                        if (_AccountingSupplierPartyIdentification != null && _AccountingSupplierPartyIdentification.Count > 0)
                        {
                            foreach (var PartyIdentificationMulti in _AccountingSupplierPartyIdentification)
                            {
                                if (PartyIdentificationMulti != null && PartyIdentificationMulti.PartyIdentification != null)
                                {
                                    var item = PartyIdentificationMulti.PartyIdentification;
                                    if (item.ID != null && !string.IsNullOrWhiteSpace(item.ID.text))
                                    {
                                        var pattern = "^[a-zA-Z0-9]+$";
                                        var regex = new Regex(pattern);
                                        if (!regex.IsMatch(item.ID.text))
                                        {
                                            _AccountingSupplierPartyErrors.Add(new ErrorModel
                                            {
                                                errMsg = "The seller identification must contain only alphanumeric characters",
                                                PropertyName = "AccountingSupplierParty/Party/PartyIdentification",
                                                PropertyValue = item.ID.text
                                            });
                                        }
                                    }
                                }
                            }
                        }
                        if (Invoice.AccountingSupplierParty.Party != null && Invoice.AccountingSupplierParty.Party.PostalAddress != null)
                        {
                            if (string.IsNullOrWhiteSpace(Invoice.AccountingSupplierParty.Party.PostalAddress.StreetName))
                            {
                                _AccountingSupplierPartyErrors.Add(new ErrorModel { errMsg = "AccountingSupplierParty/StreetName is mandatory", PropertyName = "StreetName", PropertyValue = "" });
                            }
                            if (!string.IsNullOrWhiteSpace(Invoice.AccountingSupplierParty.Party.PostalAddress.BuildingNumber))
                            {
                                if (!Regex.IsMatch(Invoice.AccountingSupplierParty.Party.PostalAddress.BuildingNumber, @"^\d{4}$"))
                                {
                                    _AccountingSupplierPartyErrors.Add(new ErrorModel { errMsg = "AccountingSupplierParty/BuildingNumber must contain 4 digits", PropertyName = "BuildingNumber", PropertyValue = Invoice.AccountingSupplierParty.Party.PostalAddress.BuildingNumber });
                                }
                            }
                            else
                            {
                                _AccountingSupplierPartyErrors.Add(new ErrorModel { errMsg = "AccountingSupplierParty/BuildingNumber is mandatory", PropertyName = "BuildingNumber", PropertyValue = Invoice.AccountingSupplierParty.Party.PostalAddress.BuildingNumber });
                            }
                            if (!string.IsNullOrWhiteSpace(Invoice.AccountingSupplierParty.Party.PostalAddress.PlotIdentification))
                            {
                                if (!Regex.IsMatch(Invoice.AccountingSupplierParty.Party.PostalAddress.PlotIdentification, @"^\d{4}$"))
                                {
                                    _AccountingSupplierPartyErrors.Add(new ErrorModel { errMsg = "AccountingSupplierParty/PlotIdentification must contain 4 digits", PropertyName = "PlotIdentification", PropertyValue = Invoice.AccountingSupplierParty.Party.PostalAddress.PlotIdentification });
                                }
                            }
                            if (string.IsNullOrWhiteSpace(Invoice.AccountingSupplierParty.Party.PostalAddress.CityName))
                            {
                                _AccountingSupplierPartyErrors.Add(new ErrorModel { errMsg = "AccountingSupplierParty/CityName is mandatory", PropertyName = "CityName", PropertyValue = "" });
                            }
                            if (!string.IsNullOrWhiteSpace(Invoice.AccountingSupplierParty.Party.PostalAddress.PostalZone))
                            {
                                if (!Regex.IsMatch(Invoice.AccountingSupplierParty.Party.PostalAddress.PostalZone, @"^\d{5}$"))
                                {
                                    _AccountingSupplierPartyErrors.Add(new ErrorModel { errMsg = "AccountingSupplierParty/PostalZone must contain 5 digits", PropertyName = "PostalZone", PropertyValue = Invoice.AccountingSupplierParty.Party.PostalAddress.PostalZone });
                                }
                            }
                            else
                            {
                                _AccountingSupplierPartyErrors.Add(new ErrorModel { errMsg = "AccountingSupplierParty/PostalZone is mandatory", PropertyName = "PostalZone", PropertyValue = Invoice.AccountingSupplierParty.Party.PostalAddress.PostalZone });
                            }
                            if (string.IsNullOrWhiteSpace(Invoice.AccountingSupplierParty.Party.PostalAddress.CitySubdivisionName))
                            {
                                _AccountingSupplierPartyErrors.Add(new ErrorModel { errMsg = "AccountingSupplierParty/CitySubdivisionName is mandatory", PropertyName = "CitySubdivisionName", PropertyValue = "" });
                            }
                            if (Invoice.AccountingSupplierParty.Party.PostalAddress.Country != null && !string.IsNullOrWhiteSpace(Invoice.AccountingSupplierParty.Party.PostalAddress.Country.IdentificationCode))
                            {
                                _AccountingSupplierPartyErrors.Add(new ValidatorExtensions().ValidateCountry(Invoice.AccountingSupplierParty.Party.PostalAddress.Country.IdentificationCode, "AccountingSupplierParty/Country"));
                            }
                            if (Invoice.AccountingSupplierParty.Party.PartyTaxScheme != null && !string.IsNullOrWhiteSpace(Invoice.AccountingSupplierParty.Party.PartyTaxScheme.CompanyID))
                            {
                                if (Invoice.AccountingSupplierParty.Party.PartyTaxScheme.TaxScheme != null && !string.IsNullOrWhiteSpace(Invoice.AccountingSupplierParty.Party.PartyTaxScheme.TaxScheme.ID) && Invoice.AccountingSupplierParty.Party.PartyTaxScheme.TaxScheme.ID == "VAT")
                                {
                                    _AccountingSupplierPartyErrors.Add(new ValidatorExtensions().ValidateVatNo(Invoice.AccountingSupplierParty.Party.PartyTaxScheme.CompanyID, "AccountingSupplierParty/PartyTaxScheme/CompanyID"));
                                }
                            }
                            if (Invoice.AccountingSupplierParty.Party.PartyLegalEntity != null)
                            {
                                if (string.IsNullOrWhiteSpace(Invoice.AccountingSupplierParty.Party.PartyLegalEntity.RegistrationName))
                                {
                                    _AccountingSupplierPartyErrors.Add(new ErrorModel()
                                    {
                                        errMsg = "AccountingSupplierParty RegistrationName is mandatory.",
                                        PropertyName = "AccountingSupplierParty/Party/PartyLegalEntity/RegistrationName",
                                        PropertyValue = ""
                                    });
                                }
                            }
                            else
                            {
                                _AccountingSupplierPartyErrors.Add(new ErrorModel()
                                {
                                    errMsg = "AccountingSupplierParty RegistrationName is mandatory.",
                                    PropertyName = "AccountingSupplierParty/Party/PartyLegalEntity/RegistrationName",
                                    PropertyValue = ""
                                });
                            }
                        }
                        if (_AccountingSupplierPartyErrors.Count > 0)
                        {
                            foreach (var item in _AccountingSupplierPartyErrors)
                            {
                                if (!string.IsNullOrWhiteSpace(item.errMsg))
                                    _ValidationErrors.Add(item);
                            }
                        }
                    }
                    #endregion
                    #region AccountingCustomerParty
                    if (Invoice.AccountingCustomerParty != null)
                    {
                        List<ErrorModel> _AccountingCustomerPartyErrors = new List<ErrorModel>();
                        var _AccountingCustomerPartyIdentification = new ValidatorExtensions().GetMultiElements<PartyIdentificationMulti>(xmlString, "PartyIdentification", "AccountingCustomerParty", "Party", true, ref errorMsg);
                        if (_AccountingCustomerPartyIdentification != null && _AccountingCustomerPartyIdentification.Count > 0)
                        {
                            foreach (var PartyIdentificationMulti in _AccountingCustomerPartyIdentification)
                            {
                                if (PartyIdentificationMulti != null && PartyIdentificationMulti.PartyIdentification != null)
                                {
                                    var item = PartyIdentificationMulti.PartyIdentification;
                                    if (item.ID != null && !string.IsNullOrWhiteSpace(item.ID.text))
                                    {
                                        var pattern = "^[a-zA-Z0-9]+$";
                                        var regex = new Regex(pattern);
                                        if (!regex.IsMatch(item.ID.text))
                                        {
                                            _AccountingCustomerPartyErrors.Add(new ErrorModel
                                            {
                                                errMsg = "The buyer identification must contain only alphanumeric characters",
                                                PropertyName = "AccountingCustomerParty/Party/PartyIdentification",
                                                PropertyValue = item.ID.text
                                            });
                                        }
                                    }
                                }
                            }
                        }
                        if (Invoice.AccountingCustomerParty.Party != null && Invoice.AccountingCustomerParty.Party.PostalAddress != null)
                        {
                            if (string.IsNullOrWhiteSpace(Invoice.AccountingCustomerParty.Party.PostalAddress.StreetName))
                            {
                                _AccountingCustomerPartyErrors.Add(new ErrorModel { errMsg = "AccountingCustomerParty/StreetName is mandatory", PropertyName = "StreetName", PropertyValue = "" });
                            }
                            if (!string.IsNullOrWhiteSpace(Invoice.AccountingCustomerParty.Party.PostalAddress.BuildingNumber))
                            {
                                if (!Regex.IsMatch(Invoice.AccountingCustomerParty.Party.PostalAddress.BuildingNumber, @"^\d{4}$"))
                                {
                                    _AccountingCustomerPartyErrors.Add(new ErrorModel { errMsg = "AccountingCustomerParty/BuildingNumber must contain 4 digits", PropertyName = "BuildingNumber", PropertyValue = Invoice.AccountingCustomerParty.Party.PostalAddress.BuildingNumber });
                                }
                            }
                            else
                            {
                                _AccountingCustomerPartyErrors.Add(new ErrorModel { errMsg = "AccountingCustomerParty/BuildingNumber is mandatory", PropertyName = "BuildingNumber", PropertyValue = Invoice.AccountingCustomerParty.Party.PostalAddress.BuildingNumber });
                            }
                            if (!string.IsNullOrWhiteSpace(Invoice.AccountingCustomerParty.Party.PostalAddress.PlotIdentification))
                            {
                                if (!Regex.IsMatch(Invoice.AccountingCustomerParty.Party.PostalAddress.PlotIdentification, @"^\d{4}$"))
                                {
                                    _AccountingCustomerPartyErrors.Add(new ErrorModel { errMsg = "AccountingCustomerParty/PlotIdentification must contain 4 digits", PropertyName = "PlotIdentification", PropertyValue = Invoice.AccountingCustomerParty.Party.PostalAddress.PlotIdentification });
                                }
                            }
                            if (string.IsNullOrWhiteSpace(Invoice.AccountingCustomerParty.Party.PostalAddress.CityName))
                            {
                                _AccountingCustomerPartyErrors.Add(new ErrorModel { errMsg = "AccountingCustomerParty/CityName is mandatory", PropertyName = "CityName", PropertyValue = "" });
                            }
                            if (!string.IsNullOrWhiteSpace(Invoice.AccountingCustomerParty.Party.PostalAddress.PostalZone))
                            {
                                if (!Regex.IsMatch(Invoice.AccountingCustomerParty.Party.PostalAddress.PostalZone, @"^\d{5}$"))
                                {
                                    _AccountingCustomerPartyErrors.Add(new ErrorModel { errMsg = "AccountingCustomerParty/PostalZone must contain 5 digits", PropertyName = "PostalZone", PropertyValue = Invoice.AccountingCustomerParty.Party.PostalAddress.PostalZone });
                                }
                            }
                            else
                            {
                                _AccountingCustomerPartyErrors.Add(new ErrorModel { errMsg = "AccountingCustomerParty/PostalZone is mandatory", PropertyName = "PostalZone", PropertyValue = Invoice.AccountingCustomerParty.Party.PostalAddress.PostalZone });
                            }
                            if (string.IsNullOrWhiteSpace(Invoice.AccountingCustomerParty.Party.PostalAddress.CitySubdivisionName))
                            {
                                _AccountingCustomerPartyErrors.Add(new ErrorModel { errMsg = "AccountingCustomerParty/CitySubdivisionName is mandatory", PropertyName = "CitySubdivisionName", PropertyValue = "" });
                            }
                            if (Invoice.AccountingCustomerParty.Party.PostalAddress.Country != null && !string.IsNullOrWhiteSpace(Invoice.AccountingCustomerParty.Party.PostalAddress.Country.IdentificationCode))
                            {
                                _AccountingCustomerPartyErrors.Add(new ValidatorExtensions().ValidateCountry(Invoice.AccountingCustomerParty.Party.PostalAddress.Country.IdentificationCode, "AccountingCustomerParty/Country"));
                            }
                            if (Invoice.AccountingCustomerParty.Party.PartyTaxScheme != null && !string.IsNullOrWhiteSpace(Invoice.AccountingCustomerParty.Party.PartyTaxScheme.CompanyID))
                            {
                                if (Invoice.AccountingCustomerParty.Party.PartyTaxScheme.TaxScheme != null && !string.IsNullOrWhiteSpace(Invoice.AccountingCustomerParty.Party.PartyTaxScheme.TaxScheme.ID) && Invoice.AccountingCustomerParty.Party.PartyTaxScheme.TaxScheme.ID == "VAT")
                                {
                                    _AccountingCustomerPartyErrors.Add(new ValidatorExtensions().ValidateVatNo(Invoice.AccountingCustomerParty.Party.PartyTaxScheme.CompanyID, "AccountingCustomerParty/PartyTaxScheme/CompanyID"));
                                }
                            }
                            if (Invoice.AccountingCustomerParty.Party.PartyLegalEntity != null)
                            {
                                if (string.IsNullOrWhiteSpace(Invoice.AccountingCustomerParty.Party.PartyLegalEntity.RegistrationName))
                                {
                                    _AccountingCustomerPartyErrors.Add(new ErrorModel()
                                    {
                                        errMsg = "AccountingCustomerParty RegistrationName is mandatory.",
                                        PropertyName = "AccountingCustomerParty/Party/PartyLegalEntity/RegistrationName",
                                        PropertyValue = ""
                                    });
                                }
                            }
                            else
                            {
                                _AccountingCustomerPartyErrors.Add(new ErrorModel()
                                {
                                    errMsg = "AccountingCustomerParty RegistrationName is mandatory.",
                                    PropertyName = "AccountingCustomerParty/Party/PartyLegalEntity/RegistrationName",
                                    PropertyValue = ""
                                });
                            }
                        }
                        if (_AccountingCustomerPartyErrors.Count > 0)
                        {
                            foreach (var item in _AccountingCustomerPartyErrors)
                            {
                                if (!string.IsNullOrWhiteSpace(item.errMsg))
                                    _ValidationErrors.Add(item);
                            }
                        }
                    }
                    #endregion
                    #region Delivery
                    if (Invoice.Delivery != null)
                    {
                        List<ErrorModel> _Deliveryvalidation = new List<ErrorModel>();
                        if (!string.IsNullOrWhiteSpace(Invoice.Delivery.ActualDeliveryDate))
                        {
                            _Deliveryvalidation.Add(new ValidatorExtensions().ValidateDateProperty(Invoice.Delivery.ActualDeliveryDate, "Delivery/ActualDeliveryDate"));
                        }
                        if (!string.IsNullOrWhiteSpace(Invoice.Delivery.LatestDeliveryDate))
                        {
                            _Deliveryvalidation.Add(new ValidatorExtensions().ValidateDateProperty(Invoice.Delivery.LatestDeliveryDate, "Delivery/LatestDeliveryDate"));
                        }
                        if (!string.IsNullOrWhiteSpace(Invoice.Delivery.LatestDeliveryDate))
                        {
                            if (string.IsNullOrWhiteSpace(Invoice.Delivery.ActualDeliveryDate))
                            {
                                _Deliveryvalidation.Add(new ErrorModel { errMsg = "If the invoice contains a supply end date, then the invoice must contain a supply date.", PropertyName = "Delivery/ActualDeliveryDate", PropertyValue = "" });
                            }
                            else
                            {
                                DateTime LatestDeliveryDate;
                                DateTime ActualDeliveryDate;
                                DateTime.TryParse(Invoice.Delivery.LatestDeliveryDate, out LatestDeliveryDate);
                                DateTime.TryParse(Invoice.Delivery.LatestDeliveryDate, out ActualDeliveryDate);
                                if (LatestDeliveryDate < ActualDeliveryDate)
                                {
                                    _Deliveryvalidation.Add(new ErrorModel { errMsg = "Supply end date must be greater than or equal to the supply date.", PropertyName = "Delivery/ActualDeliveryDate", PropertyValue = "LatestDeliveryDate : " + LatestDeliveryDate + " ActualDeliveryDate : " + ActualDeliveryDate });
                                }
                            }
                        }
                        if (Invoice.InvoiceTypeCode != null && !string.IsNullOrWhiteSpace(Invoice.InvoiceTypeCode.text))
                        {
                            if (Invoice.InvoiceTypeCode.text == "388" && Invoice.InvoiceTypeCode.name.Substring(0, 2) == "01")
                            {
                                if (string.IsNullOrWhiteSpace(Invoice.Delivery.ActualDeliveryDate))
                                {
                                    _Deliveryvalidation.Add(new ErrorModel { errMsg = "The tax invoice must contain the supply date.", PropertyName = "Delivery/ActualDeliveryDate", PropertyValue = "" });
                                }
                            }
                            if (Invoice.InvoiceTypeCode.name.Substring(0, 2) == "02" && Invoice.InvoiceTypeCode.name[6] == '1')
                            {
                                if (string.IsNullOrWhiteSpace(Invoice.Delivery.ActualDeliveryDate))
                                {
                                    _Deliveryvalidation.Add(new ErrorModel { errMsg = "Simplified invoice type must contain the supply date.", PropertyName = "Delivery/ActualDeliveryDate", PropertyValue = "" });
                                }
                                if (string.IsNullOrWhiteSpace(Invoice.Delivery.LatestDeliveryDate))
                                {
                                    _Deliveryvalidation.Add(new ErrorModel { errMsg = "Simplified invoice type must contain the supply end date.", PropertyName = "Delivery/LatestDeliveryDate", PropertyValue = "" });
                                }
                            }
                        }
                        if (_Deliveryvalidation.Count > 0)
                        {
                            foreach (var item in _Deliveryvalidation)
                            {
                                if (!string.IsNullOrWhiteSpace(item.errMsg))
                                    _ValidationErrors.Add(item);
                            }
                        }
                    }
                    #endregion
                    #region PaymentMeans 
                    List<ErrorModel> _PaymentMeansvalidation = new List<ErrorModel>();
                    var _PaymentmeansMulti = new ValidatorExtensions().GetMultiElements<PaymentmeansMulti>(xmlString, "PaymentMeans", true, ref errorMsg);
                    if (_PaymentmeansMulti != null && _PaymentmeansMulti.Count > 0)
                    {
                        foreach (var item in _PaymentmeansMulti)
                        {
                            if (item != null && item.PaymentMeans != null)
                            {
                                var PaymentMeans = item.PaymentMeans;
                                if (!string.IsNullOrWhiteSpace(PaymentMeans.PaymentMeansCode))
                                {
                                    int temp;
                                    if (int.TryParse(PaymentMeans.PaymentMeansCode, out temp))
                                    {
                                        if (temp <= 0 && temp >= 99)
                                        {
                                            _PaymentMeansvalidation.Add(new ErrorModel
                                            {
                                                errMsg = "Payment means code  must contain one of the values from subset of UNTDID 4461 code list",
                                                PropertyName = "PaymentMeans/PaymentMeansCode",
                                                PropertyValue = PaymentMeans.PaymentMeansCode
                                            });
                                        }
                                    }
                                    else
                                    {
                                        if (PaymentMeans.PaymentMeansCode != "ZZZ")
                                        {
                                            _PaymentMeansvalidation.Add(new ErrorModel
                                            {
                                                errMsg = "Payment means code  must contain one of the values from subset of UNTDID 4461 code list",
                                                PropertyName = "PaymentMeans/PaymentMeansCode",
                                                PropertyValue = PaymentMeans.PaymentMeansCode
                                            });
                                        }
                                    }
                                    if (Invoice.InvoiceTypeCode != null && !string.IsNullOrWhiteSpace(Invoice.InvoiceTypeCode.text) && (Invoice.InvoiceTypeCode.text == "383" || Invoice.InvoiceTypeCode.text == "381"))
                                    {
                                        if (string.IsNullOrWhiteSpace(PaymentMeans.InstructionNote))
                                        {
                                            _PaymentMeansvalidation.Add(new ErrorModel
                                            {
                                                errMsg = "Debit and credit note must contain the reason for this invoice type issuing",
                                                PropertyName = "PaymentMeans/InstructionNote",
                                                PropertyValue = PaymentMeans.InstructionNote
                                            });
                                        }
                                    }
                                }
                                else
                                {
                                    _PaymentMeansvalidation.Add(new ErrorModel
                                    {
                                        errMsg = "A Payment instruction shall specify the Payment means type code",
                                        PropertyName = "PaymentMeans/PaymentMeansCode",
                                        PropertyValue = ""
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        _PaymentMeansvalidation.Add(new ErrorModel
                        {
                            errMsg = "An Invoice must specify Payment instruction",
                            PropertyName = "PaymentMeans",
                            PropertyValue = ""
                        });
                    }
                    if (_PaymentMeansvalidation.Count > 0)
                    {
                        foreach (var item in _PaymentMeansvalidation)
                        {
                            if (!string.IsNullOrWhiteSpace(item.errMsg))
                                _ValidationErrors.Add(item);
                        }
                    }
                    #endregion
                    #region AllowanceCharges
                    List<ErrorModel> _AllowanceChargevalidation = new List<ErrorModel>();
                    if (Invoice.AllowanceCharge != null)
                    {
                        if (!string.IsNullOrWhiteSpace(Invoice.AllowanceCharge.ChargeIndicator) && !bool.TryParse(Invoice.AllowanceCharge.ChargeIndicator, out _))
                        {
                            _AllowanceChargevalidation.Add(new ErrorModel
                            {
                                errMsg = "AllowanceCharge/ChargeIndicator value MUST equal to 'false'/'True' respectively",
                                PropertyName = "AllowanceCharge/ChargeIndicator",
                                PropertyValue = Invoice.AllowanceCharge.ChargeIndicator
                            });
                        }
                        decimal MultiplierFactorNumeric = 0.00M;
                        if (!string.IsNullOrWhiteSpace(Invoice.AllowanceCharge.MultiplierFactorNumeric))
                        {
                            if (Invoice.AllowanceCharge.MultiplierFactorNumeric.Contains("%"))
                            {
                                //Only numerals are accepted, the percentage symbol (%) is not allowed.
                                _AllowanceChargevalidation.Add(new ErrorModel
                                {
                                    errMsg = "Only numerals are accepted, the percentage symbol (%) is not allowed",
                                    PropertyName = "AllowanceCharge/MultiplierFactorNumeric",
                                    PropertyValue = Invoice.AllowanceCharge.MultiplierFactorNumeric
                                });
                            }
                            if (decimal.TryParse(Invoice.AllowanceCharge.MultiplierFactorNumeric, out MultiplierFactorNumeric))
                            {
                                if (Invoice.AllowanceCharge.MultiplierFactorNumeric != MultiplierFactorNumeric.ToString("0.00") || MultiplierFactorNumeric < 0 || MultiplierFactorNumeric > 100)
                                {
                                    _AllowanceChargevalidation.Add(new ErrorModel
                                    {
                                        errMsg = "The allowance percentage values must be from 0.00 to 100.00, with maximum two decimal places.",
                                        PropertyName = "AllowanceCharge/MultiplierFactorNumeric",
                                        PropertyValue = Invoice.AllowanceCharge.MultiplierFactorNumeric
                                    });
                                }
                            }
                            if (Invoice.AllowanceCharge.BaseAmount == null || string.IsNullOrWhiteSpace(Invoice.AllowanceCharge.BaseAmount.text))
                            {
                                _AllowanceChargevalidation.Add(new ErrorModel
                                {
                                    errMsg = "AllowanceCharge/BaseAmount must be provided when AllowanceCharge/MultiplierFactorNumeric is provided",
                                    PropertyName = "AllowanceCharge/BaseAmount",
                                    PropertyValue = ""
                                });
                            }
                        }
                        if (Invoice.AllowanceCharge.BaseAmount != null)
                        {
                            if (!string.IsNullOrWhiteSpace(Invoice.AllowanceCharge.BaseAmount.text))
                            {
                                if (string.IsNullOrWhiteSpace(Invoice.AllowanceCharge.MultiplierFactorNumeric))
                                {
                                    _AllowanceChargevalidation.Add(new ErrorModel
                                    {
                                        errMsg = "AllowanceCharge/MultiplierFactorNumeric percentage must be provided when the AllowanceCharge/BaseAmount is provided",
                                        PropertyName = "AllowanceCharge/MultiplierFactorNumeric",
                                        PropertyValue = ""
                                    });
                                }
                                decimal BaseAmount;
                                if (decimal.TryParse(Invoice.AllowanceCharge.BaseAmount.text, out BaseAmount))
                                {
                                    if (BaseAmount.ToString("0.00") != Invoice.AllowanceCharge.BaseAmount.text)
                                    {
                                        _AllowanceChargevalidation.Add(new ErrorModel
                                        {
                                            errMsg = "The allowed maximum number of decimals for the Document level allowance base amount is 2",
                                            PropertyName = "AllowanceCharge/BaseAmount",
                                            PropertyValue = Invoice.AllowanceCharge.BaseAmount.text
                                        });
                                    }
                                    _AllowanceChargevalidation.Add(new ValidatorExtensions().ValidateDecimal(BaseAmount, "AllowanceCharge/BaseAmount"));
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(Invoice.AllowanceCharge.BaseAmount.currencyID))
                            {
                                _AllowanceChargevalidation.Add(new ValidatorExtensions().ValidateCurrencyCode(Invoice.AllowanceCharge.BaseAmount.currencyID, "AllowanceCharge/BaseAmount"));
                            }
                        }
                        if (Invoice.AllowanceCharge.Amount != null)
                        {
                            if (!string.IsNullOrWhiteSpace(Invoice.AllowanceCharge.Amount.text))
                            {
                                decimal Amount;
                                if (decimal.TryParse(Invoice.AllowanceCharge.Amount.text, out Amount))
                                {
                                    if (Amount.ToString("0.00") != Invoice.AllowanceCharge.Amount.text)
                                    {
                                        _AllowanceChargevalidation.Add(new ErrorModel
                                        {
                                            errMsg = "The allowed maximum number of decimals for the Document level allowance amount is 2",
                                            PropertyName = "AllowanceCharge/Amount",
                                            PropertyValue = Invoice.AllowanceCharge.Amount.text
                                        });
                                    }
                                    _AllowanceChargevalidation.Add(new ValidatorExtensions().ValidateDecimal(Amount, "AllowanceCharge/Amount"));
                                }
                                if (!string.IsNullOrWhiteSpace(Invoice.AllowanceCharge.MultiplierFactorNumeric) && Invoice.AllowanceCharge.BaseAmount != null && !string.IsNullOrWhiteSpace(Invoice.AllowanceCharge.BaseAmount.text))
                                {
                                    decimal BaseAmount;
                                    decimal.TryParse(Invoice.AllowanceCharge.BaseAmount.text, out BaseAmount);
                                    decimal chargeAmount = (BaseAmount * MultiplierFactorNumeric) / 100;

                                    if (chargeAmount != Amount)
                                    {
                                        _AllowanceChargevalidation.Add(new ErrorModel
                                        {
                                            errMsg = "AllowanceCharge/Amount must be equal to BaseAmount * MultiplierFactorNumeric / 100",
                                            PropertyName = "AllowanceCharge/Amount",
                                            PropertyValue = chargeAmount.ToString()
                                        });
                                    }
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(Invoice.AllowanceCharge.Amount.currencyID))
                            {
                                _AllowanceChargevalidation.Add(new ValidatorExtensions().ValidateCurrencyCode(Invoice.AllowanceCharge.Amount.currencyID, "AllowanceCharge/Amount"));
                            }
                        }
                        if (Invoice.AllowanceCharge.TaxCategory != null)
                        {
                            if (!string.IsNullOrWhiteSpace(Invoice.AllowanceCharge.TaxCategory.ID))
                            {
                                if (!TaxTypes.Contains(char.Parse(Invoice.AllowanceCharge.TaxCategory.ID)))
                                {
                                    _AllowanceChargevalidation.Add(new ErrorModel
                                    {
                                        errMsg = "VAT category code must contain one of the values (S, Z, E, O)",
                                        PropertyName = "AllowanceCharge/TaxCategory",
                                        PropertyValue = Invoice.AllowanceCharge.TaxCategory.ID
                                    });
                                }
                                if (Invoice.AllowanceCharge.TaxCategory.ID == "S")
                                {
                                    if (!string.IsNullOrWhiteSpace(Invoice.AllowanceCharge.TaxCategory.Percent))
                                    {
                                        decimal Percent;
                                        decimal.TryParse(Invoice.AllowanceCharge.TaxCategory.Percent, out Percent);
                                        if (Percent <= 0)
                                        {
                                            _AllowanceChargevalidation.Add(new ErrorModel
                                            {
                                                errMsg = "In a Document level allowance where the Document level allowance VAT category code is 'Standard rated'/'S' the Document level allowance VAT rate shall be greater than zero",
                                                PropertyName = "AllowanceCharge/TaxCategory/Percent",
                                                PropertyValue = Invoice.AllowanceCharge.TaxCategory.Percent
                                            });
                                        }
                                    }
                                    else
                                    {
                                        _AllowanceChargevalidation.Add(new ErrorModel
                                        {
                                            errMsg = "In a Document level allowance where the Document level allowance VAT category code is 'Standard rated'/'S' the Document level allowance VAT rate shall be greater than zero",
                                            PropertyName = "AllowanceCharge/TaxCategory/Percent",
                                            PropertyValue = ""
                                        });
                                    }
                                }
                                if (Invoice.AllowanceCharge.TaxCategory.ID == "Z")
                                {
                                    if (!string.IsNullOrWhiteSpace(Invoice.AllowanceCharge.TaxCategory.Percent))
                                    {
                                        decimal Percent;
                                        decimal.TryParse(Invoice.AllowanceCharge.TaxCategory.Percent, out Percent);

                                        if (Percent > 0)
                                        {
                                            _AllowanceChargevalidation.Add(new ErrorModel
                                            {
                                                errMsg = "In a Document level allowance where the Document level allowance VAT category code is 'Zero rated'/'Z' the Document level allowance VAT rate shall be 0 (zero).",
                                                PropertyName = "AllowanceCharge/TaxCategory/Percent",
                                                PropertyValue = Invoice.AllowanceCharge.TaxCategory.Percent
                                            });
                                        }
                                    }
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(Invoice.AllowanceCharge.TaxCategory.Percent))
                            {
                                if (Invoice.AllowanceCharge.TaxCategory.Percent.Contains("%"))
                                {
                                    _AllowanceChargevalidation.Add(new ErrorModel
                                    {
                                        errMsg = "Only numerals are accepted, the percentage symbol (%) is not allowed.",
                                        PropertyName = "AllowanceCharge/TaxCategory/Percent",
                                        PropertyValue = Invoice.AllowanceCharge.TaxCategory.Percent
                                    });
                                }
                                decimal Percent;
                                decimal.TryParse(Invoice.AllowanceCharge.TaxCategory.Percent, out Percent);
                                if (Percent > 0)
                                {
                                    if (Percent.ToString("0.00") != Invoice.AllowanceCharge.TaxCategory.Percent)
                                    {
                                        _AllowanceChargevalidation.Add(new ErrorModel
                                        {
                                            errMsg = "The VAT rates must be from 0.00 to 100.00, with maximum two decimals",
                                            PropertyName = "AllowanceCharge/TaxCategory/Percent",
                                            PropertyValue = Invoice.AllowanceCharge.TaxCategory.Percent
                                        });
                                    }
                                }
                            }
                        }
                    }
                    if (_AllowanceChargevalidation.Count > 0)
                    {
                        foreach (var item in _AllowanceChargevalidation)
                        {
                            if (!string.IsNullOrWhiteSpace(item.errMsg))
                                _ValidationErrors.Add(item);
                        }
                    }
                    #endregion
                    #region LegalMonetaryTotal
                    List<ErrorModel> _LegalMonetaryTotalvalidation = new List<ErrorModel>();
                    if (Invoice.LegalMonetaryTotal != null)
                    {
                        if (Invoice.LegalMonetaryTotal.LineExtensionAmount != null && !string.IsNullOrWhiteSpace(Invoice.LegalMonetaryTotal.LineExtensionAmount.text))
                        {
                            decimal LineExtensionAmount;
                            decimal.TryParse(Invoice.LegalMonetaryTotal.LineExtensionAmount.text, out LineExtensionAmount);
                            if (LineExtensionAmount.ToString("0.00") != Invoice.LegalMonetaryTotal.LineExtensionAmount.text)
                            {
                                _LegalMonetaryTotalvalidation.Add(new ErrorModel
                                {
                                    errMsg = "The allowed maximum number of decimals for the Sum of Invoice line net amount (LineExtensionAmount) is 2",
                                    PropertyName = "LegalMonetaryTotal/LineExtensionAmount",
                                    PropertyValue = Invoice.LegalMonetaryTotal.LineExtensionAmount.text
                                });
                            }
                            _AllowanceChargevalidation.Add(new ValidatorExtensions().ValidateDecimal(LineExtensionAmount, "LegalMonetaryTotal/LineExtensionAmount"));
                        }
                        if (Invoice.LegalMonetaryTotal.LineExtensionAmount != null && !string.IsNullOrWhiteSpace(Invoice.LegalMonetaryTotal.LineExtensionAmount.currencyID))
                        {
                            _LegalMonetaryTotalvalidation.Add(new ValidatorExtensions().ValidateCurrencyCode(Invoice.LegalMonetaryTotal.LineExtensionAmount.currencyID, "LegalMonetaryTotal/LineExtensionAmount"));
                        }
                        if (Invoice.LegalMonetaryTotal.AllowanceTotalAmount != null && !string.IsNullOrWhiteSpace(Invoice.LegalMonetaryTotal.AllowanceTotalAmount.text))
                        {
                            decimal LineExtensionAmount;
                            decimal.TryParse(Invoice.LegalMonetaryTotal.AllowanceTotalAmount.text, out LineExtensionAmount);
                            if (LineExtensionAmount.ToString("0.00") != Invoice.LegalMonetaryTotal.AllowanceTotalAmount.text)
                            {
                                _LegalMonetaryTotalvalidation.Add(new ErrorModel
                                {
                                    errMsg = "The allowed maximum number of decimals for the Sum of allowances on document level (AllowanceTotalAmount) is 2",
                                    PropertyName = "LegalMonetaryTotal/AllowanceTotalAmount",
                                    PropertyValue = Invoice.LegalMonetaryTotal.AllowanceTotalAmount.text
                                });
                            }
                            _AllowanceChargevalidation.Add(new ValidatorExtensions().ValidateDecimal(LineExtensionAmount, "LegalMonetaryTotal/AllowanceTotalAmount"));
                        }
                        if (Invoice.LegalMonetaryTotal.AllowanceTotalAmount != null && !string.IsNullOrWhiteSpace(Invoice.LegalMonetaryTotal.AllowanceTotalAmount.currencyID))
                        {
                            _LegalMonetaryTotalvalidation.Add(new ValidatorExtensions().ValidateCurrencyCode(Invoice.LegalMonetaryTotal.AllowanceTotalAmount.currencyID, "LegalMonetaryTotal/AllowanceTotalAmount"));
                        }
                        if (Invoice.LegalMonetaryTotal.TaxExclusiveAmount != null && !string.IsNullOrWhiteSpace(Invoice.LegalMonetaryTotal.TaxExclusiveAmount.text))
                        {
                            decimal LineExtensionAmount;
                            decimal.TryParse(Invoice.LegalMonetaryTotal.TaxExclusiveAmount.text, out LineExtensionAmount);
                            if (LineExtensionAmount.ToString("0.00") != Invoice.LegalMonetaryTotal.TaxExclusiveAmount.text)
                            {
                                _LegalMonetaryTotalvalidation.Add(new ErrorModel
                                {
                                    errMsg = "The allowed maximum number of decimals for the Invoice total amount without VAT is 2",
                                    PropertyName = "LegalMonetaryTotal/TaxExclusiveAmount",
                                    PropertyValue = Invoice.LegalMonetaryTotal.TaxExclusiveAmount.text
                                });
                            }
                            _AllowanceChargevalidation.Add(new ValidatorExtensions().ValidateDecimal(LineExtensionAmount, "LegalMonetaryTotal/TaxExclusiveAmount"));
                        }
                        if (Invoice.LegalMonetaryTotal.TaxExclusiveAmount != null && !string.IsNullOrWhiteSpace(Invoice.LegalMonetaryTotal.TaxExclusiveAmount.currencyID))
                        {
                            _LegalMonetaryTotalvalidation.Add(new ValidatorExtensions().ValidateCurrencyCode(Invoice.LegalMonetaryTotal.TaxExclusiveAmount.currencyID, "LegalMonetaryTotal/TaxExclusiveAmount"));
                        }
                        if (Invoice.LegalMonetaryTotal.TaxInclusiveAmount != null && !string.IsNullOrWhiteSpace(Invoice.LegalMonetaryTotal.TaxInclusiveAmount.text))
                        {
                            decimal LineExtensionAmount;
                            decimal.TryParse(Invoice.LegalMonetaryTotal.TaxInclusiveAmount.text, out LineExtensionAmount);
                            if (LineExtensionAmount.ToString("0.00") != Invoice.LegalMonetaryTotal.TaxInclusiveAmount.text)
                            {
                                _LegalMonetaryTotalvalidation.Add(new ErrorModel
                                {
                                    errMsg = "The allowed maximum number of decimals for the Invoice total amount with VAT is 2",
                                    PropertyName = "LegalMonetaryTotal/TaxInclusiveAmount",
                                    PropertyValue = Invoice.LegalMonetaryTotal.TaxInclusiveAmount.text
                                });
                            }
                            _AllowanceChargevalidation.Add(new ValidatorExtensions().ValidateDecimal(LineExtensionAmount, "LegalMonetaryTotal/TaxInclusiveAmount"));
                        }
                        if (Invoice.LegalMonetaryTotal.TaxInclusiveAmount != null && !string.IsNullOrWhiteSpace(Invoice.LegalMonetaryTotal.TaxInclusiveAmount.currencyID))
                        {
                            _LegalMonetaryTotalvalidation.Add(new ValidatorExtensions().ValidateCurrencyCode(Invoice.LegalMonetaryTotal.TaxInclusiveAmount.currencyID, "LegalMonetaryTotal/TaxInclusiveAmount"));
                        }
                        if (Invoice.LegalMonetaryTotal.PrepaidAmount != null && !string.IsNullOrWhiteSpace(Invoice.LegalMonetaryTotal.PrepaidAmount.text))
                        {
                            decimal LineExtensionAmount;
                            decimal.TryParse(Invoice.LegalMonetaryTotal.PrepaidAmount.text, out LineExtensionAmount);
                            if (LineExtensionAmount.ToString("0.00") != Invoice.LegalMonetaryTotal.PrepaidAmount.text)
                            {
                                _LegalMonetaryTotalvalidation.Add(new ErrorModel
                                {
                                    errMsg = "The allowed maximum number of decimals for the Pre-Paid amount is 2",
                                    PropertyName = "LegalMonetaryTotal/PrepaidAmount",
                                    PropertyValue = Invoice.LegalMonetaryTotal.PrepaidAmount.text
                                });
                            }
                            _AllowanceChargevalidation.Add(new ValidatorExtensions().ValidateDecimal(LineExtensionAmount, "LegalMonetaryTotal/PrepaidAmount"));
                        }
                        if (Invoice.LegalMonetaryTotal.PrepaidAmount != null && !string.IsNullOrWhiteSpace(Invoice.LegalMonetaryTotal.PrepaidAmount.currencyID))
                        {
                            _LegalMonetaryTotalvalidation.Add(new ValidatorExtensions().ValidateCurrencyCode(Invoice.LegalMonetaryTotal.PrepaidAmount.currencyID, "LegalMonetaryTotal/PrepaidAmount"));
                        }
                        if (Invoice.LegalMonetaryTotal.PayableAmount != null && !string.IsNullOrWhiteSpace(Invoice.LegalMonetaryTotal.PayableAmount.text))
                        {
                            decimal LineExtensionAmount;
                            decimal.TryParse(Invoice.LegalMonetaryTotal.PayableAmount.text, out LineExtensionAmount);
                            if (LineExtensionAmount.ToString("0.00") != Invoice.LegalMonetaryTotal.PayableAmount.text)
                            {
                                _LegalMonetaryTotalvalidation.Add(new ErrorModel
                                {
                                    errMsg = "The allowed maximum number of decimals for the Amount due for payment is 2",
                                    PropertyName = "LegalMonetaryTotal/PayableAmount",
                                    PropertyValue = Invoice.LegalMonetaryTotal.PayableAmount.text
                                });
                            }
                            _AllowanceChargevalidation.Add(new ValidatorExtensions().ValidateDecimal(LineExtensionAmount, "LegalMonetaryTotal/PayableAmount"));
                        }
                        if (Invoice.LegalMonetaryTotal.PayableAmount != null && !string.IsNullOrWhiteSpace(Invoice.LegalMonetaryTotal.PayableAmount.currencyID))
                        {
                            _LegalMonetaryTotalvalidation.Add(new ValidatorExtensions().ValidateCurrencyCode(Invoice.LegalMonetaryTotal.PayableAmount.currencyID, "LegalMonetaryTotal/PayableAmount"));
                        }
                    }
                    if (_LegalMonetaryTotalvalidation.Count > 0)
                    {
                        foreach (var item in _LegalMonetaryTotalvalidation)
                        {
                            if (!string.IsNullOrWhiteSpace(item.errMsg))
                                _ValidationErrors.Add(item);
                        }
                    }
                    #endregion
                    #region TaxTotal
                    if (Invoice.TaxTotal != null)
                    {
                        List<ErrorModel> _TaxTotalvalidation = new List<ErrorModel>();
                        if (Invoice.TaxTotal != null && Invoice.TaxTotal.Count() > 0)
                        {
                            var item = Invoice.TaxTotal[0];
                            if (item.TaxAmount != null && !string.IsNullOrWhiteSpace(item.TaxAmount.text))
                            {
                                decimal LineExtensionAmount;
                                decimal.TryParse(item.TaxAmount.text, out LineExtensionAmount);
                                if (LineExtensionAmount.ToString("0.00") != item.TaxAmount.text)
                                {
                                    _TaxTotalvalidation.Add(new ErrorModel
                                    {
                                        errMsg = "The allowed maximum number of decimals for the Invoice total VAT amount is 2",
                                        PropertyName = "TaxTotal/TaxAmount",
                                        PropertyValue = item.TaxAmount.text
                                    });
                                }
                                _AllowanceChargevalidation.Add(new ValidatorExtensions().ValidateDecimal(LineExtensionAmount, "TaxTotal/TaxAmount"));
                            }
                            if (item.TaxAmount != null && !string.IsNullOrWhiteSpace(item.TaxAmount.currencyID))
                            {
                                _TaxTotalvalidation.Add(new ValidatorExtensions().ValidateCurrencyCode(item.TaxAmount.currencyID, "TaxTotal/TaxAmount"));
                            }
                            var _TaxSubtotalMulti = new ValidatorExtensions().GetMultiElements<TaxsubtotalMulti>(xmlString, "TaxSubtotal", "TaxTotal", true, ref errorMsg);
                            if (_TaxSubtotalMulti != null && _TaxSubtotalMulti.Count > 0)
                            {
                                foreach (var TaxSubtotalMulti in _TaxSubtotalMulti)
                                {
                                    if (TaxSubtotalMulti.TaxSubtotal != null)
                                    {
                                        if (TaxSubtotalMulti.TaxSubtotal.TaxableAmount != null && !string.IsNullOrWhiteSpace(TaxSubtotalMulti.TaxSubtotal.TaxableAmount.text))
                                        {
                                            decimal LineExtensionAmount;
                                            decimal.TryParse(TaxSubtotalMulti.TaxSubtotal.TaxableAmount.text, out LineExtensionAmount);
                                            if (LineExtensionAmount.ToString("0.00") != TaxSubtotalMulti.TaxSubtotal.TaxableAmount.text)
                                            {
                                                _TaxTotalvalidation.Add(new ErrorModel
                                                {
                                                    errMsg = "The allowed maximum number of decimals for the VAT category taxable amount is 2.",
                                                    PropertyName = "TaxTotal/TaxSubtotal/TaxableAmount",
                                                    PropertyValue = TaxSubtotalMulti.TaxSubtotal.TaxableAmount.text
                                                });
                                            }
                                            _AllowanceChargevalidation.Add(new ValidatorExtensions().ValidateDecimal(LineExtensionAmount, "TaxTotal/TaxSubtotal/TaxableAmount"));
                                        }
                                        if (TaxSubtotalMulti.TaxSubtotal.TaxableAmount != null && !string.IsNullOrWhiteSpace(TaxSubtotalMulti.TaxSubtotal.TaxableAmount.currencyID))
                                        {
                                            _TaxTotalvalidation.Add(new ValidatorExtensions().ValidateCurrencyCode(TaxSubtotalMulti.TaxSubtotal.TaxableAmount.currencyID, "TaxTotal/TaxSubtotal/TaxableAmount"));
                                        }
                                        if (TaxSubtotalMulti.TaxSubtotal.TaxAmount != null && !string.IsNullOrWhiteSpace(TaxSubtotalMulti.TaxSubtotal.TaxAmount.text))
                                        {
                                            decimal LineExtensionAmount;
                                            decimal.TryParse(TaxSubtotalMulti.TaxSubtotal.TaxAmount.text, out LineExtensionAmount);
                                            if (LineExtensionAmount.ToString("0.00") != TaxSubtotalMulti.TaxSubtotal.TaxAmount.text)
                                            {
                                                _TaxTotalvalidation.Add(new ErrorModel
                                                {
                                                    errMsg = "The allowed maximum number of decimals for the VAT category tax amount is 2.",
                                                    PropertyName = "TaxTotal/TaxSubtotal/TaxAmount",
                                                    PropertyValue = TaxSubtotalMulti.TaxSubtotal.TaxAmount.text
                                                });
                                            }
                                            _AllowanceChargevalidation.Add(new ValidatorExtensions().ValidateDecimal(LineExtensionAmount, "TaxTotal/TaxSubtotal/TaxAmount"));
                                        }
                                        if (TaxSubtotalMulti.TaxSubtotal.TaxAmount != null && !string.IsNullOrWhiteSpace(TaxSubtotalMulti.TaxSubtotal.TaxAmount.currencyID))
                                        {
                                            _TaxTotalvalidation.Add(new ValidatorExtensions().ValidateCurrencyCode(TaxSubtotalMulti.TaxSubtotal.TaxAmount.currencyID, "TaxTotal/TaxSubtotal/TaxAmount"));
                                        }
                                        if (TaxSubtotalMulti.TaxSubtotal.TaxCategory != null)
                                        {
                                            if (!string.IsNullOrWhiteSpace(TaxSubtotalMulti.TaxSubtotal.TaxCategory.ID))
                                            {
                                                if (_TaxSubtotalMulti.Where(x => TaxTypes.Contains(char.Parse(x.TaxSubtotal.TaxCategory.ID))).ToList().Count <= 0)
                                                {
                                                    _TaxTotalvalidation.Add(new ErrorModel
                                                    {
                                                        errMsg = "VAT category code must contain one of the values (S, Z, E, O)",
                                                        PropertyName = "TaxTotal/TaxSubtotal/TaxCategory",
                                                        PropertyValue = ""
                                                    });
                                                }

                                                if (TaxSubtotalMulti.TaxSubtotal.TaxCategory.ID == "Z" || TaxSubtotalMulti.TaxSubtotal.TaxCategory.ID == "E" || TaxSubtotalMulti.TaxSubtotal.TaxCategory.ID == "O")
                                                {
                                                    if (string.IsNullOrWhiteSpace(TaxSubtotalMulti.TaxSubtotal.TaxCategory.TaxExemptionReasonCode))
                                                    {
                                                        _TaxTotalvalidation.Add(new ErrorModel
                                                        {
                                                            errMsg = "If VAT category Code is 'Z', or 'E' or ‘O’, VAT exemption (or exception) reason code must exist with specific to Saudi Arabia.",
                                                            PropertyName = "TaxTotal/TaxSubtotal/TaxCategory/TaxExemptionReasonCode",
                                                            PropertyValue = ""
                                                        });
                                                    }
                                                    string TaxId = string.Empty;
                                                    if (TaxSubtotalMulti.TaxSubtotal.TaxCategory.ID == "Z")
                                                    {
                                                        TaxId = "'Zero rated'";
                                                    }
                                                    if (TaxSubtotalMulti.TaxSubtotal.TaxCategory.ID == "E")
                                                    {
                                                        TaxId = "'Exempt from VAT'";
                                                    }
                                                    if (TaxSubtotalMulti.TaxSubtotal.TaxCategory.ID == "O")
                                                    {
                                                        TaxId = "'Not subject to VAT'";
                                                    }
                                                    if (TaxSubtotalMulti.TaxSubtotal.TaxAmount != null && !string.IsNullOrWhiteSpace(TaxSubtotalMulti.TaxSubtotal.TaxAmount.text))
                                                    {
                                                        decimal LineExtensionAmount;
                                                        decimal.TryParse(TaxSubtotalMulti.TaxSubtotal.TaxAmount.text, out LineExtensionAmount);
                                                        if (LineExtensionAmount > 0)
                                                        {
                                                            _TaxTotalvalidation.Add(new ErrorModel
                                                            {
                                                                errMsg = "The VAT category tax amount in a VAT breakdown where VAT category code is " + TaxId + " shall equal 0 (zero)",
                                                                PropertyName = "TaxTotal/TaxSubtotal/TaxAmount",
                                                                PropertyValue = TaxSubtotalMulti.TaxSubtotal.TaxAmount.text
                                                            });
                                                        }
                                                    }
                                                }
                                            }
                                            decimal Percent = 0.00M;
                                            if (!string.IsNullOrWhiteSpace(TaxSubtotalMulti.TaxSubtotal.TaxCategory.Percent))
                                            {
                                                if (TaxSubtotalMulti.TaxSubtotal.TaxCategory.Percent.Contains("%"))
                                                {
                                                    _TaxTotalvalidation.Add(new ErrorModel
                                                    {
                                                        errMsg = "Only numerals are accepted, the percentage symbol (%) is not allowed",
                                                        PropertyName = "TaxTotal/TaxSubtotal/TaxCategory/Percent",
                                                        PropertyValue = TaxSubtotalMulti.TaxSubtotal.TaxCategory.Percent
                                                    });
                                                }
                                                if (decimal.TryParse(TaxSubtotalMulti.TaxSubtotal.TaxCategory.Percent, out Percent))
                                                {
                                                    if (TaxSubtotalMulti.TaxSubtotal.TaxCategory.Percent != Percent.ToString("0.00") || Percent < 0 || Percent > 100)
                                                    {
                                                        _TaxTotalvalidation.Add(new ErrorModel
                                                        {
                                                            errMsg = "The VAT rates values must be from 0.00 to 100.00, with maximum two decimal places.",
                                                            PropertyName = "TaxTotal/TaxSubtotal/TaxCategory/Percent",
                                                            PropertyValue = TaxSubtotalMulti.TaxSubtotal.TaxCategory.Percent
                                                        });
                                                    }
                                                }
                                            }
                                            if (TaxSubtotalMulti.TaxSubtotal.TaxCategory.TaxScheme != null && !string.IsNullOrWhiteSpace(TaxSubtotalMulti.TaxSubtotal.TaxCategory.TaxScheme.ID))
                                            {
                                                if (TaxSubtotalMulti.TaxSubtotal.TaxCategory.TaxScheme.ID != "VAT")
                                                {

                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (_TaxTotalvalidation.Count > 0)
                        {
                            foreach (var item in _TaxTotalvalidation)
                            {
                                if (!string.IsNullOrWhiteSpace(item.errMsg))
                                    _ValidationErrors.Add(item);
                            }
                        }
                    }
                    #endregion
                    #region InvoiceLine
                    var _Invoiceline = new ValidatorExtensions().GetMultiElements<InvoiceLineMulti>(xmlString, "InvoiceLine", true, ref errorMsg);
                    List<ErrorModel> _InvoiceLinevalidation = new List<ErrorModel>();
                    if (_Invoiceline != null && _Invoiceline.Count > 0)
                    {
                        foreach (var InvItem in _Invoiceline)
                        {
                            if (InvItem != null && InvItem.InvoiceLine != null)
                            {
                                var item = InvItem.InvoiceLine;
                                if (item.LineExtensionAmount != null && !string.IsNullOrWhiteSpace(item.LineExtensionAmount.text))
                                {
                                    decimal LineExtensionAmount;
                                    decimal.TryParse(item.LineExtensionAmount.text, out LineExtensionAmount);
                                    if (LineExtensionAmount.ToString("0.00") != item.LineExtensionAmount.text)
                                    {
                                        _InvoiceLinevalidation.Add(new ErrorModel
                                        {
                                            errMsg = "The allowed maximum number of decimals for the Invoice line net amount is 2.",
                                            PropertyName = item.ID + " - InvoiceLine/LineExtensionAmount",
                                            PropertyValue = item.LineExtensionAmount.text
                                        });
                                    }
                                    _AllowanceChargevalidation.Add(new ValidatorExtensions().ValidateDecimal(LineExtensionAmount, item.ID + " - InvoiceLine/LineExtensionAmount"));
                                }
                                if (item.LineExtensionAmount != null && !string.IsNullOrWhiteSpace(item.LineExtensionAmount.currencyID))
                                {
                                    _InvoiceLinevalidation.Add(new ValidatorExtensions().ValidateCurrencyCode(item.LineExtensionAmount.currencyID, item.ID + " - InvoiceLine/LineExtensionAmount"));
                                }
                                if(item.InvoicedQuantity != null && !string.IsNullOrWhiteSpace(item.InvoicedQuantity.text))
                                {
                                    decimal Quantity;
                                    if (decimal.TryParse(item.InvoicedQuantity.text,out Quantity))
                                    {
                                        if (Quantity.ToString("0.00") != item.InvoicedQuantity.text)
                                        {
                                            _InvoiceLinevalidation.Add(new ErrorModel
                                            {
                                                errMsg = "The allowed maximum number of decimals for the InvoiceLine level InvoicedQuantity is 2",
                                                PropertyName = "InvoiceLine/InvoicedQuantity",
                                                PropertyValue = item.InvoicedQuantity.text
                                            });
                                        }
                                        _InvoiceLinevalidation.Add(new ValidatorExtensions().ValidateDecimal(Quantity, "InvoiceLine/InvoicedQuantity"));
                                    }
                                }
                                if (item.AllowanceCharge != null)
                                {
                                    if (!string.IsNullOrWhiteSpace(item.AllowanceCharge.ChargeIndicator) && !bool.TryParse(item.AllowanceCharge.ChargeIndicator, out _))
                                    {
                                        _InvoiceLinevalidation.Add(new ErrorModel
                                        {
                                            errMsg = "AllowanceCharge/ChargeIndicator value MUST equal to 'false'/'True' respectively",
                                            PropertyName = item.ID + " - InvoiceLine/AllowanceCharge/ChargeIndicator",
                                            PropertyValue = item.AllowanceCharge.ChargeIndicator
                                        });
                                    }
                                    if (!string.IsNullOrWhiteSpace(item.AllowanceCharge.MultiplierFactorNumeric))
                                    {
                                        if (item.AllowanceCharge.MultiplierFactorNumeric.Contains("%"))
                                        {
                                            _InvoiceLinevalidation.Add(new ErrorModel
                                            {
                                                errMsg = "Only numerals are accepted, the percentage symbol (%) is not allowed",
                                                PropertyName = item.ID + " - InvoiceLine/AllowanceCharge/MultiplierFactorNumeric",
                                                PropertyValue = item.AllowanceCharge.MultiplierFactorNumeric
                                            });
                                        }
                                        decimal MultiplierFactorNumeric;
                                        if (decimal.TryParse(item.AllowanceCharge.MultiplierFactorNumeric, out MultiplierFactorNumeric))
                                        {
                                            if (item.AllowanceCharge.MultiplierFactorNumeric != MultiplierFactorNumeric.ToString("0.00") || MultiplierFactorNumeric < 0 || MultiplierFactorNumeric > 100)
                                            {
                                                _InvoiceLinevalidation.Add(new ErrorModel
                                                {
                                                    errMsg = "The item allowance percentage values must be from 0.00 to 100.00, with maximum two decimal places.",
                                                    PropertyName = item.ID + " - InvoiceLine/AllowanceCharge/MultiplierFactorNumeric",
                                                    PropertyValue = item.AllowanceCharge.MultiplierFactorNumeric
                                                });
                                            }
                                        }
                                        if (item.AllowanceCharge.BaseAmount == null || string.IsNullOrWhiteSpace(item.AllowanceCharge.BaseAmount.text))
                                        {
                                            _InvoiceLinevalidation.Add(new ErrorModel
                                            {
                                                errMsg = item.ID + " - InvoiceLine/AllowanceCharge/BaseAmount must be provided when InvoiceLine/AllowanceCharge/MultiplierFactorNumeric is provided",
                                                PropertyName = item.ID + " - InvoiceLine/AllowanceCharge/BaseAmount",
                                                PropertyValue = ""
                                            });
                                        }
                                    }

                                    if (item.AllowanceCharge.Amount != null)
                                    {
                                        if (!string.IsNullOrWhiteSpace(item.AllowanceCharge.Amount.text))
                                        {
                                            decimal Amount;
                                            if (decimal.TryParse(item.AllowanceCharge.Amount.text, out Amount))
                                            {
                                                if (Amount.ToString("0.00") != item.AllowanceCharge.Amount.text)
                                                {
                                                    _InvoiceLinevalidation.Add(new ErrorModel
                                                    {
                                                        errMsg = "The allowed maximum number of decimals for the InvoiceLine allowance amount is 2",
                                                        PropertyName = item.ID + " - InvoiceLine/AllowanceCharge/Amount",
                                                        PropertyValue = item.AllowanceCharge.Amount.text
                                                    });
                                                }
                                                _AllowanceChargevalidation.Add(new ValidatorExtensions().ValidateDecimal(Amount, item.ID + " - InvoiceLine/AllowanceCharge/Amount"));
                                            }
                                            if (!string.IsNullOrWhiteSpace(item.AllowanceCharge.MultiplierFactorNumeric) && item.AllowanceCharge.BaseAmount != null && !string.IsNullOrWhiteSpace(item.AllowanceCharge.BaseAmount.text))
                                            {
                                                decimal BaseAmount;
                                                decimal MultiplierFactorNumeric;
                                                decimal.TryParse(Invoice.AllowanceCharge.MultiplierFactorNumeric, out MultiplierFactorNumeric);
                                                decimal.TryParse(Invoice.AllowanceCharge.BaseAmount.text, out BaseAmount);
                                                decimal chargeAmount = (BaseAmount * MultiplierFactorNumeric) / 100;

                                                if (chargeAmount != Amount)
                                                {
                                                    _InvoiceLinevalidation.Add(new ErrorModel
                                                    {
                                                        errMsg = item.ID + " - InvoiceLine/AllowanceCharge/Amount must be equal to BaseAmount * MultiplierFactorNumeric / 100",
                                                        PropertyName = item.ID + " - InvoiceLine/AllowanceCharge/Amount",
                                                        PropertyValue = chargeAmount.ToString()
                                                    });
                                                }
                                            }
                                        }
                                        if (!string.IsNullOrWhiteSpace(item.AllowanceCharge.Amount.currencyID))
                                        {
                                            _InvoiceLinevalidation.Add(new ValidatorExtensions().ValidateCurrencyCode(item.AllowanceCharge.Amount.currencyID, item.ID + " - InvoiceLine/AllowanceCharge/Amount"));
                                        }
                                    }
                                }
                                if (item.TaxTotal != null)
                                {
                                    if (item.TaxTotal.TaxAmount != null)
                                    {
                                        if (!string.IsNullOrWhiteSpace(item.TaxTotal.TaxAmount.text))
                                        {
                                            decimal Amount;
                                            if (decimal.TryParse(item.TaxTotal.TaxAmount.text, out Amount))
                                            {
                                                if (Amount.ToString("0.00") != item.TaxTotal.TaxAmount.text)
                                                {
                                                    _InvoiceLinevalidation.Add(new ErrorModel
                                                    {
                                                        errMsg = "The allowed maximum number of decimals for the InvoiceLine TaxAmount amount is 2",
                                                        PropertyName = item.ID + " - InvoiceLine/TaxTotal/TaxAmount",
                                                        PropertyValue = item.TaxTotal.TaxAmount.text
                                                    });
                                                }
                                                _AllowanceChargevalidation.Add(new ValidatorExtensions().ValidateDecimal(Amount, item.ID + " - InvoiceLine/TaxTotal/TaxAmount"));
                                            }
                                        }
                                        if (!string.IsNullOrWhiteSpace(item.TaxTotal.TaxAmount.currencyID))
                                        {
                                            _InvoiceLinevalidation.Add(new ValidatorExtensions().ValidateCurrencyCode(item.TaxTotal.TaxAmount.currencyID, item.ID + " - InvoiceLine/TaxTotal/TaxAmount"));
                                        }
                                    }
                                    if (item.TaxTotal.RoundingAmount != null)
                                    {
                                        if (Invoice.InvoiceTypeCode.name.Substring(0, 2) == "01")
                                        {
                                            if (string.IsNullOrWhiteSpace(item.TaxTotal.RoundingAmount.text))
                                            {
                                                _InvoiceLinevalidation.Add(new ErrorModel
                                                {
                                                    errMsg = "The line amount with VAT is mandatory for tax invoice (01) and associated credit notes and debit notes.",
                                                    PropertyName = item.ID + " - InvoiceLine/TaxTotal/RoundingAmount",
                                                    PropertyValue = item.TaxTotal.RoundingAmount.text
                                                });
                                            }
                                        }
                                        if (!string.IsNullOrWhiteSpace(item.TaxTotal.RoundingAmount.text))
                                        {
                                            decimal Amount;
                                            if (decimal.TryParse(item.TaxTotal.RoundingAmount.text, out Amount))
                                            {
                                                if (Amount.ToString("0.00") != item.TaxTotal.RoundingAmount.text)
                                                {
                                                    _InvoiceLinevalidation.Add(new ErrorModel
                                                    {
                                                        errMsg = "The allowed maximum number of decimals for the InvoiceLine RoundingAmount amount is 2",
                                                        PropertyName = item.ID + " - InvoiceLine/TaxTotal/RoundingAmount",
                                                        PropertyValue = item.TaxTotal.RoundingAmount.text
                                                    });
                                                }
                                                _AllowanceChargevalidation.Add(new ValidatorExtensions().ValidateDecimal(Amount, item.ID + " - InvoiceLine/TaxTotal/RoundingAmount"));
                                            }
                                        }
                                        if (!string.IsNullOrWhiteSpace(item.TaxTotal.RoundingAmount.currencyID))
                                        {
                                            _InvoiceLinevalidation.Add(new ValidatorExtensions().ValidateCurrencyCode(item.TaxTotal.RoundingAmount.currencyID, item.ID + " - InvoiceLine/TaxTotal/RoundingAmount"));
                                        }
                                    }
                                }
                                if (item.Price != null && item.Price.PriceAmount != null)
                                {
                                    if (!string.IsNullOrWhiteSpace(item.Price.PriceAmount.text))
                                    {
                                        decimal Amount;
                                        if (decimal.TryParse(item.Price.PriceAmount.text, out Amount))
                                        {
                                            _AllowanceChargevalidation.Add(new ValidatorExtensions().ValidateDecimal(Amount, item.ID + " - InvoiceLine/Price/PriceAmount"));
                                        }
                                    }
                                    if (!string.IsNullOrWhiteSpace(item.Price.PriceAmount.currencyID))
                                    {
                                        _InvoiceLinevalidation.Add(new ValidatorExtensions().ValidateCurrencyCode(item.Price.PriceAmount.currencyID, item.ID + " - InvoiceLine/Price/PriceAmount"));
                                    }
                                    if (item.Price.AllowanceCharge != null)
                                    {
                                        if (!string.IsNullOrWhiteSpace(item.Price.AllowanceCharge.ChargeIndicator))
                                        {
                                            if (item.Price.AllowanceCharge.ChargeIndicator != "true")
                                            {
                                                _InvoiceLinevalidation.Add(new ErrorModel
                                                {
                                                    errMsg = "Charge on price level is allowed.The value of Indicator should be 'True'",
                                                    PropertyName = item.ID + " - InvoiceLine/Item/Price/AllowanceCharge/ChargeIndicator",
                                                    PropertyValue = item.Price.AllowanceCharge.ChargeIndicator
                                                });
                                            }
                                        }
                                        if (item.Price.AllowanceCharge.Amount != null && !string.IsNullOrWhiteSpace(item.Price.AllowanceCharge.Amount.text))
                                        {
                                            decimal Amount;
                                            if (decimal.TryParse(item.Price.AllowanceCharge.Amount.text, out Amount))
                                            {
                                                if (Amount.ToString("0.00") != item.Price.AllowanceCharge.Amount.text)
                                                {
                                                    _InvoiceLinevalidation.Add(new ErrorModel
                                                    {
                                                        errMsg = "The allowed maximum number of decimals for the InvoiceLine Price level allowance amount is 2",
                                                        PropertyName = item.ID + " - InvoiceLine/Item/Price/AllowanceCharge/Amount",
                                                        PropertyValue = item.Price.AllowanceCharge.Amount.text
                                                    });
                                                }
                                                _AllowanceChargevalidation.Add(new ValidatorExtensions().ValidateDecimal(Amount, item.ID + " - InvoiceLine/Item/Price/AllowanceCharge/Amount"));
                                            }
                                        }
                                        if (item.Price.AllowanceCharge.Amount != null && !string.IsNullOrWhiteSpace(item.Price.AllowanceCharge.Amount.currencyID))
                                        {
                                            _InvoiceLinevalidation.Add(new ValidatorExtensions().ValidateCurrencyCode(item.Price.AllowanceCharge.Amount.currencyID, item.ID + " - InvoiceLine/Price/AllowanceCharge/Amount"));
                                        }
                                    }
                                    if (item.Price.BaseQuantity != null && !string.IsNullOrWhiteSpace(item.Price.BaseQuantity.text))
                                    {
                                        decimal Quantity;
                                        if (decimal.TryParse(item.Price.BaseQuantity.text, out Quantity))
                                        {
                                            if (Quantity.ToString("0.00") != item.Price.BaseQuantity.text)
                                            {
                                                _InvoiceLinevalidation.Add(new ErrorModel
                                                {
                                                    errMsg = "The allowed maximum number of decimals for the InvoiceLine level Price BaseQuantity is 2",
                                                    PropertyName = "InvoiceLine/Price/BaseQuantity",
                                                    PropertyValue = item.Price.BaseQuantity.text
                                                });
                                            }
                                            _InvoiceLinevalidation.Add(new ValidatorExtensions().ValidateDecimal(Quantity, "InvoiceLine/Price/BaseQuantity"));
                                        }
                                    }
                                }
                                if (item.Item != null && item.Item.ClassifiedTaxCategory != null)
                                {
                                    if (!string.IsNullOrWhiteSpace(item.Item.ClassifiedTaxCategory.ID))
                                    {
                                        if (!TaxTypes.Contains(char.Parse(item.Item.ClassifiedTaxCategory.ID)))
                                        {
                                            _InvoiceLinevalidation.Add(new ErrorModel
                                            {
                                                errMsg = "VAT category code must contain one of the values (S, Z, E, O)",
                                                PropertyName = item.ID + " - InvoiceLine/Item/ClassifiedTaxCategory",
                                                PropertyValue = item.Item.ClassifiedTaxCategory.ID
                                            });
                                        }
                                    }
                                    if (!string.IsNullOrWhiteSpace(item.Item.ClassifiedTaxCategory.Percent))
                                    {
                                        if (item.Item.ClassifiedTaxCategory.Percent.Contains("%"))
                                        {
                                            _InvoiceLinevalidation.Add(new ErrorModel
                                            {
                                                errMsg = "Only numerals are accepted, the percentage symbol (%) is not allowed",
                                                PropertyName = item.ID + " - InvoiceLine/Item/ClassifiedTaxCategory/Percent",
                                                PropertyValue = item.Item.ClassifiedTaxCategory.Percent
                                            });
                                        }
                                        decimal MultiplierFactorNumeric;
                                        if (decimal.TryParse(item.Item.ClassifiedTaxCategory.Percent, out MultiplierFactorNumeric))
                                        {
                                            if (item.Item.ClassifiedTaxCategory.Percent != MultiplierFactorNumeric.ToString("0.00") || MultiplierFactorNumeric < 0 || MultiplierFactorNumeric > 100)
                                            {
                                                _InvoiceLinevalidation.Add(new ErrorModel
                                                {
                                                    errMsg = "The item ClassifiedTaxCategory percentage values must be from 0.00 to 100.00, with maximum two decimal places.",
                                                    PropertyName = item.ID + " - InvoiceLine/Item/ClassifiedTaxCategory/Percent",
                                                    PropertyValue = item.Item.ClassifiedTaxCategory.Percent
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (_InvoiceLinevalidation.Count > 0)
                    {
                        foreach (var item in _InvoiceLinevalidation)
                        {
                            if (!string.IsNullOrWhiteSpace(item.errMsg))
                                _ValidationErrors.Add(item);
                        }
                    }
                    #endregion
                }
                return _ValidationErrors;
            }
            catch (Exception ex)
            {

                ErrorModel ErrorObj = new ErrorModel();
                ErrorObj.errMsg = "XML format error " + ex.Message;
                _ValidationErrors.Add(ErrorObj);
                return _ValidationErrors;
            }
        }
    }
}
