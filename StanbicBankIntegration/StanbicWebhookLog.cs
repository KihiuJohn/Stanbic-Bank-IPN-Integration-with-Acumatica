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
        [PXDBIdentity(IsKey = true)]
        [PXUIField(DisplayName = "Log ID", Visible = false)]
        public virtual int? LogID { get; set; }
        public abstract class logID : BqlInt.Field<logID> { }
        #endregion

        #region TransID
        [PXDBString(50)]
        [PXUIField(DisplayName = "Transaction ID")]
        public virtual string TransID { get; set; }
        public abstract class transID : BqlString.Field<transID> { }
        #endregion

        #region EventTime
        [PXDBDateAndTime]
        [PXUIField(DisplayName = "Event Time")]
        public virtual DateTime? EventTime { get; set; }
        public abstract class eventTime : BqlDateTime.Field<eventTime> { }
        #endregion

        #region LogLevel
        [PXDBString(10)]
        [PXUIField(DisplayName = "Level")]
        public virtual string LogLevel { get; set; }
        public abstract class logLevel : BqlString.Field<logLevel> { }
        #endregion

        #region Message
        [PXDBText(IsUnicode = true)]
        [PXUIField(DisplayName = "Message")]
        public virtual string Message { get; set; }
        public abstract class message : BqlString.Field<message> { }
        #endregion

        #region Exception
        [PXDBText(IsUnicode = true)]
        [PXUIField(DisplayName = "Exception Details")]
        public virtual string Exception { get; set; }
        public abstract class exception : BqlString.Field<exception> { }
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