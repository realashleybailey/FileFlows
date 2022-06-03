DROP PROCEDURE IF EXISTS GetLibraryFiles;

CREATE PROCEDURE GetLibraryFiles(IntervalIndex int, Status int)
BEGIN
    
    declare libsDisabled text;
    declare libsOutOfSchedule text;
    
    if Status > 0 then
        select * from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
                             and JSON_EXTRACT(Data, '$.Status') = Status;
    
    else
    
    set libsDisabled=(
        select GROUP_CONCAT(Uid,',') from DbObject
        where type = 'FileFlows.Shared.Models.Library'
      and JSON_EXTRACT(Data,'$.Enabled') = '0'
    );
        
    
    
    set libsOutOfSchedule = (
        select GROUP_CONCAT(Uid,',') from DbObject
        where type = 'FileFlows.Shared.Models.Library'
      and (substring(JSON_UNQUOTE(JSON_Extract(Data, '$.Schedule')), IntervalIndex, 1) = '0')
    );
    
    if Status = -2 then -- disabled
    select * from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
                             and JSON_EXTRACT(Data, '$.Status') = 0
                             and instr(JSON_EXTRACT(Data, '$.Library.Uid'), libsDisabled) > 0
                             and instr(JSON_EXTRACT(Data, '$.Library.Uid'), libsOutOfSchedule) < 1;
    
    elseif Status = -1 then -- out of schedule
    select * from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
                             and JSON_EXTRACT(Data, '$.Status') = 0
                             and instr(JSON_EXTRACT(Data, '$.Library.Uid'), libsDisabled) < 1
                             and instr(JSON_EXTRACT(Data, '$.Library.Uid'), libsOutOfSchedule) > 1;
    
    else
    select * from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
                             and JSON_EXTRACT(Data, '$.Status') = 0
                             and instr(JSON_EXTRACT(Data, '$.Library.Uid'), libsDisabled) < 1
                             and instr(JSON_EXTRACT(Data, '$.Library.Uid'), libsOutOfSchedule) < 1;
    end if;
    end if;

END;