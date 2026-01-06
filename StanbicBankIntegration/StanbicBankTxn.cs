using System;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.AR;

namespace StanbicBankIntegration
{
    [Serializable]
    [PXCacheName("Stanbic Bank Transaction")]
    public class StanbicBankTxn : PXBqlTable, IBqlTable
    {
        #region CompanyID
        // Suppress PX1027 to allow manual CompanyID declaration for debugging/persistence fixing.
#pragma warning disable PX1027
        [PXDBInt(IsKey = true)]
        [PXDefault(2)] // Hardcoded to 2 for your current debugging phase
        [PXUIField(DisplayName = "Company ID")]
        public virtual int? CompanyID { get; set; }
        public abstract class companyID : BqlInt.Field<companyID> { }
#pragma warning restore PX1027
        #endregion

        #region NoteID
        public abstract class noteID : BqlGuid.Field<noteID> { }
        [PXNote]
        public virtual Guid? NoteID { get; set; }
        #endregion

        #region CustomerID
        public abstract class customerID : BqlInt.Field<customerID> { }
        [Customer(DescriptionField = typeof(Customer.acctName), DisplayName = "Customer")]
        public virtual int? CustomerID { get; set; }
        #endregion

        #region InvoiceRefNbr
        public abstract class invoiceRefNbr : BqlString.Field<invoiceRefNbr> { }
        [PXDBString(15, IsUnicode = true)]
        [PXUIField(DisplayName = "Invoice Nbr.")]
        [ARInvoiceType.RefNbr(typeof(Search<ARInvoice.refNbr,
            Where<ARInvoice.customerID, Equal<Current<StanbicBankTxn.customerID>>,
            And<ARInvoice.docType, Equal<ARDocType.invoice>>>>))]
        public virtual string InvoiceRefNbr { get; set; }
        #endregion

        #region Status
        public abstract class status : BqlString.Field<status> { }
        [PXDBString(20, IsUnicode = true)]
        [PXUIField(DisplayName = "Status", Enabled = false)]
        [PXDefault("New")]
        [PXStringList(new string[] { "New", "Processed", "Error" },
                      new string[] { "New", "Processed", "Error" })]
        public virtual string Status { get; set; }
        #endregion

        #region TransID
        [PXDBString(50, IsKey = true, IsUnicode = true)]
        [PXUIField(DisplayName = "Transaction ID")]
        public virtual string TransID { get; set; }
        public abstract class transID : BqlString.Field<transID> { }
        #endregion

        #region TransactionType
        [PXDBString(50, IsUnicode = true)]
        [PXUIField(DisplayName = "Transaction Type")]
        public virtual string TransactionType { get; set; }
        public abstract class transactionType : BqlString.Field<transactionType> { }
        #endregion

        #region TransTime
        [PXDBString(50, IsUnicode = true)]
        [PXUIField(DisplayName = "Transaction Time")]
        public virtual string TransTime { get; set; }
        public abstract class transTime : BqlString.Field<transTime> { }
        #endregion

        #region TransAmount
        [PXDBDecimal(4)]
        [PXUIField(DisplayName = "Amount")]
        public virtual decimal? TransAmount { get; set; }
        public abstract class transAmount : BqlDecimal.Field<transAmount> { }
        #endregion

        #region Currency
        [PXDBString(10, IsUnicode = true)]
        [PXUIField(DisplayName = "Currency")]
        public virtual string Currency { get; set; }
        public abstract class currency : BqlString.Field<currency> { }
        #endregion

        #region BusinessShortCode
        [PXDBString(50, IsUnicode = true)]
        [PXUIField(DisplayName = "Business Short Code")]
        public virtual string BusinessShortCode { get; set; }
        public abstract class businessShortCode : BqlString.Field<businessShortCode> { }
        #endregion

