namespace MoneyMap.Services
{
    public class FirebaseBackupState
    {
        private readonly object _lock = new object();

        private bool _hasPendingChanges;

        public bool HasPendingChanges
        {
            get
            {
                lock (_lock)
                {
                    return _hasPendingChanges;
                }
            }
        }

        public void MarkChanged()
        {
            lock (_lock)
            {
                _hasPendingChanges = true;
            }
        }

        public BackupSnapshot CreateSnapshot()
        {
            lock (_lock)
            {
                return new BackupSnapshot
                {
                    HasPendingChanges = _hasPendingChanges
                };
            }
        }

        public void Clear(BackupSnapshot snapshot)
        {
            if (snapshot == null) return;

            lock (_lock)
            {
                if (snapshot.HasPendingChanges)
                {
                    _hasPendingChanges = false;
                }
            }
        }
    }

    public class BackupSnapshot
    {
        public bool HasPendingChanges { get; set; }

        public bool HasAny
        {
            get
            {
                return HasPendingChanges;
            }
        }
    }
}