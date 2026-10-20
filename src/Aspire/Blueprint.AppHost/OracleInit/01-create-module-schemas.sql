-- Wird vom Oracle-Container beim ERSTEN Start ausgeführt (leeres Datenvolume).
-- Ein Schema (= Benutzer ohne Login) pro Modul → klare Datenhoheit, spätere Auslagerung pro Modul möglich.
-- Remarks: https://docs.oracle.com/en/database/oracle/oracle-database/23/sqlrf/CREATE-USER.html
--          https://learn.microsoft.com/dotnet/architecture/microservices/architect-microservice-container-applications/data-sovereignty-per-microservice
ALTER SESSION SET CONTAINER = FREEPDB1;

CREATE USER CATALOG NO AUTHENTICATION DEFAULT TABLESPACE USERS QUOTA UNLIMITED ON USERS;
CREATE USER ORDERING NO AUTHENTICATION DEFAULT TABLESPACE USERS QUOTA UNLIMITED ON USERS;