        #region BusinessAccountNo
        [PXDBString(50, IsUnicode = true)]
        [PXUIField(DisplayName = "Business Account No")]
        public virtual string BusinessAccountNo { get; set; }
        public abstract class businessAccountNo : BqlString.Field<businessAccountNo> { }
        #endregion

        #region BillRefNumber
        [PXDBString(100, IsUnicode = true)]
        [PXUIField(DisplayName = "Bill Ref Number")]
        public virtual string BillRefNumber { get; set; }
        public abstract class billRefNumber : BqlString.Field<billRefNumber> { }
        #endregion

        #region InvoiceNumber
        [PXDBString(100, IsUnicode = true)]
        [PXUIField(DisplayName = "Invoice Number")]
        public virtual string InvoiceNumber { get; set; }
        public abstract class invoiceNumber : BqlString.Field<invoiceNumber> { }
        #endregion

        #region OrgAccountBalance
        [PXDBString(50, IsUnicode = true)]
        [PXUIField(DisplayName = "Org Account Balance")]
        public virtual string OrgAccountBalance { get; set; }
        public abstract class orgAccountBalance : BqlString.Field<orgAccountBalance> { }
        #endregion

        #region AvailableAccountBalance
        [PXDBString(50, IsUnicode = true)]
        [PXUIField(DisplayName = "Available Balance")]
        public virtual string AvailableAccountBalance { get; set; }
        public abstract class availableAccountBalance : BqlString.Field<availableAccountBalance> { }
        #endregion

        #region ThirdPartyTransID
        [PXDBString(50, IsUnicode = true)]
        [PXUIField(DisplayName = "Third Party Trans ID")]
        public virtual string ThirdPartyTransID { get; set; }
        public abstract class thirdPartyTransID : BqlString.Field<thirdPartyTransID> { }
        #endregion

        #region MSISDN
        [PXDBString(50, IsUnicode = true)]
        [PXUIField(DisplayName = "MSISDN (Phone)")]
        public virtual string MSISDN { get; set; }
        public abstract class mSISDN : BqlString.Field<mSISDN> { }
        #endregion

        #region PaymentDetails
        [PXDBString(255, IsUnicode = true)]
        [PXUIField(DisplayName = "Payment Details")]
        public virtual string PaymentDetails { get; set; }
        public abstract class paymentDetails : BqlString.Field<paymentDetails> { }
        #endregion

        #region SecureHash
        [PXDBString(255, IsUnicode = true)]
        [PXUIField(DisplayName = "Secure Hash")]
        public virtual string SecureHash { get; set; }
        public abstract class secureHash : BqlString.Field<secureHash> { }
        #endregion

        #region RawPayload
        [PXDBText(IsUnicode = true)]
        [PXUIField(DisplayName = "Raw JSON Payload")]
        public virtual string RawPayload { get; set; }
        public abstract class rawPayload : BqlString.Field<rawPayload> { }
        #endregion

        #region System Audit Fields (Fixes PX1069)
        [PXDBCreatedByID]
        public virtual Guid? CreatedByID { get; set; }
        public abstract class createdByID : BqlGuid.Field<createdByID> { }

        [PXDBCreatedByScreenID]
        public virtual string CreatedByScreenID { get; set; }
        public abstract class createdByScreenID : BqlString.Field<createdByScreenID> { }

        [PXDBCreatedDateTime]
        public virtual DateTime? CreatedDateTime { get; set; }
        public abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }

        [PXDBLastModifiedByID]
        public virtual Guid? LastModifiedByID { get; set; }
        public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }

        [PXDBLastModifiedByScreenID]
        public virtual string LastModifiedByScreenID { get; set; }
        public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }

        [PXDBLastModifiedDateTime]
        public virtual DateTime? LastModifiedDateTime { get; set; }
        public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }

        [PXDBTimestamp]
        public virtual byte[] Tstamp { get; set; }
        public abstract class tstamp : BqlByteArray.Field<tstamp> { }
        #endregion
    }
}