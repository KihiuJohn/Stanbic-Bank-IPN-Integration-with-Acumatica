using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CA;
using PX.Objects.GL;
using PX.Objects.GL.FinPeriods.TableDefinition;
using PX.Objects.CS;
using PX.Objects.GL.FinPeriods;

namespace StanbicBankIntegration
{
    [PXCopyPasteHiddenFields(typeof(StanbicBankTxn))]
    public class StanbicPaymentRecon : PXGraph<StanbicPaymentRecon, StanbicBankTxn>
    {
        #region Views

        public PXSelect<StanbicBankTxn> CurrentTransaction;

        [PXCopyPasteHiddenView]
        public PXSelect<InvoiceApplication> InvoiceApplications;

        #endregion

        #region Constructor

        public StanbicPaymentRecon()
        {
            InvoiceApplications.Cache.AllowInsert = false;
            InvoiceApplications.Cache.AllowUpdate = true;
            InvoiceApplications.Cache.AllowDelete = false;
        }

        #endregion

        #region Data View Delegates

        protected virtual IEnumerable invoiceApplications()
        {
            StanbicBankTxn txn = CurrentTransaction.Current;
            if (txn == null || txn.CustomerID == null)
                yield break;

            // Query open invoices for this customer
            PXSelectBase<ARInvoice> select = new PXSelect<ARInvoice,
                Where<ARInvoice.customerID, Equal<Required<ARInvoice.customerID>>,
                    And<ARInvoice.docType, Equal<ARDocType.invoice>,
                    And<ARInvoice.openDoc, Equal<True>,
                    And<ARInvoice.released, Equal<True>>>>>,
                OrderBy<Asc<ARInvoice.dueDate, Asc<ARInvoice.refNbr>>>>(this);

            decimal remainingAmount = txn.TransAmount ?? 0m;
            decimal totalApplied = 0m;

            foreach (ARInvoice invoice in select.Select(txn.CustomerID))
            {
                // Check if already in cache (user modified)
                InvoiceApplication cached = (InvoiceApplication)InvoiceApplications.Cache.Locate(
                    new InvoiceApplication { RefNbr = invoice.RefNbr, DocType = invoice.DocType });

                if (cached != null && InvoiceApplications.Cache.GetStatus(cached) != PXEntryStatus.InsertedDeleted)
                {
                    totalApplied += cached.CuryApplAmt ?? 0m;
                    yield return cached;
                    continue;
                }

                // Create new application record
                InvoiceApplication app = new InvoiceApplication
                {
                    RefNbr = invoice.RefNbr,
                    DocType = invoice.DocType,
                    CustomerID = invoice.CustomerID,
                    DocDate = invoice.DocDate,
                    DueDate = invoice.DueDate,
                    InvoiceNbr = invoice.InvoiceNbr,
                    DocDesc = invoice.DocDesc,
                    CuryID = invoice.CuryID,
                    CuryOrigDocAmt = invoice.CuryOrigDocAmt,
                    CuryDocBal = invoice.CuryDocBal,
                    Status = invoice.Status
                };

                // Auto-allocate (oldest invoice first - FIFO)
                if (remainingAmount > 0 && (invoice.CuryDocBal ?? 0m) > 0)
                {
                    decimal invoiceBalance = invoice.CuryDocBal ?? 0m;

                    if (remainingAmount >= invoiceBalance)
                    {
                        app.Selected = true;
                        app.CuryApplAmt = invoiceBalance;
                        app.PaymentStatus = "Full";
                        remainingAmount -= invoiceBalance;
                        totalApplied += invoiceBalance;
                    }
                    else
                    {
                        app.Selected = true;
                        app.CuryApplAmt = remainingAmount;
                        app.PaymentStatus = "Partial";
                        totalApplied += remainingAmount;
                        remainingAmount = 0m;
                    }
                }
                else
                {
                    app.Selected = false;
                    app.CuryApplAmt = 0m;
                    app.PaymentStatus = null;
                }

                app.RemainingBalance = (invoice.CuryDocBal ?? 0m) - (app.CuryApplAmt ?? 0m);

                yield return app;
            }

            // Update summary on transaction
            if (txn != null)
            {
                txn.TotalApplied = totalApplied;
                txn.Unallocated = (txn.TransAmount ?? 0m) - totalApplied;
            }
        }

