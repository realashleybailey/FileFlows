DROP PROCEDURE IF EXISTS GetRecentlyFinished;

CREATE PROCEDURE GetRecentlyFinished(MaxItems int) 
BEGIN

    select * from DbObject dblf
    where Type = 'FileFlows.Shared.Models.LibraryFile' and JSON_EXTRACT(Data, '$.Status') = 1
    order by cast(substring(JSON_EXTRACT(Data, '$.ProcessingEnded'),0, 24) as datetime) desc
    limit MaxItems;
    
END;