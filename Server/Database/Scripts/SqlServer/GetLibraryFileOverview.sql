DROP PROCEDURE IF EXISTS GetLibraryFileOverview;
GO

CREATE PROCEDURE GetLibraryFileOverview
    @IntervalIndex int
AS


declare @libsDisabled as varchar(max)
select @libsDisabled=isnull(STRING_AGG(cast([Uid] as varchar(36)),','), '') from DbObject
where type = 'FileFlows.Shared.Models.Library'
  and json_value([Data],'$.Enabled') = '0'


declare @libsOutOfSchedule as varchar(max)
select @libsOutOfSchedule=isnull(STRING_AGG(cast([Uid] as varchar(36)),','), '') from DbObject
where type = 'FileFlows.Shared.Models.Library'
  and not (
            json_value([Data],'$.Schedule') = null or
            json_value([Data],'$.Schedule') = '' or
            substring(json_value([Data], '$.Schedule'), @IntervalIndex, 1) <> '0'
    )


declare @cntDisabled as int
declare @cntOutOfSchedule as int
declare @cntUnprocessed as int
declare @cntProcessed as int
declare @cntProcessing as int
declare @cntFlowNotFound as int
declare @cntProcessingFailed as int
declare @cntDuplicate as int
declare @cntMappingIssue as int

select @cntDisabled=count([Uid]) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
                                                 and json_value([Data], '$.Status') = 0
                                                 and charindex(json_value([Data], '$.Library.Uid'), @libsDisabled) > 0
                                                 and charindex(json_value([Data], '$.Library.Uid'), @libsOutOfSchedule) < 1

select @cntOutOfSchedule=count([Uid]) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
                                                      and json_value([Data], '$.Status') = 0
                                                      and charindex(json_value([Data], '$.Library.Uid'), @libsDisabled) < 1
                                                      and charindex(json_value([Data], '$.Library.Uid'), @libsOutOfSchedule) > 1

select @cntUnprocessed=count([Uid]) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
                                                    and json_value([Data], '$.Status') = 0
                                                    and charindex(json_value([Data], '$.Library.Uid'), @libsDisabled) < 1
                                                    and charindex(json_value([Data], '$.Library.Uid'), @libsOutOfSchedule) < 1


select @cntProcessed=count([Uid]) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
                                                  and json_value([Data], '$.Status') = 1

select @cntProcessing=count([Uid]) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
                                                   and json_value([Data], '$.Status') = 2

select @cntFlowNotFound=count([Uid]) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
                                                     and json_value([Data], '$.Status') = 3

select @cntProcessingFailed=count([Uid]) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
                                                         and json_value([Data], '$.Status') = 4

select @cntDuplicate=count([Uid]) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
                                                  and json_value([Data], '$.Status') = 5

select @cntMappingIssue=count([Uid]) from DbObject where type = 'FileFlows.Shared.Models.LibraryFile'
                                                     and json_value([Data], '$.Status') = 6

select [Disabled]=@cntDisabled, [OutOfSchedule]=@cntOutOfSchedule, [Unprocessed]=@cntUnprocessed,
    [Processed]=@cntProcessed, [Processing]=@cntProcessing, [FlowNotFound]=@cntFlowNotFound,
    [ProcessingFailed]=@cntProcessingFailed, [Duplicate]=@cntDuplicate, [MappingIssue]=@cntMappingIssue

GO