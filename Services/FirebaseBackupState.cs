namespace MoneyMap.Services
{
    public class FirebaseBackupState
    {
        private readonly object _lock = new object();

        private bool _hasPendingChanges;

        public bool HasPendingChanges // מציין אם קיימים שינויים שעדיין לא גובו
        {
            get
            {
                lock (_lock)
                {
                    return _hasPendingChanges;
                }
            }
        }

        public void MarkChanged()  // מסמן שיש שינויים חדשים שדורשים גיבוי
        {
            lock (_lock)
            {
                _hasPendingChanges = true;
            }
        }

        public BackupSnapshot CreateSnapshot()  // יוצר צילום מצב נוכחי של מצב הגיבוי
        {
            lock (_lock)
            {
                return new BackupSnapshot
                {
                    HasPendingChanges = _hasPendingChanges
                };
            }
        }

        public void Clear(BackupSnapshot snapshot)  // מנקה את הצילום 
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

    public class BackupSnapshot // מייצג מצב שמור של מערכת הגיבוי בנקודת זמן מסוימת
    {
        public bool HasPendingChanges { get; set; }

        public bool HasAny // מחזיר האם קיימים שינויים בצילום המצב
        {
            get
            {
                return HasPendingChanges;
            }
        }
    }
}