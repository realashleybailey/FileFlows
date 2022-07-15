DROP PROCEDURE IF EXISTS SearchLibraryFiles;

CREATE PROCEDURE SearchLibraryFiles(LibraryName varchar(100) COLLATE utf8_unicode_ci, SearchText varchar(100) COLLATE utf8_unicode_ci,  FromDate Date, ToDate Date, MaxRows int)
BEGIN

    select Uid, Name, Type, DateCreated, DateModified, Data
    from DbObject where Type = 'FileFlows.Shared.Models.LibraryFile'
        and( DateCreated between FromDate and ToDate)
        and match(Name) against (SearchText IN BOOLEAN MODE)
    limit MaxRows;

END;