        #endregion

        #region Events

        protected virtual void _(Events.RowSelected<StanbicBankTxn> e)
        {
            if (e.Row == null) return;

            StanbicBankTxn row = e.Row;
            bool isNew = row.Status == "New";
            bool isError = row.Status == "Error";
            bool isProcessed = row.Status == "Processed";
            bool canEdit = isNew || isError;

            // Enable/disable fields based on status
            PXUIFieldAttribute.SetEnabled<StanbicBankTxn.customerID>(e.Cache, row, canEdit);
            PXUIFieldAttribute.SetEnabled<StanbicBankTxn.adjDate>(e.Cache, row, canEdit);
            PXUIFieldAttribute.SetEnabled<StanbicBankTxn.adjFinPeriodID>(e.Cache, row, canEdit);
            PXUIFieldAttribute.SetEnabled<StanbicBankTxn.paymentMethodID>(e.Cache, row, canEdit);
            PXUIFieldAttribute.SetEnabled<StanbicBankTxn.cashAccountID>(e.Cache, row, canEdit);

            // Ensure required field visibility
            PXUIFieldAttribute.SetRequired<StanbicBankTxn.customerID>(e.Cache, true);
            PXUIFieldAttribute.SetRequired<StanbicBankTxn.adjDate>(e.Cache, true);
            PXUIFieldAttribute.SetRequired<StanbicBankTxn.adjFinPeriodID>(e.Cache, true);
            PXUIFieldAttribute.SetRequired<StanbicBankTxn.paymentMethodID>(e.Cache, true);
            PXUIFieldAttribute.SetRequired<StanbicBankTxn.cashAccountID>(e.Cache, true);

            // Enable/disable buttons
            bool hasCustomer = row.CustomerID != null;
            bool hasCashAccount = row.CashAccountID != null;
            bool hasPeriod = !string.IsNullOrEmpty(row.AdjFinPeriodID);
            bool hasPaymentRef = !string.IsNullOrEmpty(row.PaymentRefNbr);

            createPayment.SetEnabled(canEdit && hasCustomer && hasCashAccount && hasPeriod);
            viewPayment.SetEnabled(isProcessed && hasPaymentRef);

            // Calculate totals
            CalculateTotals();
        }

        protected virtual void _(Events.RowInserted<StanbicBankTxn> e)
        {
            if (e.Row == null) return;

            // Set default period for new records
            if (string.IsNullOrEmpty(e.Row.AdjFinPeriodID) && e.Row.AdjDate != null)
            {
                string periodID = GetPeriodForDate(e.Row.AdjDate);
                if (!string.IsNullOrEmpty(periodID))
                {
                    e.Cache.SetValueExt<StanbicBankTxn.adjFinPeriodID>(e.Row, periodID);
                }
            }
        }

        protected virtual void _(Events.FieldDefaulting<StanbicBankTxn, StanbicBankTxn.adjFinPeriodID> e)
        {
            if (e.Row == null) return;

            DateTime? date = e.Row.AdjDate ?? Accessinfo.BusinessDate;
            if (date != null)
            {
                string periodID = GetPeriodForDate(date);
                if (!string.IsNullOrEmpty(periodID))
                {
                    e.NewValue = periodID;
                    e.Cancel = true;
                }
            }
        }

        protected virtual void _(Events.FieldUpdated<StanbicBankTxn, StanbicBankTxn.adjDate> e)
        {
            if (e.Row == null) return;

            // When date changes, update the period
            if (e.Row.AdjDate != null)
            {
                string periodID = GetPeriodForDate(e.Row.AdjDate);
                if (!string.IsNullOrEmpty(periodID))
                {
                    e.Cache.SetValueExt<StanbicBankTxn.adjFinPeriodID>(e.Row, periodID);
                }
            }
        }

