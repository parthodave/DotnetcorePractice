USE [master]
GO
/****** Object:  Database [OurHeroDB]    Script Date: 10-01-2025 20:47:10 ******/
CREATE DATABASE [OurHeroDB]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'OurHeroDB', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL15.SQLEXPRESS\MSSQL\DATA\OurHeroDB.mdf' , SIZE = 8192KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'OurHeroDB_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL15.SQLEXPRESS\MSSQL\DATA\OurHeroDB_log.ldf' , SIZE = 8192KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT
GO
ALTER DATABASE [OurHeroDB] SET COMPATIBILITY_LEVEL = 150
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [OurHeroDB].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [OurHeroDB] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [OurHeroDB] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [OurHeroDB] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [OurHeroDB] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [OurHeroDB] SET ARITHABORT OFF 
GO
ALTER DATABASE [OurHeroDB] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [OurHeroDB] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [OurHeroDB] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [OurHeroDB] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [OurHeroDB] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [OurHeroDB] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [OurHeroDB] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [OurHeroDB] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [OurHeroDB] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [OurHeroDB] SET  DISABLE_BROKER 
GO
ALTER DATABASE [OurHeroDB] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [OurHeroDB] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [OurHeroDB] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [OurHeroDB] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [OurHeroDB] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [OurHeroDB] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [OurHeroDB] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [OurHeroDB] SET RECOVERY SIMPLE 
GO
ALTER DATABASE [OurHeroDB] SET  MULTI_USER 
GO
ALTER DATABASE [OurHeroDB] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [OurHeroDB] SET DB_CHAINING OFF 
GO
ALTER DATABASE [OurHeroDB] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [OurHeroDB] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [OurHeroDB] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [OurHeroDB] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
ALTER DATABASE [OurHeroDB] SET QUERY_STORE = OFF
GO
USE [OurHeroDB]
GO
/****** Object:  User [sa2]    Script Date: 10-01-2025 20:47:10 ******/
CREATE USER [sa2] FOR LOGIN [sa2] WITH DEFAULT_SCHEMA=[dbo]
GO
/****** Object:  Table [dbo].[__EFMigrationsHistory]    Script Date: 10-01-2025 20:47:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[OurHeros]    Script Date: 10-01-2025 20:47:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OurHeros](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FirstName] [varchar](50) NOT NULL,
	[LastName] [varchar](50) NULL,
	[isActive] [bit] NOT NULL,
 CONSTRAINT [PK_OurHero] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 10-01-2025 20:47:10 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FirstName] [varchar](50) NOT NULL,
	[LastName] [varchar](50) NULL,
	[UserName] [varchar](50) NOT NULL,
	[Password] [varchar](50) NOT NULL,
	[isActive] [bit] NOT NULL,
 CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[OurHeros] ON 
GO
INSERT [dbo].[OurHeros] ([Id], [FirstName], [LastName], [isActive]) VALUES (1, N'Parth', N'Dave', 1)
GO
SET IDENTITY_INSERT [dbo].[OurHeros] OFF
GO
SET IDENTITY_INSERT [dbo].[Users] ON 
GO
INSERT [dbo].[Users] ([Id], [FirstName], [LastName], [UserName], [Password], [isActive]) VALUES (1, N'Parth', N'Dave', N'parthodave', N'parthodave', 1)
GO
INSERT [dbo].[Users] ([Id], [FirstName], [LastName], [UserName], [Password], [isActive]) VALUES (2, N'Parth', N'Dave', N'parthodave', N'parthodave', 1)
GO
INSERT [dbo].[Users] ([Id], [FirstName], [LastName], [UserName], [Password], [isActive]) VALUES (3, N'parthkumar', N'dave', N'parthkumar', N'parthkumar', 1)
GO
INSERT [dbo].[Users] ([Id], [FirstName], [LastName], [UserName], [Password], [isActive]) VALUES (4, N'Parthkumar', N'Dave', N'Parthkumar', N'Parthkumar', 1)
GO
INSERT [dbo].[Users] ([Id], [FirstName], [LastName], [UserName], [Password], [isActive]) VALUES (5, N'local', N'user', N'local', N'local', 1)
GO
INSERT [dbo].[Users] ([Id], [FirstName], [LastName], [UserName], [Password], [isActive]) VALUES (6, N'jagdish', N'jagdish', N'jagdish', N'jagdish', 1)
GO
INSERT [dbo].[Users] ([Id], [FirstName], [LastName], [UserName], [Password], [isActive]) VALUES (7, N'parth', N'parth', N'parth', N'parth', 1)
GO
INSERT [dbo].[Users] ([Id], [FirstName], [LastName], [UserName], [Password], [isActive]) VALUES (8, N'Kairavi', N'Kairavi', N'Kairavi', N'Kairavi', 1)
GO
INSERT [dbo].[Users] ([Id], [FirstName], [LastName], [UserName], [Password], [isActive]) VALUES (9, N'kairavi', N'kairavi', N'kairavi', N'kairavi', 1)
GO
INSERT [dbo].[Users] ([Id], [FirstName], [LastName], [UserName], [Password], [isActive]) VALUES (10, N'kairavi', N'kairavi', N'kairavi', N'kairavi', 1)
GO
INSERT [dbo].[Users] ([Id], [FirstName], [LastName], [UserName], [Password], [isActive]) VALUES (11, N'Parth', N'Dave', N'parthodave', N'parthodave', 1)
GO
INSERT [dbo].[Users] ([Id], [FirstName], [LastName], [UserName], [Password], [isActive]) VALUES (12, N'Kairavi', N'Dave', N'kairavi', N'kairavi', 1)
GO
SET IDENTITY_INSERT [dbo].[Users] OFF
GO
USE [master]
GO
ALTER DATABASE [OurHeroDB] SET  READ_WRITE 
GO
