SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SiteConfigurations](
	[SiteId] [uniqueidentifier] NOT NULL,
	[Url] [nvarchar](1024) NOT NULL,
	[UserName] [nvarchar](256) NOT NULL,
	[Password] [nvarchar](1024) NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[Status] [int] NOT NULL,
	[MaxThreads] [int] NOT NULL,
 CONSTRAINT [PK_SiteConfiguration] PRIMARY KEY CLUSTERED 
(
	[SiteId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[SystemObjects](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ParentId] [int] NULL,
	[SystemId] [int] NOT NULL,
	[ViewId] [int] NOT NULL,
	[Descriptor] [varchar](max) NULL,
	[Designation] [varchar](max) NULL,
	[Name] [varchar](max) NULL,
	[SystemName] [varchar](max) NULL,
	[ObjectId] [varchar](max) NULL,
	[Attributes] [varchar](max) NULL,
	[Properties] [varchar](max) NULL,
	[FunctionProperties] [varchar](max) NULL
 CONSTRAINT [PK_SystemObjects] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[SystemObjects]  WITH CHECK ADD  CONSTRAINT [FK_SystemObjects_SystemObjects] FOREIGN KEY([ParentId])
REFERENCES [dbo].[SystemObjects] ([Id])
GO

ALTER TABLE [dbo].[SystemObjects] CHECK CONSTRAINT [FK_SystemObjects_SystemObjects]
GO

CREATE TABLE [dbo].[ScanRequests](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SiteId] [uniqueidentifier] NOT NULL,
	[CreatedBy] [uniqueidentifier] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[Status] [int] NOT NULL,
	[StartTime] [datetime] NULL,
	[EndTime] [datetime] NULL,
	[NumberOfPoints] [int] NOT NULL,
	[Messages] [nvarchar](max) NULL,
 CONSTRAINT [PK_ScanRequests] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

