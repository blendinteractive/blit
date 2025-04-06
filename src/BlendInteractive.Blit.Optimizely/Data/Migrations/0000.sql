
CREATE TABLE BlitGlobalVariable (
	Id int NOT NULL IDENTITY(1,1),

	Name nvarchar(128) NOT NULL,
	Value nvarchar(256) NOT NULL,

	CONSTRAINT PK_BlitGlobalVariable PRIMARY KEY(Id)
);

GO

CREATE TABLE BlitBatch (
	Id int NOT NULL IDENTITY(1,1),

	Name nvarchar(128) NOT NULL,
	StageId int NOT NULL,

	Created datetime NOT NULL,
	Started datetime NULL,
	Completed datetime NULL,

	CONSTRAINT PK_BlitBatch PRIMARY KEY(Id)
);

GO

CREATE TABLE BlitContent (
	Id int NOT NULL IDENTITY(1,1),
	BatchId int NOT NULL,
	Priority int NOT NULL,

	ContentPath nvarchar(256) NULL,
	Content ntext NULL,
	StageId int NOT NULL,

	Created datetime NOT NULL,
	Started datetime NULL,
	Completed datetime NULL,

	CONSTRAINT FK_BlitBatch_Id FOREIGN KEY (BatchId) REFERENCES BlitBatch(Id),
	CONSTRAINT PK_BlitContent PRIMARY KEY(Id)
);


CREATE TABLE BlitBatchVariable (
	Id int NOT NULL IDENTITY(1,1),
	BatchID int NOT NULL,

	Name nvarchar(128) NOT NULL,
	Value nvarchar(256) NOT NULL,

	CONSTRAINT FK_BlitBatchVariable_Batch FOREIGN KEY (BatchId) REFERENCES BlitBatch(Id),
	CONSTRAINT PK_BlitBatchVariable PRIMARY KEY(Id)
);

CREATE TABLE BlitBatchLogEntry (
	Id int NOT NULL IDENTITY(1,1),
	BatchId int NOT NULL,
	ContentId int NULL,
	
	Date datetime NOT NULL,
	Text ntext NOT NULL,

	CONSTRAINT FK_BlitBatchLogEntry_Content FOREIGN KEY (ContentId) REFERENCES BlitContent(Id),
	CONSTRAINT FK_BlitBatchLogEntry_Batch FOREIGN KEY (BatchId) REFERENCES BlitBatch(Id),
	CONSTRAINT PK_BlitBatchLogEntry PRIMARY KEY(Id)
);

GO


CREATE PROCEDURE BlitVersion AS 
BEGIN 
	SELECT 1
END
