using System;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.GL.FinPeriods.TableDefinition;
using PX.Objects.CA;
using PX.Objects.CR;
using PX.Objects.GL.FinPeriods;

namespace StanbicBankIntegration
{
    [Serializable]
    [PXCacheName("Stanbic Bank Transaction")]
    [PXPrimaryGraph(typeof(StanbicPaymentRecon))]
    public class StanbicBankTxn : PXBqlTable, IBqlTable
    {
        #region TransID
        [PXDBString(50, IsUnicode = true, IsKey = true, InputMask = "")]
        [PXUIField(DisplayName = "Transaction ID")]
        [PXDefault(PersistingCheck = PXPersistingCheck.NullOrBlank)]
        public virtual string TransID { get; set; }
        public abstract class transID : BqlString.Field<transID> { }
        #endregion

        #region TotalApplied
        // Runtime calculated field - NOT persisted to database
        [PXDecimal(4)]
        [PXUIField(DisplayName = "Total Applied", Enabled = false)]
        public virtual decimal? TotalApplied { get; set; }
        public abstract class totalApplied : BqlDecimal.Field<totalApplied> { }
        #endregion

        #region Unallocated
        // Runtime calculated field - NOT persisted to database
        [PXDecimal(4)]
        [PXUIField(DisplayName = "Unallocated", Enabled = false)]
        public virtual decimal? Unallocated { get; set; }
        public abstract class unallocated : BqlDecimal.Field<unallocated> { }
        #endregion

        #region NoteID
        [PXNote]
        public virtual Guid? NoteID { get; set; }
        public abstract class noteID : BqlGuid.Field<noteID> { }
        #endregion

        #region Selected
        [PXBool]
        [PXUIField(DisplayName = "Selected")]
        [PXUnboundDefault(false)]
        public virtual bool? Selected { get; set; }
        public abstract class selected : BqlBool.Field<selected> { }
        #endregion

        #region CustomerID
        // Use the standard Acumatica [Customer] attribute - this handles everything properly
        [Customer(Visibility = PXUIVisibility.SelectorVisible, Filterable = true)]
        [PXUIField(DisplayName = "Customer")]
        public virtual int? CustomerID { get; set; }
        public abstract class customerID : BqlInt.Field<customerID> { }
        #endregion

        #region InvoiceRefNbr
        [PXDBString(15, IsUnicode = true)]
        [PXUIField(DisplayName = "Invoice Nbr.")]
        public virtual string InvoiceRefNbr { get; set; }
        public abstract class invoiceRefNbr : BqlString.Field<invoiceRefNbr> { }
        #endregion

        #region AdjDate
        [PXDBDate]
        [PXDefault(typeof(AccessInfo.businessDate))]
        [PXUIField(DisplayName = "Application Date", Required = true)]
        public virtual DateTime? AdjDate { get; set; }
        public abstract class adjDate : BqlDateTime.Field<adjDate> { }
        #endregion

        #region AdjFinPeriodID
        // Use MasterFinPeriod instead of FinPeriod - MasterFinPeriod doesn't have OrganizationID in its key
        [PXDBString(6, IsFixed = true)]
        [PXUIField(DisplayName = "Application Period", Required = true)]
        [PXSelector(typeof(Search<MasterFinPeriod.finPeriodID,
            Where<MasterFinPeriod.aRClosed, Equal<False>>,
            OrderBy<Desc<MasterFinPeriod.finPeriodID>>>),
            typeof(MasterFinPeriod.finPeriodID),
            typeof(MasterFinPeriod.descr),
            typeof(MasterFinPeriod.startDate),
            typeof(MasterFinPeriod.endDate))]
        public virtual string AdjFinPeriodID { get; set; }
        public abstract class adjFinPeriodID : BqlString.Field<adjFinPeriodID> { }
        #endregion

        #region PaymentMethodID
        [PXDBString(10, IsUnicode = true)]
        [PXUIField(DisplayName = "Payment Method", Required = true)]
        [PXSelector(typeof(Search<PaymentMethod.paymentMethodID,
            Where<PaymentMethod.isActive, Equal<True>,
            And<PaymentMethod.useForAR, Equal<True>>>>),
            DescriptionField = typeof(PaymentMethod.descr))]
        public virtual string PaymentMethodID { get; set; }
        public abstract class paymentMethodID : BqlString.Field<paymentMethodID> { }
        #endregion

        #region CashAccountID
        [PXDBInt]
        [PXUIField(DisplayName = "Cash Account", Required = true)]
        [PXSelector(
            typeof(Search<CashAccount.cashAccountID,
                Where<CashAccount.active, Equal<True>>>),
            typeof(CashAccount.cashAccountCD),
            typeof(CashAccount.descr),
            SubstituteKey = typeof(CashAccount.cashAccountCD),
            DescriptionField = typeof(CashAccount.descr))]
        public virtual int? CashAccountID { get; set; }
        public abstract class cashAccountID : BqlInt.Field<cashAccountID> { }
        #endregion

        #region PaymentRefNbr
        [PXDBString(15, IsUnicode = true)]
        [PXUIField(DisplayName = "Payment Ref Nbr", Enabled = false)]
        [PXSelector(typeof(Search<ARPayment.refNbr,
            Where<ARPayment.docType, Equal<ARDocType.payment>>>))]
        public virtual string PaymentRefNbr { get; set; }
        public abstract class paymentRefNbr : BqlString.Field<paymentRefNbr> { }
        #endregion

        #region PaymentDocType
        [PXDBString(3, IsFixed = true)]
        [PXUIField(DisplayName = "Payment Type", Enabled = false)]
        public virtual string PaymentDocType { get; set; }
        public abstract class paymentDocType : BqlString.Field<paymentDocType> { }
        #endregion

        #region ErrorMessage
        [PXDBString(500, IsUnicode = true)]
        [PXUIField(DisplayName = "Error Message", Enabled = false)]
        public virtual string ErrorMessage { get; set; }
        public abstract class errorMessage : BqlString.Field<errorMessage> { }
        #endregion

        #region Status
        [PXDBString(20, IsUnicode = true)]
        [PXUIField(DisplayName = "Status", Enabled = false)]
        [PXDefault("New")]
        [PXStringList(new string[] { "New", "Processed", "Error" },
                      new string[] { "New", "Processed", "Error" })]
        public virtual string Status { get; set; }
        public abstract class status : BqlString.Field<status> { }
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

        #region System Audit Fields
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