namespace MoneyMap.Services
{
    public class FirebaseBackupState
    {
        private readonly object _lock = new object();

        public bool CategoriesChanged { get; private set; }
        public bool BudgetsChanged { get; private set; }
        public bool ExpensesChanged { get; private set; }
        public bool InvestmentsChanged { get; private set; }
        public bool IncomesChanged { get; private set; }

        public bool HasPendingChanges
        {
            get
            {
                lock (_lock)
                {
                    return CategoriesChanged ||
                           BudgetsChanged ||
                           ExpensesChanged ||
                           InvestmentsChanged ||
                           IncomesChanged;
                }
            }
        }

        public void MarkCategoriesChanged()
        {
            lock (_lock) CategoriesChanged = true;
        }

        public void MarkBudgetsChanged()
        {
            lock (_lock) BudgetsChanged = true;
        }

        public void MarkExpensesChanged()
        {
            lock (_lock) ExpensesChanged = true;
        }

        public void MarkInvestmentsChanged()
        {
            lock (_lock) InvestmentsChanged = true;
        }

        public void MarkIncomesChanged()
        {
            lock (_lock) IncomesChanged = true;
        }

        public BackupSnapshot CreateSnapshot()
        {
            lock (_lock)
            {
                return new BackupSnapshot
                {
                    CategoriesChanged = CategoriesChanged,
                    BudgetsChanged = BudgetsChanged,
                    ExpensesChanged = ExpensesChanged,
                    InvestmentsChanged = InvestmentsChanged,
                    IncomesChanged = IncomesChanged
                };
            }
        }

        public void Clear(BackupSnapshot snapshot)
        {
            if (snapshot == null) return;

            lock (_lock)
            {
                if (snapshot.CategoriesChanged) CategoriesChanged = false;
                if (snapshot.BudgetsChanged) BudgetsChanged = false;
                if (snapshot.ExpensesChanged) ExpensesChanged = false;
                if (snapshot.InvestmentsChanged) InvestmentsChanged = false;
                if (snapshot.IncomesChanged) IncomesChanged = false;
            }
        }
    }

    public class BackupSnapshot
    {
        public bool CategoriesChanged { get; set; }
        public bool BudgetsChanged { get; set; }
        public bool ExpensesChanged { get; set; }
        public bool InvestmentsChanged { get; set; }
        public bool IncomesChanged { get; set; }

        public bool HasAny =>
            CategoriesChanged ||
            BudgetsChanged ||
            ExpensesChanged ||
            InvestmentsChanged ||
            IncomesChanged;
    }
}