        protected virtual void _(Events.FieldUpdated<StanbicBankTxn, StanbicBankTxn.customerID> e)
        {
            if (e.Row == null) return;

            StanbicBankTxn row = e.Row;

            // Clear and refresh invoice list when customer changes
            InvoiceApplications.Cache.Clear();
            InvoiceApplications.View.RequestRefresh();

            // Clear payment method and cash account
            e.Cache.SetValueExt<StanbicBankTxn.paymentMethodID>(row, null);
            e.Cache.SetValueExt<StanbicBankTxn.cashAccountID>(row, null);

            // Set default payment method and cash account from customer
            if (row.CustomerID != null)
            {
                // First try CustomerPaymentMethod
                CustomerPaymentMethod cpm = PXSelect<CustomerPaymentMethod,
                    Where<CustomerPaymentMethod.bAccountID, Equal<Required<CustomerPaymentMethod.bAccountID>>,
                        And<CustomerPaymentMethod.isActive, Equal<True>>>>
                    .SelectWindowed(this, 0, 1, row.CustomerID);

                if (cpm != null && !string.IsNullOrEmpty(cpm.PaymentMethodID))
                {
                    e.Cache.SetValueExt<StanbicBankTxn.paymentMethodID>(row, cpm.PaymentMethodID);

                    if (cpm.CashAccountID != null)
                    {
                        e.Cache.SetValueExt<StanbicBankTxn.cashAccountID>(row, cpm.CashAccountID);
                    }
                }
                else
                {
                    // Try to get default payment method from customer record
                    Customer customer = PXSelect<Customer,
                        Where<Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>
                        .Select(this, row.CustomerID);

                    if (customer != null && !string.IsNullOrEmpty(customer.DefPaymentMethodID))
                    {
                        e.Cache.SetValueExt<StanbicBankTxn.paymentMethodID>(row, customer.DefPaymentMethodID);
                    }
                }
            }
        }

        protected virtual void _(Events.FieldUpdated<StanbicBankTxn, StanbicBankTxn.paymentMethodID> e)
        {
            if (e.Row == null) return;

            // Clear cash account when payment method changes
            e.Cache.SetValueExt<StanbicBankTxn.cashAccountID>(e.Row, null);

            // Set default cash account for the payment method
            if (!string.IsNullOrEmpty(e.Row.PaymentMethodID))
            {
                PaymentMethodAccount pma = PXSelect<PaymentMethodAccount,
                    Where<PaymentMethodAccount.paymentMethodID, Equal<Required<PaymentMethodAccount.paymentMethodID>>,
                        And<PaymentMethodAccount.useForAR, Equal<True>,
                        And<PaymentMethodAccount.aRIsDefault, Equal<True>>>>>
                    .Select(this, e.Row.PaymentMethodID);

                if (pma != null && pma.CashAccountID != null)
                {
                    e.Cache.SetValueExt<StanbicBankTxn.cashAccountID>(e.Row, pma.CashAccountID);
                }
            }
        }

        protected virtual void _(Events.FieldUpdated<InvoiceApplication, InvoiceApplication.selected> e)
        {
            if (e.Row == null) return;

            InvoiceApplication row = e.Row;

            if (row.Selected != true)
            {
                row.CuryApplAmt = 0m;
                row.PaymentStatus = null;
                row.RemainingBalance = row.CuryDocBal;
            }
            else
            {
                StanbicBankTxn txn = CurrentTransaction.Current;
                if (txn != null)
                {
                    decimal available = GetAvailableAmount(row);
                    decimal invoiceBal = row.CuryDocBal ?? 0m;

                    if (available >= invoiceBal)
                    {
                        row.CuryApplAmt = invoiceBal;
                        row.PaymentStatus = "Full";
                    }
                    else if (available > 0)
                    {
                        row.CuryApplAmt = available;
                        row.PaymentStatus = "Partial";
                    }
                    else
                    {
                        row.CuryApplAmt = 0m;
                        row.PaymentStatus = null;
                        row.Selected = false;
                    }

                    row.RemainingBalance = invoiceBal - (row.CuryApplAmt ?? 0m);
                }
            }

            InvoiceApplications.Cache.Update(row);
            CalculateTotals();
        }

