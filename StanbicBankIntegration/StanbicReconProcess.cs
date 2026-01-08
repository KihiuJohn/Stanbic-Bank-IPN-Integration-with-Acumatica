using System;
using System.Collections;
using System.Collections.Generic;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.AR;
using PX.Objects.CA;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;

namespace StanbicBankIntegration
{
    public class StanbicReconProcess : PXGraph<StanbicReconProcess>
    {
        public PXCancel<StanbicReconFilter> Cancel;

        public PXFilter<StanbicReconFilter> Filter;
        // Add this to your StanbicReconProcess class
        public PXAction<StanbicReconFilter> Process;
        [PXButton(CommitChanges = true)]
        [PXUIField(DisplayName = "Process", MapEnableRights = PXCacheRights.Select)]
        protected virtual void process()
        {
            // Process logic here - Acumatica will handle the processing through PXProcessing
            // You can add validation or pre-processing logic here
        }
        // Simplified view - removed complex BQL from attribute
        [PXFilterable]
        public PXProcessing<StanbicBankTxn> Transactions;

        public StanbicReconProcess()
        {
            // Set up processing
            Transactions.SetProcessDelegate(ProcessSelectedTransactions);

            // Allow updates so user can select Customer/Invoice in the grid
            Transactions.Cache.AllowUpdate = true;

            // Set process captions
            Transactions.SetProcessCaption("Process");
            Transactions.SetProcessAllCaption("Process All");
        }

        // Method to filter transactions to show only "New" records
        protected virtual IEnumerable transactions()
        {
            PXSelectBase<StanbicBankTxn> query = new PXSelect<StanbicBankTxn,
                Where<StanbicBankTxn.status, Equal<status_New>>>(this);

            foreach (StanbicBankTxn item in query.Select())
            {
                yield return item;
            }
        }

        // Static method to process transactions
        public static void ProcessSelectedTransactions(List<StanbicBankTxn> list)
        {
            // Create a new instance of the graph to get filter values
            var graph = PXGraph.CreateInstance<StanbicReconProcess>();
            var filter = graph.Filter.Current;

            if (filter == null || filter.CashAccountID == null || string.IsNullOrEmpty(filter.PaymentMethodID))
            {
                throw new PXException(Messages.MissingFilterParams);
            }

            ProcessTransactions(list, filter);
        }

        public static void ProcessTransactions(List<StanbicBankTxn> items, StanbicReconFilter filter)
        {
            ARPaymentEntry paymentGraph = PXGraph.CreateInstance<ARPaymentEntry>();
            bool errorOccurred = false;

            foreach (StanbicBankTxn item in items)
            {
                try
                {
                    if (item.CustomerID == null)
                        throw new PXException(Messages.CustomerRequired);

                    paymentGraph.Clear();

                    // Create the payment document
                    ARPayment pmt = new ARPayment
                    {
                        DocType = ARDocType.Payment,
                        DocDate = filter.AdjDate ?? DateTime.Today,
                        FinPeriodID = filter.FinPeriodID,
                        TranPeriodID = filter.FinPeriodID,
                        BranchID = filter.BranchID,
                        AdjDate = filter.AdjDate ?? DateTime.Today,
                        AdjFinPeriodID = filter.FinPeriodID,
                        AdjTranPeriodID = filter.FinPeriodID,
                        PaymentMethodID = filter.PaymentMethodID,
                        CashAccountID = filter.CashAccountID,
                        CuryID = filter.CuryID,
                        CustomerID = item.CustomerID,
                        ExtRefNbr = item.TransID,
                        CuryOrigDocAmt = item.TransAmount,
                        DocDesc = $"Stanbic: {item.BillRefNumber}",
                        Released = true,
                        Hold = false
                    };

                    pmt = paymentGraph.Document.Insert(pmt);
                    pmt = paymentGraph.Document.Update(pmt);

                    if (!string.IsNullOrEmpty(item.InvoiceRefNbr))
                    {
                        // Create adjustment for the invoice
                        ARAdjust adj = new ARAdjust
                        {
                            AdjdDocType = ARDocType.Invoice,
                            AdjdRefNbr = item.InvoiceRefNbr,
                            CuryAdjgAmt = item.TransAmount,
                            AdjgDocType = ARDocType.Payment,
                            AdjgRefNbr = pmt.RefNbr,
                            CustomerID = item.CustomerID,
                            AdjdCustomerID = item.CustomerID,
                            AdjgBranchID = filter.BranchID,
                            AdjdBranchID = filter.BranchID,
                            CuryAdjdAmt = item.TransAmount,
                            AdjAmt = item.TransAmount
                        };

                        paymentGraph.Adjustments.Insert(adj);
                        paymentGraph.Adjustments.Update(adj);
                    }

                    // Save the payment
                    paymentGraph.Save.Press();

                    // Update staging record
                    UpdateStagingRecord(item.TransID, pmt.RefNbr, pmt.DocType);
                }
                catch (Exception ex)
                {
                    errorOccurred = true;
                    PXProcessing<StanbicBankTxn>.SetError(items.IndexOf(item), ex.Message);
                }
            }

            if (errorOccurred)
                throw new PXOperationCompletedWithErrorException(Messages.BatchError);
        }

