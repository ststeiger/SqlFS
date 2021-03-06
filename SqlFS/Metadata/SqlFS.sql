USE [ekvelb2]
GO
/****** Object:  Table [dbo].[DOKANFS]    Script Date: 08/06/2010 15:50:32 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[DOKANFS](
	[IDFILE] [int] IDENTITY(1,1) NOT NULL,
	[FILENAME] [varchar](255) NULL,
	[ISDIRECTORY] [bit] NULL,
	[CONTENT] [varbinary](max) NULL,
	[LastAccessTime] [datetime] NOT NULL,
	[LastWriteTime] [datetime] NOT NULL,
	[CreationTime] [datetime] NOT NULL,
	[Attributes] [bigint] NULL,
	[IsZipped] [bit] NULL,
	[IsEncrypted] [bit] NULL,
	[OriginalSize] [bigint] NULL,
 CONSTRAINT [PK_DOKANFS] PRIMARY KEY CLUSTERED 
(
	[IDFILE] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET ANSI_PADDING OFF
GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_DOKANFS] ON [dbo].[DOKANFS] 
(
	[FILENAME] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO
/****** Object:  StoredProcedure [dbo].[WriteFile2]    Script Date: 08/06/2010 15:51:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[WriteFile2]
	(
	@filename varchar(255),
	@data varbinary(max),
	@IsZipped bit,
	@OriginalSize bigint
	)
	
AS
	SET NOCOUNT ON
	
	if 1=(select top 1 1 from DOKANFS where FILENAME = @filename) begin
	   update DOKANFS
	      set CONTENT = @data,
	          LastAccessTime = GetDate(),
	          LastWriteTime = GETDATE()
	      where FILENAME = @filename
	   end else begin
   	   insert into DOKANFS(FILENAME,ISDIRECTORY,CONTENT,IsZipped, OriginalSize) values(@filename,0,@data,@IsZipped,@OriginalSize) 
   	   end
	 
	RETURN
GO
/****** Object:  StoredProcedure [dbo].[WriteFile]    Script Date: 08/06/2010 15:51:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[WriteFile]
	(
	@filename varchar(255),
	@offset bigint,
	@length bigint,
	@data varbinary(max)
	)
	
AS
	SET NOCOUNT ON
	
	if 1=(select top 1 1 from DOKANFS where FILENAME = @filename) begin
	   update DOKANFS
	      set CONTENT .WRITE(@data,@offset,@length)
	      where FILENAME = @filename
	   end else begin
   	   insert into DOKANFS(FILENAME,ISDIRECTORY,CONTENT) values(@filename,0,@data) 
   	   end
	 
	RETURN
GO
/****** Object:  StoredProcedure [dbo].[ReadFile2]    Script Date: 08/06/2010 15:51:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[ReadFile2]
	
	(
	@filename varchar(255)
	)
	
AS

/*



declare @readbytes bigint

exec ReadFile2
 '\ahoj.txt',
 0
 

 
 
 print @readbytes

 */
 
	SET NOCOUNT ON

	
	SELECT DATALENGTH(CONTENT) as size, IsZipped, DOKANFS.CONTENT
	from DOKANFS
	where FILENAME = @filename
	
	
	RETURN
GO
/****** Object:  StoredProcedure [dbo].[ReadFile]    Script Date: 08/06/2010 15:51:39 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[ReadFile]
	
	(
	@filename varchar(255),
	@offset bigint
	)
	
AS

/*



declare @readbytes bigint

exec ReadFile
 '\ahoj.txt',
 0
 

 
 
 print @readbytes

 */
 
	SET NOCOUNT ON

	
	SELECT DATALENGTH(CONTENT) as size, DOKANFS.CONTENT
	from DOKANFS
	where FILENAME = @filename
	
	
	RETURN
GO
/****** Object:  StoredProcedure [dbo].[MoveFile]    Script Date: 08/06/2010 15:51:39 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[MoveFile]
	
	(
	@filename varchar(255),
	@newname varchar(255),
	@replace bit
	)
	
AS
	/* SET NOCOUNT ON */
	
	if @replace = 0 begin
	   if 1=(select top 1 1 from DOKANFS where FILENAME = @newname) 
	      raiserror('File already exists',16,1)
	   end
	delete from dokanfs
	where filename = @newname
	
	update dokanfs
	set filename = @newname
	where filename = @filename
	
	RETURN
