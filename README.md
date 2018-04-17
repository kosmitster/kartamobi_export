# README #

### Для чего этот репозиторий? ###
---
Экспортёр информации о движених бонусов из системы 1С-Рарус РестАрт ред. 3: Депозитно-дисконтный сервер (https://rarus.ru/1c-restoran/restart-administrator-dds/) в систему Karta.Mobi (https://karta.mobi)
Version 1.0

### Как мне запустить в работу? ###
---
**Общее описание**
Для экспорта ExportToService.exe должен запускаться каждые 5 минут. 
Настроить это можно с помощью scheduling windows или настроить job в MS SQL  

**Настройка**
Настройки программы находятся в файле ExportToService.exe.config	

```sh
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <startup> 
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.5.2"/>
  </startup>
  <appSettings>
    <!--Настройки для хранения лога-->
    <add key="LogFilePath" value="C:\Temp"/> <!--путь к лог файлу-->
    <!--Настройки для получения данных-->
    <add key="DB_InitialCatalog" value="ddd"/> <!--название MS SQL базы-->
    <add key="DB_dataSource" value="ServerDDS"/> <!--сервер MS SQL-->
    <add key="DB_login" value="sa"/> <!--логин MS SQL-->
    <add key="DB_password" value="superPassword"/> <!-- пароль MS SQL-->
    <!--Настройки для записи данных-->
    <add key="SqliteFilePath" value="C:\Temp\Db.sqlite"/> <!-- путь к файлу базы SQLite -->
    <!--Настройки для отправки данных-->
    <add key="KartaMobi_btoken" value="***"/> <!-- b_token клиента в Karta.Mobi -->
    <add key="KartaMobi_login" value="***"/> <!-- логин клиента в Karta.Mobi -->
    <add key="KartaMobi_password" value="***"/> <!-- пароль клиента в Karta.Mobi -->
  </appSettings>
</configuration>
```

**Dependencies**
* Newtonsoft.Json
* RestSharp
* System.Data.SQLite.Core
* System.ValueTuple

**Инструкция публикации**
1. Скомпилируйте проект
2. Создайте папку, например C:\ExportToService
3. Поместите в созданную папку следующие файлы:
    * ExportToService.exe - исполняемый файл
    * ExportToService.exe.config - файл конфигурации
    * Newtonsoft.Json.dll
    * RestSharp.dll
    * System.Data.SQLite.dll
    * System.ValueTuple.dll
    * \x86\SQLite.Interop.dll
    * \x64\SQLite.Interop.dll
4. Организуйте запуск ExportToService.exe на ежедневной основе каждые 5 минут. Это можно сделать с помощью scheduling windows или настроить job в MS SQL.  
		
**Описание результатов работы**
* В процессе работы программа получает информации о транзакциях начисления и списания бонусов из базы 1С-Рарус РестАрт ред. 3: Депозитно-дисконтный сервер за последние 5 минут
* Отправляет информацию в сервис Karta.Mobi, одновременно с этим пишет в Лог (LogFilePath) подробные результаты своей работы.
* Так же пишет информацию в базу SQLite (SqliteFilePath) 
    * в таблицу SentTransactions - успешно отправленные транзакции
    * в таблицу ErrorTransactions - сбойные транзакции (в момент отправки недоступен интернет, недоступен сервис Karta.Mobi и прочее)
* Перед отправкой "свежих" транзакций, отправляются "сбойные транзакции" из SQLite.ErrorTransactions
