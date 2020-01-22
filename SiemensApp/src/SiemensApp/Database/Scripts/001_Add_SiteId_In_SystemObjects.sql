SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

alter table [SystemObjects] add [SiteId] uniqueidentifier;
GO
update [SystemObjects] set [SiteId] = 'F05D307B-8D33-4DB3-B20D-7F2A844BEF90';
GO
alter table [SystemObjects] alter column [SiteId] uniqueidentifier not null;
GO
