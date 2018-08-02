# README #

### Для чего этот репозиторий? ###
---
Экспортёр информации о движених бонусов из системы 1С-Рарус РестАрт ред. 3: Депозитно-дисконтный сервер (https://rarus.ru/1c-restoran/restart-administrator-dds/) в систему Karta.Mobi (https://karta.mobi)
Version 1.0

### Как мне запустить в работу? ###
---
**Общее описание**

Программа каждую минуту проверяет наличие "свежих" транзакций в системе 1С-Рарус РестАрт ред. 3: Депозитно-дисконтный сервер, отправляет данные в систему Karta.Mobi

**Dependencies**

* Newtonsoft.Json
* RestSharp
* System.Data.SQLite.Core
* System.ValueTuple
* WIX TOOLSET (http://wixtoolset.org/)

**Инструкция сборки**

1. Необходимо скачать и установить WIX TOOLSET (http://wixtoolset.org/) WiX Toolset Visual Studio 2017 Extension и WiX v3.11.1 (Stable)
2. Скомпилировать проект KartaMobiExporter.Install в Visual Studio community 2017 в режиме Release для той архитектуры которая необходима (x64 или x86)
3. Результат x86  \KartaMobi\Build\x86\Release\KartaMobiExporter.Install\en-us\KartaMobiExporter-Release-x86.msi
4. Результат x64  \KartaMobi\Build\x64\Release\KartaMobiExporter.Install\en-us\KartaMobiExporter-Release-x64.msi

**Инструкция запуска**

1. Установите программу KartaMobiExporter-Release-[АрхитектураВашегоПроцессора].msi
2. Заполните настройки подключения нажимите СОХРАНИТЬ
3. Нажмите ПУСК 	
**Описание результатов работы**

* В процессе работы программа получает информации о транзакциях начисления и списания бонусов из базы 1С-Рарус РестАрт ред. 3: Депозитно-дисконтный сервер каждую минуту
* Отправляет информацию в сервис Karta.Mobi, одновременно с этим пишет в LOG подробные результаты своей работы. 
* Так же пишет информацию в базу SQLite 
    * в таблицу SentTransactions - успешно отправленные транзакции
    * в таблицу ErrorTransactions - сбойные транзакции (в момент отправки недоступен интернет, недоступен сервис Karta.Mobi и прочее)
* Перед отправкой "свежих" транзакций, отправляются "сбойные транзакции" из SQLite.ErrorTransactions
* База SQLite и LOG находятся в каталоге c:\Users\ПОЛЬЗОВАТЕЛЬ\AppData\Local\KartaMobi\
