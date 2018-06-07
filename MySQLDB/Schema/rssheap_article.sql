-- MySQL dump 10.13  Distrib 5.7.17, for Win64 (x86_64)
--
-- Host: 192.99.232.179    Database: rssheap
-- ------------------------------------------------------
-- Server version	5.7.17-log

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `article`
--

DROP TABLE IF EXISTS `article`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `article` (
  `Id` int(10) NOT NULL AUTO_INCREMENT,
  `FeedId` int(11) NOT NULL,
  `Name` longtext NOT NULL,
  `Body` longtext,
  `Url` longtext CHARACTER SET utf8,
  `ViewsCount` int(10) NOT NULL DEFAULT '0',
  `LikesCount` int(10) NOT NULL DEFAULT '0',
  `FavoriteCount` int(11) NOT NULL DEFAULT '0',
  `Published` datetime DEFAULT NULL,
  `Created` datetime DEFAULT NULL,
  `Indexed` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Hash` longtext,
  `ShortUrl` varchar(200) DEFAULT NULL,
  `Flagged` bit(1) NOT NULL DEFAULT b'0',
  `FlaggedBy` varchar(200) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UQ_Article_Hash` (`Hash`(190)),
  UNIQUE KEY `UQ_ShortUrl` (`ShortUrl`(190)),
  KEY `IX_Feed` (`FeedId`),
  KEY `IX_Article_ShortUrl` (`ShortUrl`(190)),
  KEY `AR_Published` (`Published`),
  KEY `AR_Votes` (`LikesCount`),
  CONSTRAINT `FK_Article_Feed` FOREIGN KEY (`FeedId`) REFERENCES `feed` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=2338570 DEFAULT CHARSET=utf8mb4;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2018-06-07 11:07:11
