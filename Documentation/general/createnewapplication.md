Creating New Application:

## To create new MVC application

command:- dotnet new MVC

This will create new MVC application in the current directory.

Use “console” as argument instead of MVC to create a console application and “library” to create a class library project.


## To restore all Nuget package, the command is,

dotnet restore

This will restore all the required packages as seen above.


## To build application,

dotnet build

 
## To run application,

dotnet run

This will run the application and start the Kestrel webserver which will listen to port 5000 for incoming request. Visiting http://localhost:5000 will bring app like below.


## Alternatively, you can also run the app by using the below command.

dotnet appdll.dll

 



 
