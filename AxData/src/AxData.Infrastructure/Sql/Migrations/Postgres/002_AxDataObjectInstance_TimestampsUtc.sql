ALTER TABLE AxDataObjectInstance
    ALTER COLUMN PersistDateTimeUtc TYPE timestamptz
        USING PersistDateTimeUtc AT TIME ZONE 'UTC',
    ALTER COLUMN LastAccessedDateTimeUtc TYPE timestamptz
        USING LastAccessedDateTimeUtc AT TIME ZONE 'UTC',
    ALTER COLUMN PendingFromDateTimeUtc TYPE timestamptz
        USING PendingFromDateTimeUtc AT TIME ZONE 'UTC';
