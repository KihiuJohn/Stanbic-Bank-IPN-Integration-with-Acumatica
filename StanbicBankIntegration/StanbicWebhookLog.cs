using System;
using PX.Data;
using PX.Data.BQL;

namespace StanbicBankIntegration
{
    [Serializable]
    [PXCacheName("Stanbic Webhook Log")]
    public class StanbicWebhookLog : PXBqlTable, IBqlTable
    {
        #region CompanyID
#pragma warning disable PX1027
        [PXDBInt]
        [PXDefault]
        public virtual int? CompanyID { get; set; }
        public abstract class companyID : PX.Data.BQL.BqlInt.Field<companyID> { }
#pragma warning restore PX1027
        #endregion

        #region LogID
        [PXDBIdentity(IsKey = true)]
        public virtual int? LogID { get; set; }
        public abstract class logID : BqlInt.Field<logID> { }
        #endregion

        #region TransID
        [PXDBString(50)]
        public virtual string TransID { get; set; }
        public abstract class transID : BqlString.Field<transID> { }
        #endregion

        #region EventTime
        [PXDBDateAndTime]
        public virtual DateTime? EventTime { get; set; }
        public abstract class eventTime : BqlDateTime.Field<eventTime> { }
        #endregion

        #region LogLevel
        [PXDBString(10)]
        public virtual string LogLevel { get; set; }
        public abstract class logLevel : BqlString.Field<logLevel> { }
        #endregion

        #region Message
        [PXDBText(IsUnicode = true)]
        public virtual string Message { get; set; }
        public abstract class message : BqlString.Field<message> { }
        #endregion

        #region Exception
        [PXDBText(IsUnicode = true)]
        public virtual string Exception { get; set; }
        public abstract class exception : BqlString.Field<exception> { }
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