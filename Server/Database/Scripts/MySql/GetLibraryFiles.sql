DROP PROCEDURE IF EXISTS GetLibraryFiles;

CREATE PROCEDURE GetLibraryFiles(FileStatus int, IntervalIndex int, StartItem int, MaxItems int, NodeUid varchar(36) COLLATE utf8_unicode_ci, Overview bit)
BEGIN


    declare sOrder varchar(1000);
    declare sWhere varchar(500);
    declare allLibraries int;
    declare jsonLibraries text;

    drop table if exists tempLibraries;
    create temporary table tempLibraries
    select Uid, Name, js_ProcessingOrder as ProcessingOrder,
           js_Enabled as Enabled,
           js_Priority as Priority,
           case when substring(js_Schedule, IntervalIndex, 1) <> '0' then 0 else 1 end as Unscheduled
    from DbObject where Type = 'FileFlows.Shared.Models.Library';

    if NodeUid is not null and NodeUid <> '' then
        # need to remove libraries not processed by this node
        set allLibraries = (select JSON_EXTRACT(Data, '$.AllLibraries') from DbObject where Uid = NodeUid);
        set jsonLibraries = (select JSON_UNQUOTE(JSON_EXTRACT(Data, '$.Libraries[*].Uid')) from DbObject where Uid = NodeUid);

        SET SQL_SAFE_UPDATES = 0;
        delete from tempLibraries
        where not (
                    allLibraries = 1 or
                    (allLibraries = 0 and instr(jsonLibraries, Uid) > 1) or
                    (allLibraries = 2 and instr(jsonLibraries, Uid) < 1)
            );
        SET SQL_SAFE_UPDATES = 1;

    end if;


    if Overview = 1 then

        select Status, count(Uid) as Count
        from (
                 select case
                            when tempLibraries.Uid is null then 7
                            when js_Status = 0 and tempLibraries.Enabled = true and tempLibraries.Unscheduled = 0 then 0
                            when js_Status = 0 and tempLibraries.Enabled = false then -1
                            when js_Status = 0 then -2
                            else js_Status end as Status, DbObject.Uid
                 from DbObject left outer join tempLibraries on js_LibraryUid = tempLibraries.Uid
                 where Type = 'FileFlows.Shared.Models.LibraryFile'
             ) as tbl
        group by Status;

    else

        if FileStatus = 0 then
            set sWhere = ' and js_Status = 0 and tempLibraries.Enabled = true and tempLibraries.Unscheduled = 0';
        elseif FileStatus = -1 then -- out of schedule
            set sWhere = ' and js_Status  = 0 and tempLibraries.Enabled = true and tempLibraries.Unscheduled = 0';
        elseif FileStatus = -2 then -- disabled
            set sWhere = ' and js_Status  = 0 and tempLibraries.Enabled = false';
        elseif FileStatus = 7 then -- missing libraries
            set sWhere= ' and tempLibraries.Uid is null ';
        else
            set sWhere = CONCAT(' and js_Status = ', FileStatus);
        end if;

        if FileStatus = 0 or FileStatus = -1 then
            set sOrder = ' order by tempLibraries.Priority asc, 
		  case
			    when js_Status > 0 then 0
				when js_Order <> -1 then js_Order
				when tempLibraries.ProcessingOrder = 1 then FLOOR(RAND()*(10000)+1000) # random
				when tempLibraries.ProcessingOrder = 2 then 1000 + (js_OriginalSize / 1000000) #smallest first
				when tempLibraries.ProcessingOrder = 3 then 10000000 - (js_OriginalSize / 10000)  #largest first
				when tempLibraries.ProcessingOrder = 4 then now() - DateCreated #newest first
				else UNIX_TIMESTAMP(DateCreated)
				end';
        elseif FileStatus = 2 then
            set sOrder = ' order by js_ProcessingStarted asc ';
        elseif FileStatus = 1 or FileStatus = 4 then
            set sOrder = ' order by js_ProcessingEnded desc ';
        else
            set sOrder = ' order by DateCreated desc ';
        end if;
        
        select @queryString;

        SET @queryString = CONCAT(
                'select DbObject.Uid, DbObject.Name, DbObject.Type, DbObject.DateCreated, DbObject.DateModified, DbObject.Data ',
                ' from DbObject left outer join tempLibraries on js_LibraryUid = tempLibraries.Uid ',
                ' where Type = ''FileFlows.Shared.Models.LibraryFile'' ',
                sWhere, ' ', sOrder, ' limit ', StartItem, ', ', MaxItems, '; '
            );

        prepare stmt from @queryString;
        execute stmt;
        deallocate prepare stmt;

    end if;



END;