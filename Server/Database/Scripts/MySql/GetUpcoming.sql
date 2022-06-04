DROP PROCEDURE IF EXISTS GetUpcoming;

CREATE PROCEDURE GetUpcoming(IntervalIndex int, MaxItems int) 
BEGIN  

    drop table if exists tempLibraries;

    create temporary table tempLibraries
    select Uid, Name, JSON_EXTRACT(Data, '$.Priority') as Priority from DbObject
    where Type = 'FileFlows.Shared.Models.Library'
      and JSON_EXTRACT(Data, '$.Enabled') = 1
      and (JSON_UNQUOTE(JSON_EXTRACT(Data, '$.Schedule')) = '' or substring(JSON_UNQUOTE(JSON_Extract(Data, '$.Schedule')), IntervalIndex, 1) = '1');
    
    select dblf.* from DbObject dblf
                           inner join tempLibraries dbLib on JSON_UNQUOTE(JSON_EXTRACT(dblf.Data, '$.Library.Uid')) = dbLib.Uid
    where dblf.Type = 'FileFlows.Shared.Models.LibraryFile' and JSON_EXTRACT(dblf.Data, '$.Status') = 0
    order by case
             when JSON_EXTRACT(dblf.Data, '$.Order') > 0 then JSON_EXTRACT(dblf.Data, '$.Order')
             else 10000 * -dbLib.Priority
             end asc
    limit MaxItems;
    
END;