        protected virtual void _(Events.FieldUpdated<InvoiceApplication, InvoiceApplication.curyApplAmt> e)
        {
            if (e.Row == null) return;

            InvoiceApplication row = e.Row;
            decimal applAmt = row.CuryApplAmt ?? 0m;
            decimal docBal = row.CuryDocBal ?? 0m;

            row.RemainingBalance = docBal - applAmt;

            if (applAmt >= docBal && applAmt > 0)
                row.PaymentStatus = "Full";
            else if (applAmt > 0)
                row.PaymentStatus = "Partial";
            else
                row.PaymentStatus = null;

            InvoiceApplications.Cache.Update(row);
            CalculateTotals();
        }

        protected virtual void _(Events.FieldVerifying<InvoiceApplication, InvoiceApplication.curyApplAmt> e)
        {
            if (e.Row == null || e.NewValue == null) return;

            decimal newAmt = (decimal)e.NewValue;
            decimal docBal = e.Row.CuryDocBal ?? 0m;

            if (newAmt < 0)
            {
                throw new PXSetPropertyException(Messages.AmountCannotBeNegative);
            }

            if (newAmt > docBal)
            {
                throw new PXSetPropertyException(Messages.AmountExceedsInvoiceBalance, docBal);
            }

            decimal otherApplied = GetTotalAppliedExcluding(e.Row);
            StanbicBankTxn txn = CurrentTransaction.Current;
            decimal maxPayment = txn?.TransAmount ?? 0m;

            if (otherApplied + newAmt > maxPayment)
            {
                throw new PXSetPropertyException(Messages.TotalExceedsPaymentAmount, maxPayment);
            }
        }

        protected virtual void _(Events.RowSelected<InvoiceApplication> e)
        {
            if (e.Row == null) return;

            PXUIFieldAttribute.SetEnabled<InvoiceApplication.curyApplAmt>(e.Cache, e.Row, e.Row.Selected == true);
        }

        protected virtual void _(Events.RowPersisting<InvoiceApplication> e)
        {
            // Prevent persistence - this is a virtual/projection DAC
            e.Cancel = true;
        }

        #endregion

        #region Persist Override

        public override void Persist()
        {
            // Clear virtual DAC before persisting
            InvoiceApplications.Cache.Clear();
            InvoiceApplications.Cache.ClearQueryCache();
            base.Persist();
        }

        #endregion

        #region Actions

        public PXSave<StanbicBankTxn> Save;
        public PXCancel<StanbicBankTxn> Cancel;

        public PXCopyPasteAction<StanbicBankTxn> copyPasteAction;

        public PXAction<StanbicBankTxn> CopyDocument;
        [PXUIField(Visible = false)]
        [PXButton]
        protected virtual IEnumerable copyDocument(PXAdapter adapter) { return adapter.Get(); }

        public PXAction<StanbicBankTxn> PasteDocument;
        [PXUIField(Visible = false)]
        [PXButton]
        protected virtual IEnumerable pasteDocument(PXAdapter adapter) { return adapter.Get(); }

