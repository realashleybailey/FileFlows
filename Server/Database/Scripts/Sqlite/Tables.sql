CREATE TABLE DbObject
(
    Uid             VARCHAR(36)        NOT NULL          PRIMARY KEY,
    Name            VARCHAR(1024)      NOT NULL,
    Type            VARCHAR(255)       NOT NULL,
    DateCreated     datetime           default           current_timestamp,
    DateModified    datetime           default           current_timestamp,
    Data            TEXT               NOT NULL
);
ALTER TABLE DbObject ADD INDEX (Type);
ALTER TABLE DbObject ADD INDEX (Name);

CREATE TABLE DbStatistic
(
    LogDate         datetime,
    Name            varchar(100)       NOT NULL,
    Type            int                NOT NULL,
    StringValue     TEXT               NOT NULL,
    NumberValue     REAL               NOT NULL
);


CREATE TABLE RevisionedObject
(
    Uid             VARCHAR(36)        NOT NULL          PRIMARY KEY,
    RevisionUid     VARCHAR(36)        NOT NULL,
    RevisionName    VARCHAR(1024)      NOT NULL,
    RevisionType    VARCHAR(255)       NOT NULL,
    RevisionDate    datetime           default           current_timestamp,
    RevisionCreated datetime           default           current_timestamp,
    RevisionData    TEXT               NOT NULL
);

CREATE TABLE LibraryFile
(
    -- common fields from DbObject
    Uid                 VARCHAR(36)        NOT NULL          PRIMARY KEY,
    Name                VARCHAR(1024)      NOT NULL,
    DateCreated         datetime           default           now(),
    DateModified        datetime           default           now(),
    
    -- properties
    RelativePath        VARCHAR(1024)      NOT NULL,
    Status              int                NOT NULL,
    ProcessingOrder     int                NOT NULL,
    Fingerprint         VARCHAR(255)       NOT NULL,
    Enabled             boolean            not null,
    IsDirectory         boolean            not null,
    Priority            int                not null,
    
    -- size
    OriginalSize        bigint             NOT NULL,
    FinalSize           bigint             NOT NULL,
    
    -- dates 
    CreationTime        datetime           default           now(),
    LastWriteTime       datetime           default           now(),
    HoldUntil           datetime           default           '1970-01-01 00:00:01',
    ProcessingStarted   datetime           default           now()      NOT NULL,
    ProcessingEnded     datetime           default           now()      NOT NULL,
    
    -- references
    LibraryUid          varchar(36)        NOT NULL,
    LibraryName         VARCHAR(100)       NOT NULL,
    FlowUid             varchar(36)        NOT NULL,
    FlowName            VARCHAR(100)       NOT NULL,
    DuplicateUid        varchar(36)        NOT NULL,
    DuplicateName       VARCHAR(1024)      NOT NULL,
    NodeUid             varchar(36)        NOT NULL,
    NodeName            VARCHAR(100)       NOT NULL,
    WorkerUid           varchar(36)        NOT NULL,

    -- output
    OutputPath          VARCHAR(1024)      NOT NULL,
    NoLongerExistsAfterProcessing          boolean                      not null,

    -- json data
    OriginalMetadata    TEXT               NOT NULL,
    FinalMetadata       TEXT               NOT NULL,
    ExecutedNodes       TEXT               NOT NULL
);

ALTER TABLE LibraryFile ADD INDEX (Status);
ALTER TABLE LibraryFile ADD INDEX (DateModified);
-- index to make library file status/skybox faster
ALTER TABLE LibraryFile ADD INDEX (Status, HoldUntil, LibraryUid);