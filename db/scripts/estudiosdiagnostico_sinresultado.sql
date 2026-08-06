/*
SQLyog Community v13.3.1 (64 bit)
MySQL - 5.6.19 
*********************************************************************
*/
/*!40101 SET NAMES utf8 */;

create table `estudiosdiagnosticos_sinresultado` (
	`Empresa` varchar (12),
	`NoCuenta` Decimal (11),
	`NoIdentificacion` varchar (60),
	`NoOrden` Decimal (11),
	`Valor` Decimal (11),
	`CodServicio` varchar (45),
	`nombreServicio` varchar (765),
	`Dependencia` varchar (12),
	`Fecha_cargo` date ,
	`codEsquema` varchar (12),
	`Servicio` varchar (6),
	`fecha_real_cargo` date ,
	`hora_real_cargo` varchar (15),
	`Usuario_grabacargo` varchar (90),
	`Consecutivo` bigint (20),
	`NoResultado` varchar (150),
	`Reporte` char (6),
	`FfechaReporte` date ,
	`EstadoNoResultado` char (3),
	`ObservacionResuelto` varchar (600),
	`DatoBusqueda` varchar (600),
	`Edad` Decimal (11),
	`Medida_edad` char (3),
	`MedicoResultado` varchar (45)
); 
