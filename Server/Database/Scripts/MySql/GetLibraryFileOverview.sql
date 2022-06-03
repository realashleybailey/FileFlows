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


    set libsDisabled=(
        select GROUP_CONCAT(Uid,',') from DbObject
        where type = 'FileFlows.Shared.Models.Library'
        and JSON_EXTRACT(Data,'$.Enabled') = '0'
    );
    if libsDisabled is null then
        set libsDisabled  = '';
end if;          
    set libsOutOfSchedule = (
        select GROUP_CONCAT(Uid,',') from DbObject
        where type = 'FileFlows.Shared.Models.Library'
        and (substring(JSON_UNQUOTE(JSON_Extract(Data, '$.Schedule')), IntervalIndex, 1) = '0')
    );
    if libsOutOfSchedule is null then
        set libsOutOfSchedule  = '';
end if;

set cntDisabled = (
    select count(Uid) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
    and length(libsDisabled) > 0
    and JSON_EXTRACT(Data, '$.Status') = 0
    and libsDisabled not like ('%' + JSON_UNQUOTE(JSON_EXTRACT(Data, '$.Library.Uid')) + '%')
    );

set cntOutOfSchedule = (select count(Uid) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
    and length(libsOutOfSchedule) > 0
    and JSON_EXTRACT(Data, '$.Status') = 0
    and (length(libsDisabled) < 1 or libsDisabled not like ('%' + JSON_UNQUOTE(JSON_EXTRACT(Data, '$.Library.Uid')) + '%'))    
    and libsOutOfSchedule like ('%' + JSON_UNQUOTE(JSON_EXTRACT(Data, '$.Library.Uid')) + '%')
    );

set cntUnprocessed = (select count(Uid) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
    and JSON_EXTRACT(Data, '$.Status') = 0
    and (length(libsDisabled) < 1 or libsDisabled not like ('%' + JSON_UNQUOTE(JSON_EXTRACT(Data, '$.Library.Uid')) + '%'))
    and (length(libsOutOfSchedule) < 1 or libsOutOfSchedule not like ('%' + JSON_UNQUOTE(JSON_EXTRACT(Data, '$.Library.Uid')) + '%'))
    );


set cntProcessed = (select count(Uid) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
    and JSON_EXTRACT(Data, '$.Status') = 1
    );

set cntProcessing = (select count(Uid) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
    and JSON_EXTRACT(Data, '$.Status') = 2
    );

set cntFlowNotFound = (select count(Uid) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
    and JSON_EXTRACT(Data, '$.Status') = 3
    );

set cntProcessingFailed = (select count(Uid) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
    and JSON_EXTRACT(Data, '$.Status') = 4
    );

set cntDuplicate = (select count(Uid) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
    and JSON_EXTRACT(Data, '$.Status') = 5
    );

set cntMappingIssue = (select count(Uid) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
    and JSON_EXTRACT(Data, '$.Status') = 6
    );

select cntDisabled as Disabled, cntOutOfSchedule as OutOfSchedule, cntUnprocessed as Unprocessed,
       cntProcessed as Processed, cntProcessing as Processing, cntFlowNotFound as FlowNotFound,
       cntProcessingFailed as ProcessingFailed, cntDuplicate as Duplicate, cntMappingIssue as MappingIssue;


END;