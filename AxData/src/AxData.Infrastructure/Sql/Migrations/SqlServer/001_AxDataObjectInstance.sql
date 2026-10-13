CREATE TABLE AxDataObjectInstance (
    TenantId nvarchar(128) NOT NULL,
    ObjectId nvarchar(64) NOT NULL,
    TransId nvarchar(128) NOT NULL,
    KeyField nvarchar(128) NULL,
    KeyValue nvarchar(512) NULL,
    RecordId bigint NULL,
    DataObjectJson nvarchar(max) NOT NULL,
    ObjectVersion bigint NOT NULL,
    StructDefVersion nvarchar(128) NOT NULL,
    OwnerUserName nvarchar(256) NOT NULL,
    InitiatorUserName nvarchar(256) NOT NULL,
    PersistDateTimeUtc datetime2 NOT NULL,
    LastAccessedDateTimeUtc datetime2 NOT NULL,
    LifecycleStatus nvarchar(64) NOT NULL,
    ApprovalStatus nvarchar(64) NOT NULL,
    PendingWith nvarchar(max) NOT NULL,
    PendingFromDateTimeUtc datetime2 NULL,
    CurrentTaskId nvarchar(256) NULL,
    AxOpen int NOT NULL,
    CONSTRAINT pk_axdoi PRIMARY KEY (TenantId, ObjectId),
    CONSTRAINT ck_axdoi_ver CHECK (ObjectVersion > 0),
    CONSTRAINT ck_axdoi_open CHECK (AxOpen IN (0, 1))
);

CREATE INDEX ix_axdoi_object ON AxDataObjectInstance (ObjectId);
CREATE INDEX ix_axdoi_owner ON AxDataObjectInstance (TenantId, OwnerUserName);
CREATE INDEX ix_axdoi_record ON AxDataObjectInstance (TenantId, RecordId);
CREATE INDEX ix_axdoi_lifecycle ON AxDataObjectInstance (TenantId, LifecycleStatus);
CREATE INDEX ix_axdoi_approval ON AxDataObjectInstance (TenantId, ApprovalStatus);
CREATE INDEX ix_axdoi_task ON AxDataObjectInstance (TenantId, CurrentTaskId);