        // Method to update staging record using PXDatabase.Update
        private static void UpdateStagingRecord(string transID, string paymentRefNbr, string paymentDocType)
        {
            PXDatabase.Update<StanbicBankTxn>(
                new PXDataFieldAssign<StanbicBankTxn.status>("Processed"),
                new PXDataFieldAssign<StanbicBankTxn.paymentRefNbr>(paymentRefNbr),
                new PXDataFieldAssign<StanbicBankTxn.paymentDocType>(paymentDocType),
                new PXDataFieldAssign<StanbicBankTxn.lastModifiedDateTime>(DateTime.Now),
                new PXDataFieldRestrict<StanbicBankTxn.transID>(transID)
            );
        }

        protected virtual void _(Events.RowSelected<StanbicBankTxn> e)
        {
            StanbicBankTxn row = e.Row;
            if (row == null) return;
            bool isEditable = (row.Status == "New");
            PXUIFieldAttribute.SetEnabled<StanbicBankTxn.customerID>(e.Cache, row, isEditable);
            PXUIFieldAttribute.SetEnabled<StanbicBankTxn.invoiceRefNbr>(e.Cache, row, isEditable);
        }

        protected virtual void _(Events.FieldDefaulting<StanbicReconFilter, StanbicReconFilter.branchID> e)
        {
            if (e.Row != null && e.Row.BranchID == null)
            {
                e.NewValue = e.Cache.Graph.Accessinfo.BranchID;
            }
        }

        protected virtual void _(Events.FieldDefaulting<StanbicReconFilter, StanbicReconFilter.adjDate> e)
        {
            if (e.Row != null && e.Row.AdjDate == null)
            {
                e.NewValue = e.Cache.Graph.Accessinfo.BusinessDate;
            }
        }

        public class status_New : PX.Data.BQL.BqlString.Constant<status_New>
        {
            public status_New() : base("New") { }
        }

        // Messages class for localization
        [PXLocalizable]
        public static class Messages
        {
            public const string MissingFilterParams = "Missing filter parameters. Please select Payment Method and Cash Account.";
            public const string CustomerRequired = "Customer is required.";
            public const string BatchError = "Some transactions could not be processed. See details above.";
        }
    }

    // Filter DAC - Simplified
    [Serializable]
    [PXCacheName("Reconciliation Filter")]
    public class StanbicReconFilter : PXBqlTable, IBqlTable
    {
        #region BranchID
        public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
        [Branch]
        public virtual int? BranchID { get; set; }
        #endregion

        #region AdjDate
        public abstract class adjDate : PX.Data.BQL.BqlDateTime.Field<adjDate> { }
        [PXDBDate]
        [PXDefault(typeof(AccessInfo.businessDate))]
        [PXUIField(DisplayName = "Payment Date")]
        public virtual DateTime? AdjDate { get; set; }
        #endregion

        #region FinPeriodID
        public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
        [OpenPeriod(typeof(adjDate), typeof(branchID))]
        [PXUIField(DisplayName = "Post Period")]
        public virtual string FinPeriodID { get; set; }
        #endregion

        #region PaymentMethodID
        public abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID> { }
        [PXDBString(10, IsUnicode = true)]
        [PXUIField(DisplayName = "Payment Method")]
        [PXSelector(typeof(Search<PaymentMethod.paymentMethodID>),
            DescriptionField = typeof(PaymentMethod.descr))]
        public virtual string PaymentMethodID { get; set; }
        #endregion

        #region CashAccountID
        public abstract class cashAccountID : PX.Data.BQL.BqlInt.Field<cashAccountID> { }
        [PXDBInt]
        [PXUIField(DisplayName = "Cash Account")]
        //[CashAccount]
        public virtual int? CashAccountID { get; set; }
        #endregion

        #region CuryID
        public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
        [PXDBString(5, IsUnicode = true)]
        [PXDefault("KES")]
        [PXSelector(typeof(Currency.curyID))]
        [PXUIField(DisplayName = "Currency")]
        public virtual string CuryID { get; set; }
        #endregion

        #region Audit Fields
        public abstract class tstamp : BqlByteArray.Field<tstamp> { }
        [PXDBTimestamp]
        public virtual byte[] Tstamp { get; set; }

        public abstract class createdByID : BqlGuid.Field<createdByID> { }
        [PXDBCreatedByID]
        public virtual Guid? CreatedByID { get; set; }

        public abstract class createdByScreenID : BqlString.Field<createdByScreenID> { }
        [PXDBCreatedByScreenID]
        public virtual string CreatedByScreenID { get; set; }

        public abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }
        [PXDBCreatedDateTime]
        public virtual DateTime? CreatedDateTime { get; set; }

        public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }
        [PXDBLastModifiedByID]
        public virtual Guid? LastModifiedByID { get; set; }

        public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }
        [PXDBLastModifiedByScreenID]
        public virtual string LastModifiedByScreenID { get; set; }

        public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }
        [PXDBLastModifiedDateTime]
        public virtual DateTime? LastModifiedDateTime { get; set; }
        #endregion
    }
}