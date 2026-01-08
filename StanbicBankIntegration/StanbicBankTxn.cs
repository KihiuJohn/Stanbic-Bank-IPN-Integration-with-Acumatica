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

        #region Selected
        public abstract class selected : BqlBool.Field<selected> { }
        [PXBool]
        [PXUIField(DisplayName = "Selected")]
        public virtual bool? Selected { get; set; }
        #endregion

        #region TransID
        public abstract class transID : BqlString.Field<transID> { }
        [PXDBString(50, IsKey = true, IsUnicode = true)]
        [PXUIField(DisplayName = "Transaction ID")]
        public virtual string TransID { get; set; }
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

        #region Payment Link Fields (Fixed CS0426)
        public abstract class paymentRefNbr : BqlString.Field<paymentRefNbr> { }
        [PXDBString(15, IsUnicode = true)]
        [PXUIField(DisplayName = "Payment Ref", Enabled = false)]
        public virtual string PaymentRefNbr { get; set; }

        public abstract class paymentDocType : BqlString.Field<paymentDocType> { }
        [PXDBString(3, IsFixed = true)]
        public virtual string PaymentDocType { get; set; }
        #endregion

        #region InvoiceRefNbr
        public abstract class invoiceRefNbr : BqlString.Field<invoiceRefNbr> { }
        [PXDBString(15, IsUnicode = true)]
        [PXUIField(DisplayName = "Invoice Nbr.")]
        [ARInvoiceType.RefNbr(typeof(Search<ARInvoice.refNbr,
            Where<ARInvoice.customerID, Equal<Current<StanbicBankTxn.customerID>>>>))]
        public virtual string InvoiceRefNbr { get; set; }
        #endregion

        #region Status
        public abstract class status : BqlString.Field<status> { }
        [PXDBString(20, IsUnicode = true)]
        [PXUIField(DisplayName = "Status", Enabled = false)]
        [PXDefault("New")]
        public virtual string Status { get; set; }
        #endregion

        #region TransAmount
        public abstract class transAmount : BqlDecimal.Field<transAmount> { }
        [PXDBDecimal(4)]
        [PXUIField(DisplayName = "Amount")]
        public virtual decimal? TransAmount { get; set; }
        #endregion

        #region Currency
        public abstract class currency : BqlString.Field<currency> { }
        [PXDBString(10, IsUnicode = true)]
        [PXUIField(DisplayName = "Currency")]
        public virtual string Currency { get; set; }
        #endregion

        #region Bank Details
        public abstract class transactionType : BqlString.Field<transactionType> { }
        [PXDBString(50, IsUnicode = true)]
        [PXUIField(DisplayName = "Trans Type")]
        public virtual string TransactionType { get; set; }

        public abstract class billRefNumber : BqlString.Field<billRefNumber> { }
        [PXDBString(100, IsUnicode = true)]
        [PXUIField(DisplayName = "Bill Ref")]
        public virtual string BillRefNumber { get; set; }

        public abstract class mSISDN : BqlString.Field<mSISDN> { }
        [PXDBString(50, IsUnicode = true)]
        [PXUIField(DisplayName = "Phone")]
        public virtual string MSISDN { get; set; }

        public abstract class rawPayload : BqlString.Field<rawPayload> { }
        [PXDBText(IsUnicode = true)]
        public virtual string RawPayload { get; set; }
        #endregion

        #region Audit Fields
        [PXDBCreatedByID] public virtual Guid? CreatedByID { get; set; }
        public abstract class createdByID : BqlGuid.Field<createdByID> { }
        [PXDBCreatedByScreenID] public virtual string CreatedByScreenID { get; set; }
        public abstract class createdByScreenID : BqlString.Field<createdByScreenID> { }
        [PXDBCreatedDateTime] public virtual DateTime? CreatedDateTime { get; set; }
        public abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }

        [PXDBLastModifiedByID] public virtual Guid? LastModifiedByID { get; set; }
        public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }
        [PXDBLastModifiedByScreenID] public virtual string LastModifiedByScreenID { get; set; }
        public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }
        [PXDBLastModifiedDateTime] public virtual DateTime? LastModifiedDateTime { get; set; }
        public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }

        [PXDBTimestamp] public virtual byte[] Tstamp { get; set; }
        public abstract class tstamp : BqlByteArray.Field<tstamp> { }
        #endregion
    }
}