        public PXAction<StanbicBankTxn> createPayment;
        [PXButton(CommitChanges = true)]
        [PXUIField(DisplayName = "Create Payment", MapEnableRights = PXCacheRights.Update)]
        protected virtual IEnumerable CreatePayment(PXAdapter adapter)
        {
            StanbicBankTxn txn = CurrentTransaction.Current;
            if (txn == null) return adapter.Get();
            if (txn.Status != "New" && txn.Status != "Error") return adapter.Get();

            // 1. Validate required fields
            if (txn.CustomerID == null) throw new PXException(Messages.CustomerRequired);
            if (txn.TransAmount == null || txn.TransAmount <= 0) throw new PXException(Messages.AmountRequired);
            if (string.IsNullOrEmpty(txn.PaymentMethodID)) throw new PXException(Messages.PaymentMethodRequired);
            if (txn.CashAccountID == null) throw new PXException(Messages.CashAccountRequired);
            if (string.IsNullOrEmpty(txn.AdjFinPeriodID)) throw new PXException(Messages.PeriodRequired);

            // 2. Get selected invoices
            var selectedInvoices = new List<InvoiceApplication>();
            foreach (InvoiceApplication app in InvoiceApplications.Select())
            {
                if (app.Selected == true && (app.CuryApplAmt ?? 0m) > 0)
                {
                    selectedInvoices.Add(app);
                }
            }

            // 3. Resolve Branch from First Invoice (Prevents "RefNbr not found" error)
            int? targetBranchID = null;
            if (selectedInvoices.Count > 0)
            {
                var firstInv = selectedInvoices[0];
                ARInvoice invRecord = ARInvoice.PK.Find(this, firstInv.DocType, firstInv.RefNbr);
                if (invRecord != null) targetBranchID = invRecord.BranchID;
            }

            // 4. Initialize Payment Graph
            ARPaymentEntry paymentGraph = PXGraph.CreateInstance<ARPaymentEntry>();

            // FIX: Suppress "Update Last Ref Number" dialog
            paymentGraph.Document.View.Answer = WebDialogResult.No;

            try
            {
                paymentGraph.Clear();

                // Create payment header
                ARPayment payment = new ARPayment();
                payment.DocType = ARDocType.Payment;
                payment = paymentGraph.Document.Insert(payment);

                // Set Branch FIRST
                if (targetBranchID != null)
                    paymentGraph.Document.SetValueExt<ARPayment.branchID>(payment, targetBranchID);

                // Set customer
                paymentGraph.Document.SetValueExt<ARPayment.customerID>(payment, txn.CustomerID);

                // Set payment method & cash account
                paymentGraph.Document.SetValueExt<ARPayment.paymentMethodID>(payment, txn.PaymentMethodID);
                paymentGraph.Document.SetValueExt<ARPayment.cashAccountID>(payment, txn.CashAccountID);

                // Set amount
                paymentGraph.Document.SetValueExt<ARPayment.curyOrigDocAmt>(payment, txn.TransAmount);

                // Set date
                if (txn.AdjDate != null)
                    paymentGraph.Document.SetValueExt<ARPayment.adjDate>(payment, txn.AdjDate);

                //Set transaction id as External ref nbr 
                // paymentGraph.Document.SetValueExt<ARPayment.extRefNbr>(payment, txn.TransID);

                //Set transaction id as External ref nbr 
                paymentGraph.Document.SetValueExt<ARPayment.docDesc>(payment, txn.PaymentDetails);


                // Set External Ref (Bank ID)
                //string extRef = txn.TransID;
                //if (string.IsNullOrEmpty(extRef)) extRef = "STANBIC-" + DateTime.Now.ToString("yyyyMMddHHmmss");

                // paymentGraph.Document.SetValueExt<ARPayment.extRefNbr>(payment, extRef);
                // paymentGraph.Document.Current.DocDesc = string.Format(Messages.PaymentDescription, extRef);

                payment = paymentGraph.Document.Update(paymentGraph.Document.Current);

                // Apply to selected invoices
                foreach (InvoiceApplication app in selectedInvoices)
                {
                    // 1. Initialize the line with KEYS (DocType + RefNbr) BEFORE inserting
                    ARAdjust adj = new ARAdjust
                    {
                        AdjdDocType = app.DocType,
                        AdjdRefNbr = app.RefNbr
                    };


                    adj = paymentGraph.Adjustments.Insert(adj);

                    if (adj == null)
                    {
                        throw new PXException(
                        		// Acuminator disable once PX1050 HardcodedStringInLocalizationMethod [Justification]
                        		$"Could not add Invoice {app.RefNbr} to the payment. Check for Branch or Customer mismatches.");
                    }

                    // 3. Set the Amount to Apply
                    paymentGraph.Adjustments.SetValueExt<ARAdjust.curyAdjgAmt>(adj, app.CuryApplAmt);

                    // 4. Update the line to finalize calculations
                    paymentGraph.Adjustments.Update(adj);
                }

                // Save the payment
                paymentGraph.Actions.PressSave();

                // Get created payment info
                string paymentRefNbr = paymentGraph.Document.Current.RefNbr;
                string paymentDocType = paymentGraph.Document.Current.DocType;

                // Update transaction record
                PXDatabase.Update<StanbicBankTxn>(
                    new PXDataFieldAssign<StanbicBankTxn.paymentDocType>(paymentDocType),
                    new PXDataFieldAssign<StanbicBankTxn.paymentRefNbr>(paymentRefNbr),
                    new PXDataFieldAssign<StanbicBankTxn.status>("Processed"),
                    new PXDataFieldAssign<StanbicBankTxn.errorMessage>(null),
                    new PXDataFieldAssign<StanbicBankTxn.lastModifiedDateTime>(PXTimeZoneInfo.UtcNow),
                    new PXDataFieldRestrict<StanbicBankTxn.transID>(PXDbType.NVarChar, 50, txn.TransID, PXComp.EQ)
                );

                // Refresh screen
                CurrentTransaction.Cache.Clear();
                CurrentTransaction.View.RequestRefresh();

                // Redirect to payment
                throw new PXRedirectRequiredException(paymentGraph, Messages.PaymentCreated);
            }
            catch (PXRedirectRequiredException)
            {
                throw;
            }
            catch (Exception ex)
            {
                string errorMsg = ex.Message;
                if (ex.InnerException != null) errorMsg += " | " + ex.InnerException.Message;
                if (errorMsg.Length > 500) errorMsg = errorMsg.Substring(0, 500);

                PXDatabase.Update<StanbicBankTxn>(
                    new PXDataFieldAssign<StanbicBankTxn.status>("Error"),
                    new PXDataFieldAssign<StanbicBankTxn.errorMessage>(errorMsg),
                    new PXDataFieldAssign<StanbicBankTxn.lastModifiedDateTime>(PXTimeZoneInfo.UtcNow),
                    new PXDataFieldRestrict<StanbicBankTxn.transID>(PXDbType.NVarChar, 50, txn.TransID, PXComp.EQ)
                );

                CurrentTransaction.Cache.Clear();
                CurrentTransaction.View.RequestRefresh();

                throw new PXException(Messages.PaymentCreationFailed, errorMsg);
            }

            return adapter.Get();
        }

