using FocusKSAValidatorBL.Models;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Macs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace FocusKSAValidatorBL.Extensions
{
    internal class ValidatorExtensions
    {
        public XNamespace cac = @"urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        public XNamespace cbc = @"urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        private static readonly HashSet<string> ValidDocumentTypeCodes = new HashSet<string>
            {
                "380", // Invoice
                "381", // Credit Note
                "388", // Tax invoice
                "383"  // Debit Note
            };
        private static readonly HashSet<string> ValidCurrencyCodes = new HashSet<string>
        {
            "AED", // United Arab Emirates Dirham
            "AFN", // Afghan Afghani
            "ALL", // Albanian Lek
            "AMD", // Armenian Dram
            "ANG", // Netherlands Antillean Guilder
            "AOA", // Angolan Kwanza
            "ARS", // Argentine Peso
            "AUD", // Australian Dollar
            "AWG", // Aruban Florin
            "AZN", // Azerbaijani Manat
            "BAM", // Bosnia-Herzegovina Convertible Mark
            "BBD", // Barbados Dollar
            "BDT", // Bangladeshi Taka
            "BGN", // Bulgarian Lev
            "BHD", // Bahraini Dinar
            "BIF", // Burundian Franc
            "BMD", // Bermudian Dollar
            "BND", // Brunei Dollar
            "BOB", // Bolivian Boliviano
            "BRL", // Brazilian Real
            "BSD", // Bahamian Dollar
            "BTN", // Bhutanese Ngultrum
            "BWP", // Botswana Pula
            "BYN", // Belarusian Ruble
            "BZD", // Belize Dollar
            "CAD", // Canadian Dollar
            "CDF", // Congolese Franc
            "CHF", // Swiss Franc
            "CLP", // Chilean Peso
            "CNY", // Chinese Yuan
            "COP", // Colombian Peso
            "CRC", // Costa Rican Colón
            "CUP", // Cuban Peso
            "CZK", // Czech Koruna
            "DJF", // Djiboutian Franc
            "DKK", // Danish Krone
            "DOP", // Dominican Peso
            "DZD", // Algerian Dinar
            "EGP", // Egyptian Pound
            "ERN", // Eritrean Nakfa
            "ETB", // Ethiopian Birr
            "EUR", // Euro
            "FJD", // Fijian Dollar
            "FKP", // Falkland Islands Pound
            "GBP", // British Pound Sterling
            "GEL", // Georgian Lari
            "GHS", // Ghanaian Cedi
            "GIP", // Gibraltar Pound
            "GMD", // Gambian Dalasi
            "GNF", // Guinean Franc
            "GTQ", // Guatemalan Quetzal
            "GYD", // Guyanaese Dollar
            "HKD", // Hong Kong Dollar
            "HNL", // Honduran Lempira
            "HRK", // Croatian Kuna
            "HTG", // Haitian Gourde
            "HUF", // Hungarian Forint
            "IDR", // Indonesian Rupiah
            "ILS", // Israeli New Sheqel
            "IMP", // Isle of Man Pound
            "INR", // Indian Rupee
            "IQD", // Iraqi Dinar
            "IRR", // Iranian Rial
            "ISK", // Icelandic Króna
            "JMD", // Jamaican Dollar
            "JOD", // Jordanian Dinar
            "JPY", // Japanese Yen
            "KES", // Kenyan Shilling
            "KGS", // Kyrgyzstani Som
            "KHR", // Cambodian Riel
            "KMF", // Comorian Franc
            "KPW", // North Korean Won
            "KRW", // South Korean Won
            "KWD", // Kuwaiti Dinar
            "KYD", // Cayman Islands Dollar
            "KZT", // Kazakhstani Tenge
            "LAK", // Laotian Kip
            "LBP", // Lebanese Pound
            "LKR", // Sri Lankan Rupee
            "LRD", // Liberian Dollar
            "LSL", // Lesotho Loti
            "LYD", // Libyan Dinar
            "MAD", // Moroccan Dirham
            "MDL", // Moldovan Leu
            "MGA", // Malagasy Ariary
            "MKD", // Macedonian Denar
            "MMK", // Myanma Kyat
            "MNT", // Mongolian Tögrög
            "MOP", // Macanese Pataca
            "MRU", // Mauritanian Ouguiya
            "MUR", // Mauritian Rupee
            "MVR", // Maldivian Rufiyaa
            "MWK", // Malawian Kwacha
            "MXN", // Mexican Peso
            "MYR", // Malaysian Ringgit
            "MZN", // Mozambican Metical
            "NAD", // Namibian Dollar
            "NGN", // Nigerian Naira
            "NIO", // Nicaraguan Córdoba
            "NOK", // Norwegian Krone
            "NPR", // Nepalese Rupee
            "NZD", // New Zealand Dollar
            "OMR", // Omani Rial
            "PAB", // Panamanian Balboa
            "PEN", // Peruvian Nuevo Sol
            "PGK", // Papua New Guinean Kina
            "PHP", // Philippine Peso
            "PKR", // Pakistani Rupee
            "PLN", // Polish Zloty
            "PYG", // Paraguayan Guarani
            "QAR", // Qatari Rial
            "RON", // Romanian Leu
            "RSD", // Serbian Dinar
            "RUB", // Russian Ruble
            "RWF", // Rwandan Franc
            "SAR", // Saudi Riyal
            "SBD", // Solomon Islands Dollar
            "SCR", // Seychellois Rupee
            "SDG", // Sudanese Pound
            "SEK", // Swedish Krona
            "SGD", // Singapore Dollar
            "SHP", // Saint Helena Pound
            "SLL", // Sierra Leonean Leone
            "SOS", // Somali Shilling
            "SRD", // Surinamese Dollar
            "SSP", // South Sudanese Pound
            "STD", // São Tomé and Príncipe Dobra
            "SYP", // Syrian Pound
            "SZL", // Swazi Lilangeni
            "THB", // Thai Baht
            "TJS", // Tajikistani Somoni
            "TMT", // Turkmenistani Manat
            "TND", // Tunisian Dinar
            "TOP", // Tongan Paʻanga
            "TRY", // Turkish Lira
            "TTD", // Trinidad and Tobago Dollar
            "TWD", // New Taiwan Dollar
            "TZS", // Tanzanian Shilling
            "UAH", // Ukrainian Hryvnia
            "UGX", // Ugandan Shilling
            "USD", // United States Dollar
            "UYU", // Uruguayan Peso
            "UZS", // Uzbekistani Som
            "VES", // Venezuelan Bolívar Soberano
            "VND", // Vietnamese Dong
            "VUV", // Vanuatu Vatu
            "WST", // Samoan Tala
            "XAF", // Central African CFA Franc
            "XAG", // Silver Ounce
            "XAU", // Gold Ounce
            "XCD", // East Caribbean Dollar
            "XOF", // West African CFA Franc
            "XPF", // CFP Franc
            "YER", // Yemeni Rial
            "ZAR", // South African Rand
            "ZMW", // Zambian Kwacha
            "ZWL"  // Zimbabwean Dollar
        };
        private static readonly Dictionary<string, string> Iso3166CountryCodes = new Dictionary<string, string>
        {
            { "AF", "Afghanistan" },
            { "AL", "Albania" },
            { "DZ", "Algeria" },
            { "AS", "American Samoa" },
            { "AD", "Andorra" },
            { "AO", "Angola" },
            { "AI", "Anguilla" },
            { "AQ", "Antarctica" },
            { "AG", "Antigua and Barbuda" },
            { "AR", "Argentina" },
            { "AM", "Armenia" },
            { "AW", "Aruba" },
            { "AU", "Australia" },
            { "AT", "Austria" },
            { "AZ", "Azerbaijan" },
            { "BS", "Bahamas" },
            { "BH", "Bahrain" },
            { "BD", "Bangladesh" },
            { "BB", "Barbados" },
            { "BY", "Belarus" },
            { "BE", "Belgium" },
            { "BZ", "Belize" },
            { "BJ", "Benin" },
            { "BM", "Bermuda" },
            { "BT", "Bhutan" },
            { "BO", "Bolivia" },
            { "BQ", "Bonaire, Sint Eustatius and Saba" },
            { "BA", "Bosnia and Herzegovina" },
            { "BW", "Botswana" },
            { "BR", "Brazil" },
            { "BN", "Brunei Darussalam" },
            { "BG", "Bulgaria" },
            { "BF", "Burkina Faso" },
            { "BI", "Burundi" },
            { "CV", "Cabo Verde" },
            { "KH", "Cambodia" },
            { "CM", "Cameroon" },
            { "CA", "Canada" },
            { "KY", "Cayman Islands" },
            { "CF", "Central African Republic" },
            { "TD", "Chad" },
            { "CL", "Chile" },
            { "CN", "China" },
            { "CO", "Colombia" },
            { "KM", "Comoros" },
            { "CD", "Congo, Democratic Republic of the" },
            { "CG", "Congo, Republic of the" },
            { "CK", "Cook Islands" },
            { "CR", "Costa Rica" },
            { "HR", "Croatia" },
            { "CU", "Cuba" },
            { "CW", "Curaçao" },
            { "CY", "Cyprus" },
            { "CZ", "Czechia" },
            { "DK", "Denmark" },
            { "DJ", "Djibouti" },
            { "DM", "Dominica" },
            { "DO", "Dominican Republic" },
            { "EC", "Ecuador" },
            { "EG", "Egypt" },
            { "SV", "El Salvador" },
            { "GQ", "Equatorial Guinea" },
            { "ER", "Eritrea" },
            { "EE", "Estonia" },
            { "SZ", "Eswatini" },
            { "ET", "Ethiopia" },
            { "FJ", "Fiji" },
            { "FI", "Finland" },
            { "FR", "France" },
            { "GA", "Gabon" },
            { "GM", "Gambia" },
            { "GE", "Georgia" },
            { "DE", "Germany" },
            { "GH", "Ghana" },
            { "GI", "Gibraltar" },
            { "GR", "Greece" },
            { "GL", "Greenland" },
            { "GT", "Guatemala" },
            { "GG", "Guernsey" },
            { "GN", "Guinea" },
            { "GW", "Guinea-Bissau" },
            { "GY", "Guyana" },
            { "HT", "Haiti" },
            { "HM", "Heard Island and McDonald Islands" },
            { "VA", "Holy See" },
            { "HN", "Honduras" },
            { "HK", "Hong Kong" },
            { "HU", "Hungary" },
            { "IS", "Iceland" },
            { "IN", "India" },
            { "ID", "Indonesia" },
            { "IR", "Iran" },
            { "IQ", "Iraq" },
            { "IE", "Ireland" },
            { "IM", "Isle of Man" },
            { "IL", "Israel" },
            { "IT", "Italy" },
            { "JM", "Jamaica" },
            { "JP", "Japan" },
            { "JE", "Jersey" },
            { "JO", "Jordan" },
            { "KZ", "Kazakhstan" },
            { "KE", "Kenya" },
            { "KI", "Kiribati" },
            { "KP", "Korea, Democratic People's Republic of" },
            { "KR", "Korea, Republic of" },
            { "KW", "Kuwait" },
            { "KG", "Kyrgyzstan" },
            { "LA", "Lao People's Democratic Republic" },
            { "LV", "Latvia" },
            { "LB", "Lebanon" },
            { "LS", "Lesotho" },
            { "LR", "Liberia" },
            { "LY", "Libya" },
            { "LI", "Liechtenstein" },
            { "LT", "Lithuania" },
            { "LU", "Luxembourg" },
            { "MO", "Macao" },
            { "MG", "Madagascar" },
            { "MW", "Malawi" },
            { "MY", "Malaysia" },
            { "MV", "Maldives" },
            { "ML", "Mali" },
            { "MT", "Malta" },
            { "MH", "Marshall Islands" },
            { "MR", "Mauritania" },
            { "MU", "Mauritius" },
            { "YT", "Mayotte" },
            { "MX", "Mexico" },
            { "FM", "Micronesia" },
            { "MD", "Moldova" },
            { "MC", "Monaco" },
            { "MN", "Mongolia" },
            { "ME", "Montenegro" },
            { "MS", "Montserrat" },
            { "MA", "Morocco" },
            { "MZ", "Mozambique" },
            { "MM", "Myanmar" },
            { "NA", "Namibia" },
            { "NR", "Nauru" },
            { "NP", "Nepal" },
            { "NL", "Netherlands" },
            { "NZ", "New Zealand" },
            { "NI", "Nicaragua" },
            { "NE", "Niger" },
            { "NG", "Nigeria" },
            { "NU", "Niue" },
            { "NF", "Norfolk Island" },
            { "MP", "Northern Mariana Islands" },
            { "NO", "Norway" },
            { "OM", "Oman" },
            { "PK", "Pakistan" },
            { "PW", "Palau" },
            { "PS", "Palestine, State of" },
            { "PA", "Panama" },
            { "PG", "Papua New Guinea" },
            { "PY", "Paraguay" },
            { "PE", "Peru" },
            { "PH", "Philippines" },
            { "PN", "Pitcairn" },
            { "PL", "Poland" },
            { "PT", "Portugal" },
            { "PR", "Puerto Rico" },
            { "QA", "Qatar" },
            { "RE", "Réunion" },
            { "RO", "Romania" },
            { "RU", "Russian Federation" },
            { "RW", "Rwanda" },
            { "BL", "Saint Barthélemy" },
            { "KN", "Saint Kitts and Nevis" },
            { "LC", "Saint Lucia" },
            { "MF", "Saint Martin" },
            { "WS", "Samoa" },
            { "SM", "San Marino" },
            { "ST", "Sao Tome and Principe" },
            { "SA", "Saudi Arabia" },
            { "SN", "Senegal" },
            { "RS", "Serbia" },
            { "SC", "Seychelles" },
            { "SL", "Sierra Leone" },
            { "SG", "Singapore" },
            { "SX", "Sint Maarten" },
            { "SK", "Slovakia" },
            { "SI", "Slovenia" },
            { "SB", "Solomon Islands" },
            { "SO", "Somalia" },
            { "ZA", "South Africa" },
            { "GS", "South Georgia and the South Sandwich Islands" },
            { "SS", "South Sudan" },
            { "ES", "Spain" },
            { "LK", "Sri Lanka" },
            { "SD", "Sudan" },
            { "SR", "Suriname" },
            { "SJ", "Svalbard and Jan Mayen" },
            { "CH", "Switzerland" },
            { "SY", "Syrian Arab Republic" },
            { "TW", "Taiwan" },
            { "TJ", "Tajikistan" },
            { "TZ", "Tanzania" },
            { "TH", "Thailand" },
            { "TL", "Timor-Leste" },
            { "TG", "Togo" },
            { "TK", "Tokelau" },
            { "TO", "Tonga" },
            { "TT", "Trinidad and Tobago" },
            { "TN", "Tunisia" },
            { "TR", "Turkey" },
            { "TM", "Turkmenistan" },
            { "TC", "Turks and Caicos Islands" },
            { "TV", "Tuvalu" },
            { "UG", "Uganda" },
            { "UA", "Ukraine" },
            { "AE", "United Arab Emirates" },
            { "GB", "United Kingdom" },
            { "US", "United States of America" },
            { "UY", "Uruguay" },
            { "UZ", "Uzbekistan" },
            { "VU", "Vanuatu" },
            { "VE", "Venezuela" },
            { "VN", "Vietnam" },
            { "WF", "Wallis and Futuna" },
            { "EH", "Western Sahara" },
            { "YE", "Yemen" },
            { "ZM", "Zambia" },
            { "ZW", "Zimbabwe" }
        };
        private const string ExpectedSignatureInfoId = "urn:oasis:names:specification:ubl:signature:1";
        private const string ExpectedReferencedSignatureId = "urn:oasis:names:specification:ubl:signature:Invoice";
        private const string ExpectedSignatureMethod = "urn:oasis:names:specification:ubl:dsig:enveloped:xades";
        public ErrorModel ValidateSpecialStrings(string propertyValue, string propertyName, string allowedChars)
        {
            var errorModel = new ErrorModel();
            var allowedCharSet = new HashSet<char>(allowedChars) { '\t', '\r', '\n', ' ' };
            if (!string.IsNullOrWhiteSpace(propertyValue))
            {
                foreach (var character in propertyValue)
                {
                    if (!allowedCharSet.Contains(character) && !char.IsLetterOrDigit(character))
                    {
                        errorModel.errMsg = string.IsNullOrWhiteSpace(allowedChars)
                            ? $"Special characters are not allowed in {propertyName}."
                            : $"Special characters are not allowed except for '{allowedChars}' in {propertyName}.";
                        errorModel.PropertyName = propertyName;
                        errorModel.PropertyValue = propertyValue;
                        break;
                    }
                }
            }
            return errorModel;
        }
        public ErrorModel ValidateDateProperty(string propertyValue, string propertyName)
        {
            var errorModel = new ErrorModel();

            if (!Regex.IsMatch(propertyValue, @"^\d{4}-\d{2}-\d{2}$"))
            {
                errorModel.errMsg = $"Invalid '{propertyName}' date format, should be YYYY-MM-DD";
                errorModel.PropertyName = propertyName;
                errorModel.PropertyValue = propertyValue;
                return errorModel;
            }
            var strArr = propertyValue.Split('-');
            if (!int.TryParse(strArr[1], out int month) || month < 1 || month > 12)
            {
                errorModel.errMsg = $"Invalid '{propertyName}' date format, should be YYYY-MM-DD";
                errorModel.PropertyName = propertyName;
                errorModel.PropertyValue = propertyValue;
                return errorModel;
            }
            if (!int.TryParse(strArr[2], out int day) || day < 1 || day > 31)
            {
                errorModel.errMsg = $"Invalid '{propertyName}' date format, should be YYYY-MM-DD";
                errorModel.PropertyName = propertyName;
                errorModel.PropertyValue = propertyValue;
                return errorModel;
            }
            DateTime isValid = DateTime.MinValue;
            if (!DateTime.TryParse(propertyValue, out isValid))
            {
                errorModel.errMsg = $"Invalid '{propertyName}' date format, should be YYYY-MM-DD";
                errorModel.PropertyName = propertyName;
                errorModel.PropertyValue = propertyValue;
            }
            else
            {
                //if(isValid > DateTime.Now)
                //{
                //    errorModel.errMsg = $"'{propertyName}' should not be greater than today";
                //    errorModel.PropertyName = propertyName;
                //    errorModel.PropertyValue = propertyValue;
                //}
            }

            return errorModel;
        }
        public ErrorModel ValidateTimeProperty(string propertyValue, string propertyName)
        {
            var errorModel = new ErrorModel();
            if (propertyValue.EndsWith("Z"))
            {
                propertyValue = propertyValue.TrimEnd('Z');
            }
            if (!Regex.IsMatch(propertyValue, @"^\d{2}:\d{2}:\d{2}$"))
            {
                errorModel.errMsg = $"Invalid format for '{propertyName}'. Expected format is hh:mm:ss(AST) or hh:mm:ssZ(UTC).";
                errorModel.PropertyName = propertyName;
                errorModel.PropertyValue = propertyValue;
                return errorModel;
            }
            if (!TimeSpan.TryParse(propertyValue, out _))
            {
                errorModel.errMsg = $"Invalid time value for '{propertyName}'.";
                errorModel.PropertyName = propertyName;
                errorModel.PropertyValue = propertyValue;
            }

            return errorModel;
        }
        public ErrorModel ValidateDocumentTypeCode(string documentTypeCode, string propertyName)
        {
            var errorModel = new ErrorModel();
            if (!ValidDocumentTypeCodes.Contains(documentTypeCode))
            {
                errorModel.errMsg = $"Invalid '{propertyName}'. The document type code must be one of the valid codes defined by UNTDID 1001.";
                errorModel.PropertyName = propertyName;
                errorModel.PropertyValue = documentTypeCode;
            }
            return errorModel;
        }
        public List<ErrorModel> ValidateTransactionCode(string transactionCode, string propertyName)
        {
            var errorModels = new List<ErrorModel>();
            if (transactionCode.Length != 7)
            {
                ErrorModel errorModel = new ErrorModel();
                errorModel.errMsg = $"Invalid '{propertyName}'. The transaction code must be exactly 7 characters long.";
                errorModel.PropertyName = propertyName;
                errorModel.PropertyValue = transactionCode;
                errorModels.Add(errorModel);
            }
            string subtype = transactionCode.Substring(0, 2);
            char thirdParty = transactionCode[2];
            char nominal = transactionCode[3];
            char export = transactionCode[4];
            char summary = transactionCode[5];
            char selfBilled = transactionCode[6];
            if (subtype != "01" && subtype != "02")
            {
                ErrorModel errorModel = new ErrorModel();
                errorModel.errMsg = $"Invalid '{propertyName}'. Position 1-2 must be '01' for tax invoice or '02' for simplified tax invoice.";
                errorModel.PropertyName = propertyName;
                errorModel.PropertyValue = transactionCode;
                errorModels.Add(errorModel);
            }
            if (!"01".Contains(thirdParty) || !("01".Contains(nominal) || nominal == '0') ||
                !("01".Contains(export) || export == '0') || !("01".Contains(summary) || summary == '0'))
            {
                ErrorModel errorModel = new ErrorModel();
                errorModel.errMsg = $"Invalid '{propertyName}'. Positions 3-6 must be '0' or '1'.";
                errorModel.PropertyName = propertyName;
                errorModel.PropertyValue = transactionCode;
                errorModels.Add(errorModel);
            }
            if (subtype == "02")
            {
                if (thirdParty != '1')
                {
                    ErrorModel errorModel = new ErrorModel();
                    errorModel.errMsg = $"Invalid '{propertyName}'. For simplified tax invoices, 3rd Party invoice transaction is mandatory.";
                    errorModel.PropertyName = propertyName;
                    errorModel.PropertyValue = transactionCode;
                    errorModels.Add(errorModel);
                }

                if (nominal != '1')
                {
                    ErrorModel errorModel = new ErrorModel();
                    errorModel.errMsg = $"Invalid '{propertyName}'. For simplified tax invoices, Nominal invoice transaction is mandatory.";
                    errorModel.PropertyName = propertyName;
                    errorModel.PropertyValue = transactionCode;
                    errorModels.Add(errorModel);
                }

                if (summary != '1')
                {
                    ErrorModel errorModel = new ErrorModel();
                    errorModel.errMsg = $"Invalid '{propertyName}'. For simplified tax invoices, Summary invoice transaction is mandatory'.";
                    errorModel.PropertyName = propertyName;
                    errorModel.PropertyValue = transactionCode;
                    errorModels.Add(errorModel);
                }
            }
            if (export == '1' && selfBilled == '1')
            {
                ErrorModel errorModel = new ErrorModel();
                errorModel.errMsg = $"Invalid '{propertyName}'. Self-billing is not allowed  for export invoices.";
                errorModel.PropertyName = propertyName;
                errorModel.PropertyValue = transactionCode;
                errorModels.Add(errorModel);
            }

            return errorModels;
        }
        public ErrorModel ValidateCurrencyCode(string currencyCode, string propertyName)
        {
            var errorModel = new ErrorModel();

            if (!ValidCurrencyCodes.Contains(currencyCode))
            {
                errorModel.errMsg = $"Invalid '{propertyName}' currency code MUST be coded using ISO code list 4217.";
                errorModel.PropertyName = propertyName;
                errorModel.PropertyValue = currencyCode;
            }

            return errorModel;
        }
        public ErrorModel ValidateOnlyNumeric(string invoiceCounter, string propertyName)
        {
            var errorModel = new ErrorModel();
            if (string.IsNullOrWhiteSpace(invoiceCounter) || !invoiceCounter.All(char.IsDigit))
            {
                errorModel.errMsg = $"Invalid '{propertyName}'. must contain only digits.";
                errorModel.PropertyName = propertyName;
                errorModel.PropertyValue = invoiceCounter;
            }
            return errorModel;
        }
        public ErrorModel ValidatePreviousInvoiceHash(string invoiceXml, string previousHash, string propertyName)
        {
            var errorModel = new ErrorModel();
            try
            {
                var xmlDoc = XDocument.Parse(invoiceXml);
                var canonicalXml = Canonicalize(xmlDoc.ToString());
                using (var sha256 = SHA256.Create())
                {
                    var bytes = Encoding.UTF8.GetBytes(canonicalXml);
                    var hashBytes = sha256.ComputeHash(bytes);
                    var base64Hash = Convert.ToBase64String(hashBytes);
                    previousHash = previousHash.Replace("\r\n","").Trim();
                    if (base64Hash != previousHash)
                    {
                        errorModel.errMsg = $"Invalid '{propertyName}'. The computed hash does not match the previous invoice hash.";
                        errorModel.PropertyName = propertyName;
                        errorModel.PropertyValue = previousHash;
                    }
                }
            }
            catch (Exception ex)
            {
                errorModel.errMsg = $"Error processing invoice: {ex.Message}";
                errorModel.PropertyName = propertyName;
                errorModel.PropertyValue = invoiceXml;
            }

            return errorModel;
        }
        public List<ErrorModel> ValidateCryptographicStamp(string invoiceXml, string propertyName)
        {
            var errorModels = new List<ErrorModel>();
            try
            {
                var xmlDoc = XDocument.Parse(invoiceXml);
                var signatureInfo = xmlDoc.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "SignatureInformation");

                if (signatureInfo != null)
                {
                    var signatureId = signatureInfo.Element(XName.Get("ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"));
                    if (signatureId?.Value != ExpectedSignatureInfoId)
                    {
                        var errorModel = new ErrorModel();
                        errorModel.errMsg = $"Invalid '{propertyName}'. Signature Information ID must be '{ExpectedSignatureInfoId}'.";
                        errorModel.PropertyName = propertyName;
                        errorModel.PropertyValue = signatureId?.Value;
                        errorModels.Add(errorModel);
                    }
                    var referencedId = signatureInfo.Element(XName.Get("ReferencedSignatureID", "urn:oasis:names:specification:ubl:schema:xsd:SignatureBasicComponents-2"));
                    if (referencedId?.Value != ExpectedReferencedSignatureId)
                    {
                        var errorModel = new ErrorModel();
                        errorModel.errMsg = $"Invalid '{propertyName}'. Referenced Signature ID must be '{ExpectedReferencedSignatureId}'.";
                        errorModel.PropertyName = propertyName;
                        errorModel.PropertyValue = referencedId?.Value;
                        errorModels.Add(errorModel);
                    }
                }
                var signature = xmlDoc.Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "Signature" && x.Name.NamespaceName == "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");

                if (signature != null)
                {
                    var signatureId = signature.Element(XName.Get("ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"));
                    if (signatureId?.Value != ExpectedReferencedSignatureId)
                    {
                        var errorModel = new ErrorModel();
                        errorModel.errMsg = $"Invalid '{propertyName}'. Signature ID must be '{ExpectedReferencedSignatureId}'.";
                        errorModel.PropertyName = propertyName;
                        errorModel.PropertyValue = signatureId?.Value;
                        errorModels.Add(errorModel);
                    }
                    var signatureMethod = signature.Element(XName.Get("SignatureMethod", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"));
                    if (signatureMethod?.Value != ExpectedSignatureMethod)
                    {
                        var errorModel = new ErrorModel();
                        errorModel.errMsg = $"Invalid '{propertyName}'. Signature Method must be '{ExpectedSignatureMethod}'.";
                        errorModel.PropertyName = propertyName;
                        errorModel.PropertyValue = signatureMethod?.Value;
                        errorModels.Add(errorModel);
                    }
                }
                else
                {
                    var errorModel = new ErrorModel();
                    errorModel.errMsg = $"Invalid '{propertyName}'. Signature element not found.";
                    errorModel.PropertyName = propertyName;
                    errorModel.PropertyValue = invoiceXml;
                    errorModels.Add(errorModel);
                }
            }
            catch (Exception ex)
            {
                var errorModel = new ErrorModel();
                errorModel.errMsg = $"Error processing invoice: {ex.Message}";
                errorModel.PropertyName = propertyName;
                errorModel.PropertyValue = invoiceXml;
                errorModels.Add(errorModel);
            }

            return errorModels; // Return the error model with any errors found
        }
        public ErrorModel ValidateCountry(string countryyCode, string propertyName)
        {
            var errorModel = new ErrorModel();

            if (Iso3166CountryCodes.Where(x=>x.Key == countryyCode).ToList().Count <= 0)
            {
                errorModel.errMsg = $"Invalid '{propertyName}' country code MUST be coded using ISO code list 3166-1.";
                errorModel.PropertyName = propertyName;
                errorModel.PropertyValue = countryyCode;
            }

            return errorModel;
        }
        public ErrorModel ValidateVatNo(string vatNo, string propertyName)
        {
            var errorModel = new ErrorModel();

            if (!Regex.IsMatch(vatNo, @"^3\d{13}3$"))
            {
                errorModel.errMsg = $"Invalid '{propertyName}' VAT registration number must contain 15 digits. The first and the last digits are “3”.";
                errorModel.PropertyName = propertyName;
                errorModel.PropertyValue = vatNo;
            }

            return errorModel;
        }
        public ErrorModel ValidateDecimal(decimal value, string propertyName)
        {
            var errorModel = new ErrorModel();
            if (value <= 0)
            {
                errorModel.errMsg = $"Invalid '{propertyName}': value must be positive.";
                errorModel.PropertyName = propertyName;
                errorModel.PropertyValue = value.ToString();
            }

            return errorModel;
        }
        public List<T> GetMultiElements<T>(string xmlString, string name, bool isCac, ref string error)
        {
            List<T> _MultiElements = new List<T>();
            XDocument xDoc = XDocument.Parse(xmlString); ;
            IEnumerable<XElement> MultiElements = isCac ? xDoc.Root.Elements(cac + name) : xDoc.Root.Elements(cbc + name);
            foreach (XElement MonoElement in MultiElements)
            {
                string jsontxt = JsonConvert.SerializeXNode(MonoElement);
                jsontxt = JsonHandler(jsontxt);
                try
                {
                    T item = JsonConvert.DeserializeObject<T>(jsontxt);
                    _MultiElements.Add(item);
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
            }
            return _MultiElements;
        }
        public List<T> GetMultiElements<T>(string xmlString, string name, string ParentName, bool isCac, ref string error)
        {
            List<T> _MultiElements = new List<T>();
            XDocument xDoc = XDocument.Parse(xmlString); ;
            IEnumerable<XElement> MultiElements = isCac ? xDoc.Root.Elements(cac + ParentName).Elements(cac + name) : xDoc.Root.Elements(cbc + ParentName).Elements(cbc + name);
            foreach (XElement MonoElement in MultiElements)
            {
                string jsontxt = JsonConvert.SerializeXNode(MonoElement);
                jsontxt = JsonHandler(jsontxt);
                try
                {
                    T item = JsonConvert.DeserializeObject<T>(jsontxt);
                    _MultiElements.Add(item);
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
            }
            return _MultiElements;
        }
        public List<T> GetMultiElements<T>(string xmlString, string name, string ParentName1, string ParentName2, bool isCac, ref string error)
        {
            List<T> _MultiElements = new List<T>();
            XDocument xDoc = XDocument.Parse(xmlString); ;
            IEnumerable<XElement> MultiElements = isCac ? xDoc.Root.Elements(cac + ParentName1).Elements(cac + ParentName2).Elements(cac + name) : xDoc.Root.Elements(cbc + ParentName1).Elements(cbc + ParentName2).Elements(cbc + name);
            foreach (XElement MonoElement in MultiElements)
            {
                string jsontxt = JsonConvert.SerializeXNode(MonoElement);
                jsontxt = JsonHandler(jsontxt);
                try
                {
                    T item = JsonConvert.DeserializeObject<T>(jsontxt);
                    _MultiElements.Add(item);
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
            }
            return _MultiElements;
        }
        public string Canonicalize(string xml)
        {
            var xmlDoc = XDocument.Parse(xml);

            var ublExtensions = xmlDoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "UBLExtensions");
            ublExtensions?.Remove();
            var additionalDocumentReference = xmlDoc.Descendants()
                .Where(x => x.Name.LocalName == "AdditionalDocumentReference")
                .FirstOrDefault(x => x.Element(XName.Get("ID", "cbc"))?.Value == "QR");
            additionalDocumentReference?.Remove();
            var signature = xmlDoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "Signature");
            signature?.Remove();

            return SerializeXml(xmlDoc);
        }
        private string SerializeXml(XDocument xmlDoc)
        {
            using (var stringWriter = new StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = false }))
                {
                    xmlDoc.Save(xmlWriter);
                    return stringWriter.ToString();
                }
            }
        }
        public string JsonHandler(string json)
        {
            return json.Replace("cbc:", "").
                Replace("cac:", "").
                Replace("@xmlns", "xmlns").
                Replace("@schemeAgencyName", "schemeAgencyName").
                Replace("@schemeAgencyID", "schemeAgencyID").
                Replace("@name", "name").
                Replace("@schemeID", "schemeID").
                Replace("@listID", "listID").
                Replace("@listAgencyID", "listAgencyID").
                Replace("@currencyID", "currencyID").
                Replace("@unitCode", "unitCode").
                Replace("@listVersionID", "listVersionID").
                Replace("@mimeCode", "mimeCode").
                Replace("@version", "version").
                Replace("@encoding", "encoding").
                Replace("?xml", "xml").
                Replace("#text", "text");
        }
    }
}
