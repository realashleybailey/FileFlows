DROP PROCEDURE IF EXISTS GetLibraryFileOverview;

CREATE PROCEDURE GetLibraryFileOverview(IntervalIndex int)
BEGIN
    
declare cntDisabled int;
declare cntOutOfSchedule int;
declare cntUnprocessed int;
declare cntProcessed int;
declare cntProcessing int;
declare cntFlowNotFound int;
declare cntProcessingFailed int;
declare cntDuplicate int;
declare cntMappingIssue int;
declare libsDisabled text;
declare libsOutOfSchedule text;

set libsDisabled = (select STRING_AGG(Uid,',') from DbObject
where type = 'FileFlows.Shared.Models.Library'
  and JSON_EXTRACT(Data,'$.Enabled') = '0'
);


set libsOutOfSchedule = (
select STRING_AGG(Uid,',') from DbObject
where type = 'FileFlows.Shared.Models.Library'
  and (JSON_EXTRACT(Data,'$.Schedule') = null or JSON_EXTRACT(Data,'$.Schedule') = '' or
       substring(JSON_EXTRACT(Data, '$.Schedule'), IntervalIndex, 1) = '1')
);


set cntDisabled = (
    select count (Uid) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
    and JSON_EXTRACT(Data, '$.Status') = 0
    and charindex(JSON_EXTRACT(Data, '$.Library.Uid'), libsDisabled) > 0
    and charindex(JSON_EXTRACT(Data, '$.Library.Uid'), libsOutOfSchedule) < 1
    );

set cntOutOfSchedule = (select count (Uid) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
    and JSON_EXTRACT(Data, '$.Status') = 0
    and charindex(JSON_EXTRACT(Data, '$.Library.Uid'), libsDisabled) < 1
    and charindex(JSON_EXTRACT(Data, '$.Library.Uid'), libsOutOfSchedule) > 1
    );

set cntUnprocessed = (select count (Uid) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
    and JSON_EXTRACT(Data, '$.Status') = 0
    and charindex(JSON_EXTRACT(Data, '$.Library.Uid'), libsDisabled) < 1
    and charindex(JSON_EXTRACT(Data, '$.Library.Uid'), libsOutOfSchedule) < 1
    );


set cntProcessed = (select count (Uid) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
    and JSON_EXTRACT(Data, '$.Status') = 1
    );

set cntProcessing = (select count (Uid) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
    and JSON_EXTRACT(Data, '$.Status') = 2
    );

set cntFlowNotFound = (select count (Uid) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
    and JSON_EXTRACT(Data, '$.Status') = 3
    );

set cntProcessingFailed = (select count (Uid) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
    and JSON_EXTRACT(Data, '$.Status') = 4
    );

set cntDuplicate = (select count (Uid) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
    and JSON_EXTRACT(Data, '$.Status') = 5
    );

set cntMappingIssue = (select count (Uid) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
    and JSON_EXTRACT(Data, '$.Status') = 6
    );

select cntDisabled as Disabled, cntOutOfSchedule as OutOfSchedule, cntUnprocessed as Unprocessed,
       cntProcessed as Processed, cntProcessing as Processing, cntFlowNotFound as FlowNotFound,
       cntProcessingFailed as ProcessingFailed, cntDuplicate as Duplicate, cntMappingIssue as MappingIssue;


END;