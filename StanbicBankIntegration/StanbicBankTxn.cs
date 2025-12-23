using System;
using PX.Data;
using PX.Data.BQL;

namespace StanbicBankIntegration
{
    [Serializable]
    [PXCacheName("Stanbic Bank Transaction")]
    public class StanbicBankTxn :PXBqlTable, IBqlTable
    {
        #region TransID
        [PXDBString(50, IsKey = true)]
        [PXUIField(DisplayName = "Transaction ID")]
        public virtual string TransID { get; set; }
        public abstract class transID : BqlString.Field<transID> { }
        #endregion

        #region TransactionType
        [PXDBString(20)]
        [PXUIField(DisplayName = "Transaction Type")]
        public virtual string TransactionType { get; set; }
        public abstract class transactionType : BqlString.Field<transactionType> { }
        #endregion

        #region TransTime
        [PXDBString(50)]
        [PXUIField(DisplayName = "Transaction Time")]
        public virtual string TransTime { get; set; }
        public abstract class transTime : BqlString.Field<transTime> { }
        #endregion

        #region TransAmount
        [PXDBDecimal]
        [PXUIField(DisplayName = "Amount")]
        public virtual decimal? TransAmount { get; set; }
        public abstract class transAmount : BqlDecimal.Field<transAmount> { }
        #endregion

        #region Currency
        [PXDBString(3)]
        [PXUIField(DisplayName = "Currency")]
        public virtual string Currency { get; set; }
        public abstract class currency : BqlString.Field<currency> { }
        #endregion

        #region MSISDN
        [PXDBString(20)]
        [PXUIField(DisplayName = "Phone Number")]
        public virtual string MSISDN { get; set; }
        public abstract class mSISDN : BqlString.Field<mSISDN> { }
        #endregion

        #region BillRefNumber
        [PXDBString(200)]
        [PXUIField(DisplayName = "Bill Reference")]
        public virtual string BillRefNumber { get; set; }
        public abstract class billRefNumber : BqlString.Field<billRefNumber> { }
        #endregion

        #region ThirdPartyTransID
        [PXDBString(100)]
        [PXUIField(DisplayName = "Third Party Trans ID")]
        public virtual string ThirdPartyTransID { get; set; }
        public abstract class thirdPartyTransID : BqlString.Field<thirdPartyTransID> { }
        #endregion

        #region SecureHash
        [PXDBString(256)]
        [PXUIField(DisplayName = "Secure Hash")]
        public virtual string SecureHash { get; set; }
        public abstract class secureHash : BqlString.Field<secureHash> { }
        #endregion

        #region RawPayload
        [PXDBText(IsUnicode = true)]
        [PXUIField(DisplayName = "Raw Payload")]
        public virtual string RawPayload { get; set; }
        public abstract class rawPayload : BqlString.Field<rawPayload> { }
        #endregion

        #region Status
        [PXDBString(20)]
        [PXUIField(DisplayName = "Processing Status")]
        public virtual string Status { get; set; }
        public abstract class status : BqlString.Field<status> { }
        #endregion

        // System fields
        [PXDBCreatedByID] public virtual Guid? CreatedByID { get; set; }
        public abstract class createdByID : BqlGuid.Field<createdByID> { }
        [PXDBCreatedDateTime] public virtual DateTime? CreatedDateTime { get; set; }
        public abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }
        [PXDBLastModifiedByID] public virtual Guid? LastModifiedByID { get; set; }
        public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }
        [PXDBLastModifiedDateTime] public virtual DateTime? LastModifiedDateTime { get; set; }
        public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }
        [PXDBTimestamp] public virtual byte[] Tstamp { get; set; }
        public abstract class tstamp : BqlByteArray.Field<tstamp> { }
    }
}