GO
/****** Object:  StoredProcedure [dbo].[GetFileInformation]    Script Date: 08/06/2010 15:51:39 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[GetFileInformation]
	(
	@filename varchar(255),
	@IsDirectory  bit OUTPUT,
	@Length bigint OUTPUT,
	@LastAccessTime DateTime OUTPUT,
	@LastWriteTime DateTime OUTPUT,
	@CreationTime DateTime OUTPUT
	)
	
AS
	SET NOCOUNT ON 
	
	select @IsDirectory = isdirectory, @Length = IsNull(OriginalSize,DATALENGTH([CONTENT])),
	       @LastAccessTime = LastAccessTime, @LastWriteTime = LastWriteTime,
	       @CreationTime = CreationTime
	  from DOKANFS 
	 where (filename = @filename)
	
	RETURN
GO
/****** Object:  StoredProcedure [dbo].[FindFiles]    Script Date: 08/06/2010 15:51:39 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[FindFiles]
	(
	@filename varchar(255)
	)
	
AS
    /*
    exec FindFiles @filename = '\Treti' 
    */
	SET NOCOUNT ON 
	
	if @filename = '\' set @filename = '\' else set @filename = @filename+'\'
	
	select filename, isdirectory, IsNull(OriginalSize,DATALENGTH([CONTENT])) as size, filename as fullfilename,
	       LastAccessTime,LastWriteTime,CreationTime
	  into #TEMP
	  from DOKANFS 
	 where (filename like @filename+'%' and FILENAME not like @filename+'%\%')
	
	update #TEMP
	set filename = SUBSTRING(filename, CHARINDEX(@filename,filename)+LEN(@filename),255)
	
	insert into #TEMP (filename, isdirectory,size,fullfilename,LastAccessTime,LastWriteTime,CreationTime) 
	       values ('.',1,0,'.',GETDATE(),GETDATE(),GETDATE())
	if @filename <> '\' 
	   insert into #TEMP (filename,isdirectory,size,fullfilename,LastAccessTime,LastWriteTime,CreationTime) 
	          values ('..',1,0,'..',GETDATE(),GETDATE(),GETDATE())
	
	select * from #TEMP order by filename
	
	RETURN
GO
/****** Object:  StoredProcedure [dbo].[DeleteFile]    Script Date: 08/06/2010 15:51:39 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[DeleteFile]
	
	(
	@filename varchar(255)
	)
	
AS
    SET NOCOUNT ON 
    
    delete from DOKANFS where FILENAME like @filename+'%'
    
    RETURN
GO
/****** Object:  StoredProcedure [dbo].[CreateDirectory]    Script Date: 08/06/2010 15:51:39 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[CreateDirectory]
	
	(
	@filename varchar(255)
	)
	
AS
    SET NOCOUNT ON 
    insert into DOKANFS(FILENAME, ISDIRECTORY) VALUES (@Filename,1) 
	RETURN
GO
/****** Object:  Default [DF_DOKANFS_LastAccessTime]    Script Date: 08/06/2010 15:50:32 ******/
ALTER TABLE [dbo].[DOKANFS] ADD  CONSTRAINT [DF_DOKANFS_LastAccessTime]  DEFAULT (getdate()) FOR [LastAccessTime]
GO
/****** Object:  Default [DF_DOKANFS_LastWriteTime]    Script Date: 08/06/2010 15:50:32 ******/
ALTER TABLE [dbo].[DOKANFS] ADD  CONSTRAINT [DF_DOKANFS_LastWriteTime]  DEFAULT (getdate()) FOR [LastWriteTime]
GO
/****** Object:  Default [DF_DOKANFS_CreationTime]    Script Date: 08/06/2010 15:50:32 ******/
ALTER TABLE [dbo].[DOKANFS] ADD  CONSTRAINT [DF_DOKANFS_CreationTime]  DEFAULT (getdate()) FOR [CreationTime]
GO
/****** Object:  Default [DF_DOKANFS_IsZipped]    Script Date: 08/06/2010 15:50:32 ******/
ALTER TABLE [dbo].[DOKANFS] ADD  CONSTRAINT [DF_DOKANFS_IsZipped]  DEFAULT ((0)) FOR [IsZipped]
GO
/****** Object:  Default [DF_DOKANFS_IsEncrypted]    Script Date: 08/06/2010 15:50:32 ******/
ALTER TABLE [dbo].[DOKANFS] ADD  CONSTRAINT [DF_DOKANFS_IsEncrypted]  DEFAULT ((0)) FOR [IsEncrypted]
GO
