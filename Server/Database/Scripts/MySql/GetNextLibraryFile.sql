DROP PROCEDURE IF EXISTS GetNextLibraryFile;

CREATE PROCEDURE GetNextLibraryFile(NodeUid varchar(36), WorkerUid varchar(36), IntervalIndex int, StartDate varchar(50)) 
NextLibraryFile:BEGIN  

	declare nodeName varchar(255);
	declare tData mediumtext;
	declare tUid varchar(36);
    declare allLibraries int;
    declare jsonLibraries text;
    
    set nodeName = '';
    set tData = '';
    set tUid = '';
    set allLibraries = 0;
    set jsonLibraries = '';
            
    # get node name, only if in schedule    
	set nodeName = (
		select Name from DbObject 
		where Uid = NodeUid and Type = 'FileFlows.Shared.Models.ProcessingNode'
		and JSON_EXTRACT(Data, '$.Enabled') = 1
		and (JSON_UNQUOTE(JSON_EXTRACT(Data, '$.Schedule')) = '' or substring(JSON_UNQUOTE(JSON_Extract(Data, '$.Schedule')), IntervalIndex, 1) = '1')
        limit 1
    );
	if nodeName is NULL OR nodeName = '' then
		LEAVE NextLibraryFile;		
    end if;
        
    # get supported libraries 
    drop table if exists tempLibraries;
    
    set allLibraries = (select JSON_EXTRACT(Data, '$.AllLibraries') from DbObject where Uid = NodeUid);
    set jsonLibraries = (select JSON_UNQUOTE(JSON_EXTRACT(Data, '$.Libraries[*].Uid')) from DbObject where Uid = NodeUid);
        
    create temporary table tempLibraries 
    select Uid, Name, JSON_EXTRACT(Data, '$.Priority') as Priority from DbObject 
    where Type = 'FileFlows.Shared.Models.Library'
    and JSON_EXTRACT(Data, '$.Enabled') = 1
    and (JSON_UNQUOTE(JSON_EXTRACT(Data, '$.Schedule')) = '' or substring(JSON_UNQUOTE(JSON_Extract(Data, '$.Schedule')), IntervalIndex, 1) = '1')
    and (allLibraries = 1 OR (allLibraries = 0 and instr(jsonLibraries, Uid) > 0) OR (allLibraries = 2 and instr(jsonLibraries, Uid) < 1));
                   
        
	set tUid = '';
    set tData = '';

    set tUid = (
		select DbObject.Uid
        from DbObject inner join tempLibraries 
			on JSON_UNQUOTE(JSON_EXTRACT(DbObject.Data, '$.Library.Uid')) = tempLibraries.Uid
        where Type = 'FileFlows.Shared.Models.LibraryFile'
		and JSON_EXTRACT(DbObject.Data, '$.Status') = 0
        #and JSON_EXTRACT(DbObject.Data, '$.Order') > 0
		order by 
		case
		when JSON_EXTRACT(DbObject.Data, '$.Order') > 0 then JSON_EXTRACT(Data, '$.Order')
		else 1000000
		end asc, 
        tempLibraries.Priority desc,
        DbObject.DateCreated asc
		limit 1
    );
        
    
    drop table if exists tempLibraries;

	if tUid is NULL OR tUid = '' then
		LEAVE NextLibraryFile;
    end if;
        
    set tData = (select Data from DbObject where Uid = tUid);
    
    set tData = (select JSON_SET(tData, '$.Status', 2, '$.Node', JSON_OBJECT('NodeUid', NodeUid, 'Name', nodeName), '$.ProcessingStarted', StartDate, '$.WorkerUid', WorkerUid));
	    
    update DbObject set Data = tData where Uid = tUid;
        
	select * from DbObject where Uid = tUid;

END;