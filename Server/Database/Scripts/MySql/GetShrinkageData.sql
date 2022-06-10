DROP PROCEDURE IF EXISTS GetShrinkageData;

CREATE PROCEDURE GetShrinkageData()
BEGIN

select
    dbLib.Name as Library,
    count(dbFile.Uid) as Items,
    sum(JSON_EXTRACT(dbFile.Data, '$.OriginalSize')) as OriginalSize,
    sum(JSON_EXTRACT(dbFile.Data, '$.FinalSize')) as FinalSize
from FileFlows.DbObject dbFile
         inner join FileFlows.DbObject dbLib on dbLib.Uid = JSON_UNQUOTE(JSON_EXTRACT(dbFile.Data, '$.Library.Uid'))
where
        dbFile.Type = 'FileFlows.Shared.Models.LibraryFile'
  and
        JSON_EXTRACT(dbFile.Data, '$.Status') = 1
group by dbLib.Name

END;