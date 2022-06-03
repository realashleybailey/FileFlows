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
    
    if Status = -2 then -- disabled
    select * from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
                             and JSON_EXTRACT(Data, '$.Status') = 0
                             and libsDisabled not like ('%' + JSON_UNQUOTE(JSON_EXTRACT(Data, '$.Library.Uid')) + '%');
    
    elseif Status = -1 then -- out of schedule
    select * from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
                             and JSON_EXTRACT(Data, '$.Status') = 0
                             and libsDisabled not like ('%' + JSON_UNQUOTE(JSON_EXTRACT(Data, '$.Library.Uid')) + '%')
                             and libsOutOfSchedule like ('%' + JSON_UNQUOTE(JSON_EXTRACT(Data, '$.Library.Uid')) + '%');
    
    else
    select * from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
                             and JSON_EXTRACT(Data, '$.Status') = 0
                             and libsDisabled not like ('%' + JSON_UNQUOTE(JSON_EXTRACT(Data, '$.Library.Uid')) + '%')
                             and libsOutOfSchedule not like ('%' + JSON_UNQUOTE(JSON_EXTRACT(Data, '$.Library.Uid')) + '%');
    end if;
    end if;

END;