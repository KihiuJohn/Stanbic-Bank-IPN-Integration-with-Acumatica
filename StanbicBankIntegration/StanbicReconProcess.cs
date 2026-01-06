using System;
using System.Collections.Generic;
using PX.Common;
using PX.Data;
using PX.Objects.AR;

namespace StanbicBankIntegration
{
    public class StanbicReconProcess : PXGraph<StanbicReconProcess>
    {
        public PXProcessing<StanbicBankTxn,
               Where<StanbicBankTxn.status, Equal<ListValue.status_New>>> Transactions;

        public StanbicReconProcess()
        {
            Transactions.SetProcessDelegate(delegate (List<StanbicBankTxn> list)
            {
                ProcessTransactions(list);
            });
        }

        public static void ProcessTransactions(List<StanbicBankTxn> items)
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

                    ARPayment payment = paymentGraph.Document.Insert(new ARPayment()
                    {
                        DocType = ARDocType.Payment
                    });

                    payment.CustomerID = item.CustomerID;
                    payment.ExtRefNbr = item.TransID;
                    payment.CuryOrigDocAmt = item.TransAmount;
                    payment.DocDesc = "Stanbic " + item.TransID;
                    payment = paymentGraph.Document.Update(payment);

                    if (!string.IsNullOrEmpty(item.InvoiceRefNbr))
                    {
                        ARAdjust adj = new ARAdjust
                        {
                            AdjdDocType = ARDocType.Invoice,
                            AdjdRefNbr = item.InvoiceRefNbr,
                            CuryAdjgAmt = item.TransAmount
                        };
                        paymentGraph.Adjustments.Insert(adj);
                    }

                    paymentGraph.Actions.PressSave();

                    // FIX: Use PXDataFieldRestrict instead of PXDataFieldValue
                    // This explicitly tells the compiler this is a WHERE clause parameter
                    PXDatabase.Update<StanbicBankTxn>(
                        new PXDataFieldAssign<StanbicBankTxn.status>("Processed"),
                        new PXDataFieldRestrict<StanbicBankTxn.transID>(item.TransID)
                    );

                    PXProcessing<StanbicBankTxn>.SetInfo(items.IndexOf(item), Messages.ProcessedSuccess);
                }
                catch (Exception ex)
                {
                    errorOccurred = true;

                    // Same fix here for the error status update
                    PXDatabase.Update<StanbicBankTxn>(
                        new PXDataFieldAssign<StanbicBankTxn.status>("Error"),
                        new PXDataFieldRestrict<StanbicBankTxn.transID>(item.TransID)
                    );

                    PXProcessing<StanbicBankTxn>.SetError(items.IndexOf(item), ex.Message);
                }
            }

            if (errorOccurred)
                throw new PXOperationCompletedWithErrorException(Messages.BatchError);
        }

        [PXLocalizable(Messages.Prefix)]
        public static class Messages
        {
            public const string Prefix = "Stanbic Error";
            public const string CustomerRequired = "Customer ID is required to process payment.";
            public const string ProcessedSuccess = "Payment Created Successfully";
            public const string BatchError = "Some payments failed to process.";
        }

        public class ListValue
        {
            public class status_New : PX.Data.BQL.BqlString.Constant<status_New> { public status_New() : base("New") { } }
        }
    }
}