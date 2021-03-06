
USE SQLFS
GO


-- IF  EXISTS (SELECT * FROM sys.server_principals WHERE name = N'Dokan') DROP LOGIN Dokan; 


IF NOT EXISTS(SELECT * FROM sys.server_principals WHERE name = N'Dokan')
BEGIN
	CREATE LOGIN Dokan WITH PASSWORD=N'Foobar2000' 
	--, DEFAULT_DATABASE=master, DEFAULT_LANGUAGE=[Deutsch], CHECK_EXPIRATION=OFF, CHECK_POLICY=OFF 
	, DEFAULT_DATABASE=master, DEFAULT_LANGUAGE=[us_english], CHECK_EXPIRATION=OFF, CHECK_POLICY=OFF 
END
;

EXEC sys.sp_addsrvrolemember @loginame = N'Dokan', @rolename = N'sysadmin';




-- IF  EXISTS (SELECT * FROM sys.database_principals WHERE name = N'nedopilm') DROP USER [nedopilm];
-- CREATE USER [nedopilm] FOR LOGIN [nedopilm] WITH DEFAULT_SCHEMA=[dbo];

GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DOKANFS]') AND type in (N'U'))
BEGIN
	CREATE TABLE dbo.DOKANFS
	(
		 IDFILE int IDENTITY(1, 1) NOT NULL 
		,FILENAME varchar(255) NULL 
		,ISDIRECTORY bit NULL 
		,CONTENT varbinary(MAX) NULL 
		,LastAccessTime datetime NOT NULL CONSTRAINT DF_DOKANFS_LastAccessTime DEFAULT (CURRENT_TIMESTAMP) 
		,LastWriteTime datetime NOT NULL CONSTRAINT DF_DOKANFS_LastWriteTime DEFAULT (CURRENT_TIMESTAMP) 
		,CreationTime datetime NOT NULL CONSTRAINT DF_DOKANFS_CreationTime DEFAULT (CURRENT_TIMESTAMP) 
		,Attributes bigint NULL 
		,IsZipped bit NULL CONSTRAINT DF_DOKANFS_IsZipped DEFAULT ((CAST(0 AS bit))) 
		,IsEncrypted bit NULL CONSTRAINT DF_DOKANFS_IsEncrypted DEFAULT ((CAST(0 AS bit))) 
		,OriginalSize bigint NULL 
		,Version int NULL 
		,CONSTRAINT PK_DOKANFS PRIMARY KEY (IDFILE) 
	);
END
GO

/*
CREATE TABLE DOKANFS
(
	 IDFILE int NOT NULL 
	,FILENAME varchar(255) NULL 
	,ISDIRECTORY bit NULL 
	,CONTENT bytea NULL 
	,LastAccessTime timestamp without time zone NOT NULL CONSTRAINT DF_DOKANFS_LastAccessTime DEFAULT (CURRENT_TIMESTAMP) 
	,LastWriteTime timestamp without time zone NOT NULL CONSTRAINT DF_DOKANFS_LastWriteTime DEFAULT (CURRENT_TIMESTAMP) 
	,CreationTime timestamp without time zone NOT NULL CONSTRAINT DF_DOKANFS_CreationTime DEFAULT (CURRENT_TIMESTAMP) 
	,Attributes bigint NULL 
	,IsZipped bit NULL CONSTRAINT DF_DOKANFS_IsZipped DEFAULT ((CAST(0 AS bit))) 
	,IsEncrypted bit NULL CONSTRAINT DF_DOKANFS_IsEncrypted DEFAULT ((CAST(0 AS bit))) 
	,OriginalSize bigint NULL 
	,Version int NULL 
	,CONSTRAINT PK_DOKANFS PRIMARY KEY (IDFILE) 
);
*/ 



IF  EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[DOKANFS]') AND name = N'IX_DOKANFS')
DROP INDEX IX_DOKANFS ON dbo.DOKANFS WITH ( ONLINE = OFF )
GO



