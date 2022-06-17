DROP PROCEDURE IF EXISTS GetLibraryFiles;

CREATE PROCEDURE GetLibraryFiles(FileStatus int, IntervalIndex int, StartItem int, MaxItems int, NodeUid varchar(36) COLLATE utf8_unicode_ci, Overview bit)
BEGIN


    declare sOrder varchar(500);
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

    drop table if exists tempFiles;
    create temporary table tempFiles
    select tempLibraries.Uid as LibraryUid, DbObject.*,
           case
               when js_Status <> 0 then js_Status
               when tempLibraries.Enabled = false then -2
               when tempLibraries.Unscheduled = 1 then -1
               else 0
               end as Status,
           case
               when js_Order <> -1 then js_Order
               when tempLibraries.ProcessingOrder = 1 then FLOOR(RAND()*(10000)+1000) # random
               when tempLibraries.ProcessingOrder = 2 then 1000 + (js_OriginalSize / 1000000) #smallest first
               when tempLibraries.ProcessingOrder = 3 then 10000000 - (js_OriginalSize / 10000)  #largest first
               when tempLibraries.ProcessingOrder = 4 then now() - DateCreated #newest first
               else 10000
               end as Priority,
           tempLibraries.Priority as LibraryPriority,
           js_ProcessingStarted as ProcessingStarted,
           js_ProcessingEnded as ProcessingEnded
    from DbObject inner join tempLibraries on js_LibraryUid = tempLibraries.Uid
    where Type = 'FileFlows.Shared.Models.LibraryFile';

    if Overview = 1 then

        select tempFiles.Status, Count(Uid) as Count from tempFiles
        group by tempFiles.Status;

    else
        if FileStatus = 0 or FileStatus = -1 then
            set sOrder = ' order by LibraryPriority asc, Priority asc ';
        elseif FileStatus = 2 then
            set sOrder = ' order by ProcessingStarted asc ';
        elseif FileStatus = 1 or FileStatus = 4 then
            set sOrder = ' order by ProcessingEnded desc ';
        else
            set sOrder = ' order by DateCreated desc ';
        end if;

        SET @queryString = CONCAT(
                'select dblf.Uid, dblf.Name, dblf.Type, dblf.DateCreated, dblf.DateModified, dblf.Data from tempFiles dblf ',
            #'select dblf.*, js_OriginalSize as OriginalSize from tempFiles dblf ',
                ' where dblf.Status = ', FileStatus,
                ' ', sOrder, ' limit ', StartItem, ', ', MaxItems, '; '
            );

        prepare stmt from @queryString;
        execute stmt;
        deallocate prepare stmt;

    end if;


END;