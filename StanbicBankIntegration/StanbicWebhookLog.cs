using System;
using PX.Data;
using PX.Data.BQL;

namespace StanbicBankIntegration
{
    [Serializable]
    [PXCacheName("Stanbic Webhook Log")]
    public class StanbicWebhookLog : PXBqlTable, IBqlTable
    {
        #region LogID
        public abstract class logID : BqlInt.Field<logID> { }
        [PXDBIdentity(IsKey = true)]
        public virtual int? LogID { get; set; }
        #endregion

        #region TransID
        public abstract class transID : BqlString.Field<transID> { }
        [PXDBString(50)]
        public virtual string TransID { get; set; }
        #endregion

        #region LogLevel
        public abstract class logLevel : BqlString.Field<logLevel> { }
        [PXDBString(10)]
        public virtual string LogLevel { get; set; }
        #endregion

        #region Message
        public abstract class message : BqlString.Field<message> { }
        [PXDBText(IsUnicode = true)]
        public virtual string Message { get; set; }
        #endregion

        #region Exception
        public abstract class exception : BqlString.Field<exception> { }
        [PXDBText(IsUnicode = true)]
        public virtual string Exception { get; set; }
        #endregion

        #region EventTime
        public abstract class eventTime : BqlDateTime.Field<eventTime> { }
        [PXDBDateAndTime]
        public virtual DateTime? EventTime { get; set; }
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