CREATE UNIQUE NONCLUSTERED INDEX IX_DOKANFS ON dbo.DOKANFS 
(
	 FILENAME ASC 
	,Version ASC 
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO



IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[WriteFile]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[WriteFile]
GO





CREATE PROCEDURE [dbo].[WriteFile]
(
	 @filename varchar(255) 
	,@data varbinary(MAX) 
	,@IsZipped bit 
	,@OriginalSize bigint 
)
AS
    DECLARE @isVersioned int
    SET @isVersioned = 0
    
	SET NOCOUNT ON
	IF PATINDEX('%.version', @filename) > 0 
	BEGIN
	   SET @filename = SUBSTRING(@filename, 1, PATINDEX('%.version',@filename)-1)
	   SET @isVersioned = 1
	END
	   
    IF 1=(SELECT TOP 1 1 FROM DOKANFS WHERE FILENAME = @filename AND Version IS NULL) 
    BEGIN
		-- get last version number AND increment 
	    IF @isVersioned = 1 
	    BEGIN	   
			DECLARE @version int
			
			SELECT @version = ISNULL(version,0) + 1
			FROM DOKANFS
			WHERE FILENAME = @filename 
			AND ISNULL(Version, 0) = (SELECT MAX(ISNULL(version,0)) FROM DOKANFS WHERE FILENAME = @filename)
		   
		   -- save current to version file 
		   INSERT INTO DOKANFS(FILENAME,ISDIRECTORY,CONTENT,IsZipped,OriginalSize,CreationTime,IsEncrypted,LastAccessTime,LastWriteTime, Version)
		   SELECT FILENAME,ISDIRECTORY,CONTENT,IsZipped,OriginalSize,CreationTime,IsEncrypted,LastAccessTime,LastWriteTime, @version
		   FROM DOKANFS WHERE FILENAME = @filename AND Version IS NULL 
		END
	   
		-- UPDATE current context file 
		UPDATE DOKANFS
			SET  CONTENT = @data
				,LastAccessTime = CURRENT_TIMESTAMP
				,LastWriteTime = CURRENT_TIMESTAMP
				,OriginalSize = @OriginalSize
				,isZipped = @IsZipped
	      WHERE (1=1) 
	      AND FILENAME = @filename 
	      AND Version IS NULL 
	END 
	ELSE 
	BEGIN 
	   INSERT INTO DOKANFS(FILENAME, ISDIRECTORY, CONTENT, IsZipped, OriginalSize,CreationTime, LastAccessTime, LastWriteTime) 
	   VALUES(@filename,0,@data,@IsZipped,@OriginalSize, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) 
	END 
	
	RETURN 
GO


IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReadFile]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[ReadFile]
GO





CREATE PROCEDURE [dbo].[ReadFile]
(
	@filename varchar(255)
)	
AS 
	-- exec ReadFile '\bc.JPG' 
	SET NOCOUNT ON
    
	SELECT DATALENGTH(CONTENT) AS size, IsZipped, DOKANFS.CONTENT
	FROM DOKANFS
	WHERE 
	(
		filename 
		+  
		CASE 
			WHEN Version IS NOT NULL 
				THEN '.' + CAST( COALESCE(version, '') AS varchar(10)) 
			ELSE '' 
		END 
	) = @filename 
	
	RETURN
GO



IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MoveFile]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[MoveFile]
GO





CREATE PROCEDURE [dbo].[MoveFile] 
( 
	 @filename varchar(255) 
	,@newname varchar(255) 
	,@replace bit 
) 
AS
	-- SET NOCOUNT ON 
	
	IF @replace = 0 
	BEGIN
		IF 1 = (SELECT top 1 1 FROM DOKANFS WHERE FILENAME = @newname) 
	      RAISERROR ('File already exists', 16, 1) 
	END 
	
	DELETE FROM dokanfs WHERE filename = @newname AND Version IS NULL 
	
	UPDATE dokanfs 
		SET filename = @newname 
	WHERE filename = @filename 
	AND Version IS NULL 
	
	RETURN
GO



IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GetFileInformation]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[GetFileInformation]
GO



CREATE PROCEDURE [dbo].[GetFileInformation]
(
	 @filename varchar(255) 
	,@IsDirectory  bit OUTPUT
	,@Length bigint OUTPUT
	,@LastAccessTime DateTime OUTPUT
	,@LastWriteTime DateTime OUTPUT
	,@CreationTime DateTime OUTPUT 
)
	-- DECLARE @isdir bit
	-- DECLARE @len bigint
	-- DECLARE @La datetime
	-- DECLARE @lw datetime
	-- DECLARE @cr datetime
	-- DECLARE @file varchar(255)
	-- SET @file = '\bc.JPG'
	-- EXEC GetFileInformation @file , @isdir out, @len out, @La out, @lw out, @cr out
	-- PRINT @file
	-- PRINT @cr