        public PXAction<StanbicBankTxn> viewPayment;
        [PXButton]
        [PXUIField(DisplayName = "View Payment", MapEnableRights = PXCacheRights.Select)]
        protected virtual IEnumerable ViewPayment(PXAdapter adapter)
        {
            StanbicBankTxn txn = CurrentTransaction.Current;
            if (txn == null || txn.Status != "Processed") return adapter.Get();

            if (!string.IsNullOrEmpty(txn.PaymentRefNbr) && !string.IsNullOrEmpty(txn.PaymentDocType))
            {
                ARPaymentEntry graph = PXGraph.CreateInstance<ARPaymentEntry>();
                graph.Document.Current = graph.Document.Search<ARPayment.refNbr>(txn.PaymentRefNbr, txn.PaymentDocType);

                if (graph.Document.Current != null)
                {
                    throw new PXRedirectRequiredException(graph, Messages.ViewPaymentTitle);
                }
            }

            throw new PXException(Messages.PaymentNotFound);
        }

        public PXAction<StanbicBankTxn> autoAllocate;
        [PXButton]
        [PXUIField(DisplayName = "Auto-Allocate", MapEnableRights = PXCacheRights.Update)]
        protected virtual IEnumerable AutoAllocate(PXAdapter adapter)
        {
            InvoiceApplications.Cache.Clear();
            InvoiceApplications.Cache.ClearQueryCache();
            InvoiceApplications.View.RequestRefresh();
            CalculateTotals();
            return adapter.Get();
        }

        public PXAction<StanbicBankTxn> clearAllocation;
        [PXButton]
        [PXUIField(DisplayName = "Clear Selection", MapEnableRights = PXCacheRights.Update)]
        protected virtual IEnumerable ClearAllocation(PXAdapter adapter)
        {
            foreach (InvoiceApplication app in InvoiceApplications.Select())
            {
                if (app != null && (app.Selected == true || (app.CuryApplAmt ?? 0) > 0))
                {
                    app.Selected = false;
                    app.CuryApplAmt = 0m;
                    app.PaymentStatus = null;
                    app.RemainingBalance = app.CuryDocBal;
                    InvoiceApplications.Cache.Update(app);
                }
            }

            CalculateTotals();
            InvoiceApplications.View.RequestRefresh();

            return adapter.Get();
        }

        #endregion

        #region Helper Methods

