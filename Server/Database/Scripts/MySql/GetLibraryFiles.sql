DROP PROCEDURE IF EXISTS GetLibraryFiles;

CREATE PROCEDURE GetLibraryFiles(IntervalIndex int, Status int)
BEGIN
    
    declare libsDisabled as varchar(max)
    declare libsOutOfSchedule as varchar(max)
    
    if Status > 0 then
        select * from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
        and JSON_EXTRACT([Data], '$.Status') = Status
    else
    
        select libsDisabled=STRING_AGG(cast([Uid] as varchar(36)),',') from DbObject
        where type = 'FileFlows.Shared.Models.Library'
            and JSON_EXTRACT([Data],'$.Enabled') = '0'
        
        
        select libsOutOfSchedule=STRING_AGG(cast([Uid] as varchar(36)),',') from DbObject
        where type = 'FileFlows.Shared.Models.Library'
            and (JSON_EXTRACT([Data],'$.Schedule') = null or JSON_EXTRACT([Data],'$.Schedule') = '' or
            substring(JSON_EXTRACT([Data], '$.Schedule'), IntervalIndex, 1) = '1')
        
        if Status = -2 then -- disabled
            select * from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
            and JSON_EXTRACT([Data], '$.Status') = 0
            and charindex(JSON_EXTRACT([Data], '$.Library.Uid'), libsDisabled) > 0
            and charindex(JSON_EXTRACT([Data], '$.Library.Uid'), libsOutOfSchedule) < 1
        
        else if Status = -1 then -- out of schedule
            select * from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
            and JSON_EXTRACT([Data], '$.Status') = 0
            and charindex(JSON_EXTRACT([Data], '$.Library.Uid'), libsDisabled) < 1
            and charindex(JSON_EXTRACT([Data], '$.Library.Uid'), libsOutOfSchedule) > 1
        
        else
            select * from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
            and JSON_EXTRACT([Data], '$.Status') = 0
            and charindex(JSON_EXTRACT([Data], '$.Library.Uid'), libsDisabled) < 1
            and charindex(JSON_EXTRACT([Data], '$.Library.Uid'), libsOutOfSchedule) < 1
        end if;
    end if;

GO