AS
	SET NOCOUNT ON 

	SELECT 
		 @filename = [filename]
		,@IsDirectory = isdirectory
		,@Length = ISNULL(OriginalSize, DATALENGTH(CONTENT) )
		,@LastAccessTime = LastAccessTime
		,@LastWriteTime = LastWriteTime
		,@CreationTime = CreationTime
	FROM DOKANFS   
	WHERE 
	(
		filename +  
		CASE 
			WHEN Version IS NOT NULL 
				THEN '.' + CAST( COALESCE(version, '') AS varchar(10)) 
			ELSE ''
		END
	) = @filename 
	
	RETURN
GO


IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FindFiles]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[FindFiles]
GO



CREATE PROCEDURE [dbo].[FindFiles]
(
	@filename varchar(255)
)
AS
    -- EXEC FindFiles @filename = '\' 
	SET NOCOUNT ON 
	
	IF @filename = '\' 
		SET @filename = '\' 
	ELSE 
		SET @filename = @filename+'\'
	
	SELECT 
		 filename
		,isdirectory
		,ISNULL(OriginalSize, DATALENGTH([CONTENT])) AS size
		,filename AS fullfilename
		,LastAccessTime
		,LastWriteTime
		,CreationTime
	INTO #TEMP 
	FROM DOKANFS 
	WHERE (1=1) 
	AND filename LIKE @filename+'%' 
	AND FILENAME NOT LIKE @filename+'%\%' 
	AND Version IS NULL 
	
	
	-- all versions 
	DECLARE @allVersion int
	SELECT @allVersion = ISNULL(CAST(content AS int),0) FROM DOKANFS WHERE FILENAME = '\'
	PRINT @allVersion
	IF @allVersion = 1 
	BEGIN 
		SELECT 
			 filename + '.' + CAST(COALESCE(version, '0') AS varchar(10)) AS filename
			,isdirectory
			,ISNULL(OriginalSize, DATALENGTH(content)) AS size
			,filename + '.'+ CAST(COALESCE(version, '0') AS varchar(10)) AS fullfilename
			,LastAccessTime
			,LastWriteTime
			,CreationTime
		INTO #TEMP2
		FROM DOKANFS 
		WHERE (filename LIKE @filename + '%' AND FILENAME NOT LIKE @filename + '%\%' AND Version IS NOT NULL) 
		
		UPDATE #TEMP2 
			SET filename = SUBSTRING(filename, CHARINDEX(@filename, filename) + LEN(@filename), 255) 
	END
	
	
	UPDATE #TEMP 
		SET filename = SUBSTRING(filename, CHARINDEX(@filename, filename) + LEN(@filename), 255) 
		
		
	INSERT INTO #TEMP (filename, isdirectory, size, fullfilename, LastAccessTime, LastWriteTime, CreationTime) 
	VALUES ('.', 1, 0, '.', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
	
	IF @filename <> '\' 
		INSERT INTO #TEMP (filename, isdirectory, size, fullfilename, LastAccessTime, LastWriteTime, CreationTime) 
		VALUES ('..', 1, 0, '..', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
	
	IF @allVersion = 1 
	BEGIN
		SELECT * FROM #TEMP 
		UNION 
		SELECT * FROM #TEMP2 ORDER BY filename
	END 
	ELSE 
	BEGIN
		SELECT * FROM #TEMP ORDER BY filename
	END 
	
	RETURN
GO



IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DELETEFile]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[DELETEFile]
GO



CREATE PROCEDURE [dbo].[DELETEFile]
(
	@filename varchar(255)
)
AS
    SET NOCOUNT ON 
    
    DELETE FROM DOKANFS WHERE FILENAME LIKE @filename + '%' AND Version IS NULL 
    RETURN
GO




IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CreateDirectory]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[CreateDirectory]
GO




CREATE PROCEDURE [dbo].[CreateDirectory] 
(
	 @filename varchar(255)
) 
AS
    SET NOCOUNT ON 
    INSERT INTO DOKANFS(FILENAME, ISDIRECTORY) VALUES (@Filename, 1) 
	RETURN
GO

