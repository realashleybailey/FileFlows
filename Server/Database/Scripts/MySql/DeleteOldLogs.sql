DROP PROCEDURE IF EXISTS DeleteOldLogs;

CREATE PROCEDURE DeleteOldLogs(MaxItems int)
BEGIN

    CREATE TABLE new LIKE DbLogMessage;   # empty table with same schema
    INSERT INTO new SELECT * FROM DbLogMessage ORDER BY LogDate desc LIMIT MaxItems;  # copy the rows to _keep_
    RENAME TABLE DbLogMessage TO old, new TO DbLogMessage;  # rearrange
    DROP TABLE old;  # clean up.

END;