        private string GetPeriodForDate(DateTime? date)
        {
            if (date == null) return null;

            try
            {
                // Use MasterFinPeriod to find the period for the date
                MasterFinPeriod period = PXSelect<MasterFinPeriod,
                    Where<MasterFinPeriod.startDate, LessEqual<Required<MasterFinPeriod.startDate>>,
                        And<MasterFinPeriod.endDate, Greater<Required<MasterFinPeriod.endDate>>,
                        And<MasterFinPeriod.aRClosed, Equal<False>>>>,
                    OrderBy<Desc<MasterFinPeriod.finPeriodID>>>
                    .SelectWindowed(this, 0, 1, date, date);

                if (period != null)
                {
                    return period.FinPeriodID;
                }

                // Fallback: get any period that contains this date (even if AR is closed)
                MasterFinPeriod anyPeriod = PXSelect<MasterFinPeriod,
                    Where<MasterFinPeriod.startDate, LessEqual<Required<MasterFinPeriod.startDate>>,
                        And<MasterFinPeriod.endDate, Greater<Required<MasterFinPeriod.endDate>>>>,
                    OrderBy<Desc<MasterFinPeriod.finPeriodID>>>
                    .SelectWindowed(this, 0, 1, date, date);

                return anyPeriod?.FinPeriodID;
            }
            catch
            {
                return null;
            }
        }

        private string GetFullErrorMessage(Exception ex)
        {
            var messages = new List<string>();
            Exception current = ex;

            while (current != null)
            {
                if (!string.IsNullOrEmpty(current.Message) && !messages.Contains(current.Message))
                {
                    messages.Add(current.Message);
                }

                if (current is PXOuterException outerEx)
                {
                    foreach (string innerMsg in outerEx.InnerMessages)
                    {
                        if (!string.IsNullOrEmpty(innerMsg) && !messages.Contains(innerMsg))
                        {
                            messages.Add(innerMsg);
                        }
                    }
                }

                current = current.InnerException;
            }

            return string.Join(" | ", messages);
        }

        private decimal GetTotalAppliedExcluding(InvoiceApplication excludeRow)
        {
            decimal total = 0m;
            foreach (InvoiceApplication app in InvoiceApplications.Select())
            {
                if (app != null && app.Selected == true)
                {
                    if (excludeRow == null || app.RefNbr != excludeRow.RefNbr || app.DocType != excludeRow.DocType)
                    {
                        total += app.CuryApplAmt ?? 0m;
                    }
                }
            }
            return total;
        }

        private decimal GetAvailableAmount(InvoiceApplication excludeRow)
        {
            StanbicBankTxn txn = CurrentTransaction.Current;
            if (txn == null) return 0m;

            decimal totalApplied = GetTotalAppliedExcluding(excludeRow);
            return (txn.TransAmount ?? 0m) - totalApplied;
        }

        private void CalculateTotals()
        {
            StanbicBankTxn txn = CurrentTransaction.Current;
            if (txn == null) return;

            decimal totalApplied = 0m;
            foreach (InvoiceApplication app in InvoiceApplications.Select())
            {
                if (app != null && app.Selected == true)
                {
                    totalApplied += app.CuryApplAmt ?? 0m;
                }
            }

            txn.TotalApplied = totalApplied;
            txn.Unallocated = (txn.TransAmount ?? 0m) - totalApplied;
        }

        #endregion

        #region InvoiceApplication DAC

        [Serializable]
        [PXHidden]
        [PXVirtual]
        [PXCacheName("Invoice Application")]
        public class InvoiceApplication : PXBqlTable, IBqlTable
        {
            #region Selected
            [PXBool]
            [PXUIField(DisplayName = "Apply")]
            [PXUnboundDefault(false)]
            public virtual bool? Selected { get; set; }
            public abstract class selected : BqlBool.Field<selected> { }
            #endregion

            #region RefNbr
            [PXString(15, IsUnicode = true, IsKey = true)]
            [PXUIField(DisplayName = "Invoice Nbr")]
            public virtual string RefNbr { get; set; }
            public abstract class refNbr : BqlString.Field<refNbr> { }
            #endregion

            #region DocType
            [PXString(3, IsKey = true, IsFixed = true)]
            [PXUIField(DisplayName = "Type")]
            public virtual string DocType { get; set; }
            public abstract class docType : BqlString.Field<docType> { }
            #endregion

