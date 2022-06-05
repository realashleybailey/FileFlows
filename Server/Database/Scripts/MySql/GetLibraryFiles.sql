DROP PROCEDURE IF EXISTS GetLibraryFiles;

CREATE PROCEDURE GetLibraryFiles(FileStatus int, IntervalIndex int, StartItem int, MaxItems int, NodeUid varchar(36), Overview bit)
BEGIN


	

    declare sOrder varchar(500);
    declare allLibraries int;
    declare jsonLibraries text;

drop table if exists tempLibraries;
create temporary table tempLibraries
select Uid, Name,
       case when JSON_EXTRACT(Data, '$.Enabled') = 1 then 1
            else 0 end
                                                                                                                      as Enabled,
       convert(JSON_EXTRACT(Data, '$.Priority'), signed) as Priority,
       case when substring(JSON_UNQUOTE(JSON_Extract(Data, '$.Schedule')), IntervalIndex, 1) <> '0' then 0 else 1 end as Unscheduled
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
           when JSON_EXTRACT(Data, '$.Status') <> 0 then convert(JSON_EXTRACT(Data, '$.Status'), signed)
           when tempLibraries.Enabled = 0 then -2
           when tempLibraries.Unscheduled = 1 then -1
           else 0
           end as Status,
       case
           when JSON_EXTRACT(Data, '$.Order') <> -1 then convert(JSON_EXTRACT(Data, '$.Order'), signed)
           else  10000 - (tempLibraries.Priority * 100)
           end as Priority,
       convert(substring(JSON_UNQUOTE(JSON_EXTRACT(Data, '$.ProcessingStarted')), 1, 23), datetime) as ProcessingStarted,
       convert(substring(JSON_UNQUOTE(JSON_EXTRACT(Data, '$.ProcessingEnded')), 1, 23), datetime) as ProcessingEnded
from DbObject inner join tempLibraries on JSON_UNQUOTE(JSON_EXTRACT(Data, '$.Library.Uid')) = tempLibraries.Uid
where Type = 'FileFlows.Shared.Models.LibraryFile';


if Overview = 1 then

select tempFiles.Status, Count(Uid) as Count from tempFiles
group by tempFiles.Status;

else
	if FileStatus = 0 or FileStatus = -1 then
	  set sOrder = ' order by Priority asc ';
	elseif FileStatus = 2 then
		  set sOrder = ' order by ProcessingStarted asc ';
	elseif FileStatus = 1 or FileStatus = 4 then
		  set sOrder = ' order by ProcessingEnded desc ';
else
		  set sOrder = ' order by DateCreated desc ';
end if;
    
		SET @queryString = CONCAT(
			'select dblf.Uid, dblf.Name, dblf.Type, dblf.DateCreated, dblf.DateModified, dblf.Data from tempFiles dblf ',
            #'select dblf.* from tempFiles dblf ',
			' where dblf.Status = ', FileStatus,
			' ', sOrder, ' limit ', StartItem, ', ', MaxItems, '; '
		);
prepare stmt from @queryString;
execute stmt;
deallocate prepare stmt;

end if;




END;