
# BookStore_Ass2 #
### BookStore assignment for COMP5348 ###

How to build and run the solution
1. Open the repository and select BookStore.sln to open
2. Build the solution by right clicking on Solution BookStore node and pressing Build Solution
3. Create database
a. First select Tools > SQL Server > New Query
b. Then expand Local and select MSSQLLocalDb. Then select Connect
c. Write  'create database BookStore; and SQL>Execute the file
d. Do the above for all the databases (Bank and DeliveryCo). The commands for this are 'create database Bank;' then SQL>Execute the file. You will also need to do 'create database DeliveryCo;'
4. Create schema for the database
a. Find the file BookStore.Business/BookStore.Business.Entities/BookStoreEntiyModel.edmx.sql. Click on this file and execute the code
b. Expand Local and select MSSQLLocalDb and then Connect
c. You will also need to do this for BankEntityModel.edmx.sql and DeliveryCoEntityModel.edmx.sql
5. Running the code
a. Right click on BookStore.Application/BookStore.Process project,  click Debug > Start New Instance for the application server. You will also need to do this for Bank.Process, DeliveryCo.Process, Email.Process
b. Right click on BookStore.Presentation/BookStore.WebClient, click Debug > Start New Instance for the webserver server

Installing Windows Message Queues (this will only work on a Windows computer)
1. Open the Control Panel. Select Programs> Programs and Features> Turn Windows Features on and off
2. Check the box for Microsoft Message Queue Server and expand the folder
3. Check the box for Microsoft Message Queue Server Core and check the box
4. You may also need to select the box for MSMQ HTTP Support and MSMQ Active Directory Domain Service
5. Click OK and restart your computer if prompted
