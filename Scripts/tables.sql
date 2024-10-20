/****** Object:  Table [dbo].[ScheduledTask]     ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ScheduledTask](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TaskTypeId] [int] NULL,
	[TaskParameters] [nvarchar](max) NULL,
	[ScheduledTime] [datetime] NULL,
	[CronExpression] [nvarchar](255) NULL,
	[Status] [nvarchar](50) NULL,
	[ProcessedBy] [nvarchar](255) NULL,
	[ProcessedAt] [datetime] NULL,
	[IntervalInSeconds] [int] NULL,
	[TaskStatus] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SystemEvent]     ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SystemEvent](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EventType] [nvarchar](100) NULL,
	[SystemType] [nvarchar](255) NULL,
	[Message] [nvarchar](max) NULL,
	[Timestamp] [datetime] NULL,
 CONSTRAINT [PK_SystemEvent] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TaskCategory]     ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TaskCategory](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CategoryName] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TaskExecutionHistory]     ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TaskExecutionHistory](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TaskId] [int] NULL,
	[TaskParameters] [nvarchar](max) NULL,
	[ExecutionStartTime] [datetime] NULL,
	[ExecutionEndTime] [datetime] NULL,
	[Status] [nvarchar](50) NULL,
	[ProcessedBy] [nvarchar](255) NULL,
	[Result] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TaskType]     ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TaskType](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TaskCategoryId] [int] NULL,
	[TypeName] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[ScheduledTask]  WITH CHECK ADD FOREIGN KEY([TaskTypeId])
REFERENCES [dbo].[TaskType] ([Id])
GO
ALTER TABLE [dbo].[TaskExecutionHistory]  WITH CHECK ADD FOREIGN KEY([TaskId])
REFERENCES [dbo].[ScheduledTask] ([Id])
GO
ALTER TABLE [dbo].[TaskType]  WITH CHECK ADD FOREIGN KEY([TaskCategoryId])
REFERENCES [dbo].[TaskCategory] ([Id])
GO
