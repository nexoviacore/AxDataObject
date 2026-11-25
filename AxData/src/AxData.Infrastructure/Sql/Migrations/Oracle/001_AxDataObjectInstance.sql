CREATE TABLE AxDataObjectInstance (
    TenantId VARCHAR2(128 CHAR) NOT NULL,
    ObjectId VARCHAR2(64 CHAR) NOT NULL,
    TransId VARCHAR2(128 CHAR) NOT NULL,
    RecordId NUMBER(19) NULL,
    DataObjectJson CLOB NOT NULL,
    InitiatorUserName VARCHAR2(256 CHAR) NOT NULL,
    PersistDateTimeUtc TIMESTAMP NOT NULL,
    LastAccessedDateTimeUtc TIMESTAMP NOT NULL,
    LifecycleStatus VARCHAR2(64 CHAR) NOT NULL,
    ApprovalStatus VARCHAR2(64 CHAR) NOT NULL,
    PendingWith CLOB NOT NULL,
    PendingFromDateTimeUtc TIMESTAMP NULL,
    CurrentTaskId VARCHAR2(256 CHAR) NULL,
    AxOpen NUMBER(1) NOT NULL,
    CONSTRAINT pk_axdoi PRIMARY KEY (TenantId, ObjectId),
    CONSTRAINT ck_axdoi_open CHECK (AxOpen IN (0, 1))
);

CREATE INDEX ix_axdoi_object ON AxDataObjectInstance (ObjectId);
CREATE INDEX ix_axdoi_record ON AxDataObjectInstance (TenantId, RecordId);
CREATE INDEX ix_axdoi_lifecycle ON AxDataObjectInstance (TenantId, LifecycleStatus);
CREATE INDEX ix_axdoi_approval ON AxDataObjectInstance (TenantId, ApprovalStatus);
CREATE INDEX ix_axdoi_task ON AxDataObjectInstance (TenantId, CurrentTaskId);