            #region CustomerID
            [PXInt]
            public virtual int? CustomerID { get; set; }
            public abstract class customerID : BqlInt.Field<customerID> { }
            #endregion

            #region DocDate
            [PXDate]
            [PXUIField(DisplayName = "Date")]
            public virtual DateTime? DocDate { get; set; }
            public abstract class docDate : BqlDateTime.Field<docDate> { }
            #endregion

            #region DueDate
            [PXDate]
            [PXUIField(DisplayName = "Due Date")]
            public virtual DateTime? DueDate { get; set; }
            public abstract class dueDate : BqlDateTime.Field<dueDate> { }
            #endregion

            #region InvoiceNbr
            [PXString(40, IsUnicode = true)]
            [PXUIField(DisplayName = "Invoice #")]
            public virtual string InvoiceNbr { get; set; }
            public abstract class invoiceNbr : BqlString.Field<invoiceNbr> { }
            #endregion

            #region DocDesc
            [PXString(255, IsUnicode = true)]
            [PXUIField(DisplayName = "Description")]
            public virtual string DocDesc { get; set; }
            public abstract class docDesc : BqlString.Field<docDesc> { }
            #endregion

            #region CuryID
            [PXString(5, IsUnicode = true)]
            [PXUIField(DisplayName = "Currency")]
            public virtual string CuryID { get; set; }
            public abstract class curyID : BqlString.Field<curyID> { }
            #endregion

            #region CuryOrigDocAmt
            [PXDecimal(4)]
            [PXUIField(DisplayName = "Original Amount")]
            public virtual decimal? CuryOrigDocAmt { get; set; }
            public abstract class curyOrigDocAmt : BqlDecimal.Field<curyOrigDocAmt> { }
            #endregion

            #region CuryDocBal
            [PXDecimal(4)]
            [PXUIField(DisplayName = "Balance")]
            public virtual decimal? CuryDocBal { get; set; }
            public abstract class curyDocBal : BqlDecimal.Field<curyDocBal> { }
            #endregion

            #region CuryApplAmt
            [PXDecimal(4)]
            [PXUIField(DisplayName = "Amount to Apply")]
            public virtual decimal? CuryApplAmt { get; set; }
            public abstract class curyApplAmt : BqlDecimal.Field<curyApplAmt> { }
            #endregion

            #region RemainingBalance
            [PXDecimal(4)]
            [PXUIField(DisplayName = "Remaining Balance", Enabled = false)]
            public virtual decimal? RemainingBalance { get; set; }
            public abstract class remainingBalance : BqlDecimal.Field<remainingBalance> { }
            #endregion

            #region PaymentStatus
            [PXString(10, IsUnicode = true)]
            [PXUIField(DisplayName = "Payment Status", Enabled = false)]
            public virtual string PaymentStatus { get; set; }
            public abstract class paymentStatus : BqlString.Field<paymentStatus> { }
            #endregion

            #region Status
            [PXString(1, IsFixed = true)]
            [PXUIField(DisplayName = "Status")]
            public virtual string Status { get; set; }
            public abstract class status : BqlString.Field<status> { }
            #endregion
        }

        #endregion

        #region Messages

        [PXLocalizable]
        public static class Messages
        {
            public const string CustomerRequired = "Customer is required.";
            public const string AmountRequired = "Transaction amount must be greater than zero.";
            public const string PaymentMethodRequired = "Payment method is required.";
            public const string CashAccountRequired = "Cash account is required.";
            public const string PeriodRequired = "Application period is required.";
            public const string NoInvoicesSelected = "Please select at least one invoice to apply payment.";
            public const string AmountExceedsInvoiceBalance = "Amount cannot exceed invoice balance of {0:N2}.";
            public const string TotalExceedsPaymentAmount = "Total applied amount cannot exceed payment amount of {0:N2}.";
            public const string AmountCannotBeNegative = "Amount cannot be negative.";
            public const string PaymentCreationFailed = "Failed to create payment: {0}";
            public const string PaymentNotFound = "Payment not found for this transaction.";
            public const string PaymentCreated = "Payment Created";
            public const string PaymentDescription = "Stanbic Payment {0}";
            public const string ViewPaymentTitle = "View Payment";
        }

        #endregion
    }
}