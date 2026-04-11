USE [master]
GO
/****** Object:  Database [JohnCis_HotelManagement_System]    Script Date: 4/11/2026 4:21:09 PM ******/
CREATE DATABASE [JohnCis_HotelManagement_System]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'JohnCis_HotelManagement_System', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\JohnCis_HotelManagement_System.mdf' , SIZE = 8192KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'JohnCis_HotelManagement_System_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\JohnCis_HotelManagement_System_log.ldf' , SIZE = 8192KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET COMPATIBILITY_LEVEL = 150
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [JohnCis_HotelManagement_System].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET ARITHABORT OFF 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET  DISABLE_BROKER 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET RECOVERY SIMPLE 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET  MULTI_USER 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET DB_CHAINING OFF 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET QUERY_STORE = OFF
GO
USE [JohnCis_HotelManagement_System]
GO
/****** Object:  Table [dbo].[Billing]    Script Date: 4/11/2026 4:21:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Billing](
	[BillingID] [int] IDENTITY(1,1) NOT NULL,
	[ReservationID] [int] NULL,
	[TotalAmount] [decimal](10, 2) NULL,
	[TaxAmount] [decimal](10, 2) NULL,
	[DiscountAmount] [decimal](10, 2) NULL,
	[FinalAmount] [decimal](10, 2) NULL,
	[BillingStatus] [varchar](20) NULL,
PRIMARY KEY CLUSTERED 
(
	[BillingID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[ReservationID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Billing_Details]    Script Date: 4/11/2026 4:21:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Billing_Details](
	[BillingDetailsID] [int] IDENTITY(1,1) NOT NULL,
	[BillingID] [int] NULL,
	[Description] [varchar](255) NULL,
	[Quantity] [int] NULL,
	[UnitPrice] [decimal](10, 2) NULL,
	[Subtotal] [decimal](10, 2) NULL,
PRIMARY KEY CLUSTERED 
(
	[BillingDetailsID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Customers]    Script Date: 4/11/2026 4:21:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Customers](
	[CustomerID] [int] IDENTITY(1,1) NOT NULL,
	[FirstName] [varchar](50) NULL,
	[LastName] [varchar](50) NULL,
	[ContactNumber] [varchar](20) NULL,
	[Email] [varchar](100) NULL,
	[ValidIDType] [varchar](50) NULL,
	[ValidIDNumber] [varchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[CustomerID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Housekeeping]    Script Date: 4/11/2026 4:21:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Housekeeping](
	[HousekeepingID] [int] IDENTITY(1,1) NOT NULL,
	[StaffID] [int] NULL,
	[RoomID] [int] NULL,
	[CleaningDate] [date] NULL,
	[CleaningStatus] [varchar](50) NULL,
	[Notes] [varchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[HousekeepingID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Inventory]    Script Date: 4/11/2026 4:21:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Inventory](
	[InventoryID] [int] IDENTITY(1,1) NOT NULL,
	[ItemName] [varchar](100) NULL,
	[Category] [varchar](50) NULL,
	[QuantityInStock] [int] NULL,
	[ReorderLevel] [int] NULL,
	[UnitCost] [decimal](10, 2) NULL,
	[Status] [varchar](20) NULL,
PRIMARY KEY CLUSTERED 
(
	[InventoryID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Inventory_Usage]    Script Date: 4/11/2026 4:21:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Inventory_Usage](
	[UsageID] [int] IDENTITY(1,1) NOT NULL,
	[InventoryID] [int] NULL,
	[RoomID] [int] NULL,
	[StaffID] [int] NULL,
	[Quantity] [int] NULL,
	[UsageDate] [date] NULL,
PRIMARY KEY CLUSTERED 
(
	[UsageID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Payment]    Script Date: 4/11/2026 4:21:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Payment](
	[PaymentID] [int] IDENTITY(1,1) NOT NULL,
	[ReservationID] [int] NULL,
	[AmountPaid] [decimal](10, 2) NULL,
	[PaymentDate] [datetime] NULL,
	[PaymentMethod] [varchar](50) NULL,
	[PaymentReferenceNumber] [varchar](100) NULL,
	[PaymentStatus] [varchar](20) NULL,
PRIMARY KEY CLUSTERED 
(
	[PaymentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Reservations]    Script Date: 4/11/2026 4:21:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Reservations](
	[ReservationID] [int] IDENTITY(1,1) NOT NULL,
	[CustomerID] [int] NULL,
	[RoomID] [int] NULL,
	[CheckInDate] [date] NULL,
	[CheckOutDate] [date] NULL,
	[NumberOfGuest] [int] NULL,
	[CreatedBy] [int] NULL,
	[ReservationDate] [date] NULL,
	[ReservationStatus] [varchar](20) NULL,
PRIMARY KEY CLUSTERED 
(
	[ReservationID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Roles]    Script Date: 4/11/2026 4:21:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Roles](
	[RoleID] [int] IDENTITY(1,1) NOT NULL,
	[RoleName] [varchar](100) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[RoleID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Rooms]    Script Date: 4/11/2026 4:21:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Rooms](
	[RoomID] [int] IDENTITY(1,1) NOT NULL,
	[RoomNumber] [varchar](10) NULL,
	[RoomTypeID] [int] NULL,
	[Status] [varchar](20) NULL,
PRIMARY KEY CLUSTERED 
(
	[RoomID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[RoomNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RoomTypes]    Script Date: 4/11/2026 4:21:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RoomTypes](
	[RoomTypeID] [int] IDENTITY(1,1) NOT NULL,
	[TypeName] [varchar](100) NULL,
	[Description] [varchar](255) NULL,
	[PricePerNight] [decimal](10, 2) NULL,
PRIMARY KEY CLUSTERED 
(
	[RoomTypeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Staff]    Script Date: 4/11/2026 4:21:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Staff](
	[StaffID] [int] IDENTITY(1,1) NOT NULL,
	[RoleID] [int] NULL,
	[FullName] [varchar](100) NULL,
	[UserName] [varchar](50) NULL,
	[PasswordHash] [varchar](255) NULL,
	[ContactNumber] [varchar](20) NULL,
	[Email] [varchar](100) NULL,
	[Status] [varchar](20) NULL,
PRIMARY KEY CLUSTERED 
(
	[StaffID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[UserName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Billing]  WITH CHECK ADD FOREIGN KEY([ReservationID])
REFERENCES [dbo].[Reservations] ([ReservationID])
GO
ALTER TABLE [dbo].[Billing_Details]  WITH CHECK ADD FOREIGN KEY([BillingID])
REFERENCES [dbo].[Billing] ([BillingID])
GO
ALTER TABLE [dbo].[Housekeeping]  WITH CHECK ADD FOREIGN KEY([RoomID])
REFERENCES [dbo].[Rooms] ([RoomID])
GO
ALTER TABLE [dbo].[Housekeeping]  WITH CHECK ADD FOREIGN KEY([StaffID])
REFERENCES [dbo].[Staff] ([StaffID])
GO
ALTER TABLE [dbo].[Inventory_Usage]  WITH CHECK ADD FOREIGN KEY([InventoryID])
REFERENCES [dbo].[Inventory] ([InventoryID])
GO
ALTER TABLE [dbo].[Inventory_Usage]  WITH CHECK ADD FOREIGN KEY([RoomID])
REFERENCES [dbo].[Rooms] ([RoomID])
GO
ALTER TABLE [dbo].[Inventory_Usage]  WITH CHECK ADD FOREIGN KEY([StaffID])
REFERENCES [dbo].[Staff] ([StaffID])
GO
ALTER TABLE [dbo].[Payment]  WITH CHECK ADD FOREIGN KEY([ReservationID])
REFERENCES [dbo].[Reservations] ([ReservationID])
GO
ALTER TABLE [dbo].[Reservations]  WITH CHECK ADD FOREIGN KEY([CreatedBy])
REFERENCES [dbo].[Staff] ([StaffID])
GO
ALTER TABLE [dbo].[Reservations]  WITH CHECK ADD FOREIGN KEY([CustomerID])
REFERENCES [dbo].[Customers] ([CustomerID])
GO
ALTER TABLE [dbo].[Reservations]  WITH CHECK ADD FOREIGN KEY([RoomID])
REFERENCES [dbo].[Rooms] ([RoomID])
GO
ALTER TABLE [dbo].[Rooms]  WITH CHECK ADD FOREIGN KEY([RoomTypeID])
REFERENCES [dbo].[RoomTypes] ([RoomTypeID])
GO
ALTER TABLE [dbo].[Staff]  WITH CHECK ADD FOREIGN KEY([RoleID])
REFERENCES [dbo].[Roles] ([RoleID])
GO
USE [master]
GO
ALTER DATABASE [JohnCis_HotelManagement_System] SET  READ_WRITE 
GO
