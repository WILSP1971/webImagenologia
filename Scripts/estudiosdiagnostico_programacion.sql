/*
SQLyog Community v13.3.1 (64 bit)
MySQL - 5.6.19 
*********************************************************************
*/
/*!40101 SET NAMES utf8 */;

create table `estudiosdiagnosticos_programacion` (
	`Empresa` varchar (12),
	`Consecutivo` bigint (20),
	`NoCuenta` Decimal (11),
	`CedulaMedico` varchar (45),
	`FechaProgramacion` date ,
	`Servicio` varchar (6),
	`CodServicio` varchar (45),
	`Dependencia` varchar (12),
	`NoOrden` Decimal (11),
	`AudioRadiologo` blob ,
	`UsuarioOperador` varchar (60),
	`FechaAsignacion` date ,
	`Estado` char (6)
); 
