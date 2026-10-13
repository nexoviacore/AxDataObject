CREATE TABLE AxDataObjectInstance (
    TenantId varchar(128) NOT NULL,
    ObjectId varchar(64) NOT NULL,
    TransId varchar(128) NOT NULL,
    KeyField varchar(128) NULL,
    KeyValue varchar(512) NULL,
    RecordId bigint NULL,
    DataObjectJson json NOT NULL,
    ObjectVersion bigint NOT NULL CHECK (ObjectVersion > 0),
    StructDefVersion varchar(128) NOT NULL,
    OwnerUserName varchar(256) NOT NULL,
    InitiatorUserName varchar(256) NOT NULL,
    PersistDateTimeUtc timestamptz NOT NULL,
    LastAccessedDateTimeUtc timestamptz NOT NULL,
    LifecycleStatus varchar(64) NOT NULL,
    ApprovalStatus varchar(64) NOT NULL,
    PendingWith json NOT NULL,
    PendingFromDateTimeUtc timestamptz NULL,
    CurrentTaskId varchar(256) NULL,
    AxOpen integer NOT NULL CHECK (AxOpen IN (0, 1)),
    CONSTRAINT pk_axdoi PRIMARY KEY (TenantId, ObjectId)
);

CREATE INDEX ix_axdoi_object ON AxDataObjectInstance (ObjectId);
CREATE INDEX ix_axdoi_owner ON AxDataObjectInstance (TenantId, OwnerUserName);
CREATE INDEX ix_axdoi_record ON AxDataObjectInstance (TenantId, RecordId);
CREATE INDEX ix_axdoi_lifecycle ON AxDataObjectInstance (TenantId, LifecycleStatus);
CREATE INDEX ix_axdoi_approval ON AxDataObjectInstance (TenantId, ApprovalStatus);
CREATE INDEX ix_axdoi_task ON AxDataObjectInstance (TenantId, CurrentTaskId);
