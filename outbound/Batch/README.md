# Altinn Batch Receiver Samples

There are three solutions in this sample:
* AltinnBatchReceiverService
* AltinnReceiverWithXmlSerialization
* AltinnSimulator

## AltinnBatchReceiverService
This is the server which receives messages from Altinn. To test the server:

* Make sure you have .NET 4.5.2 installed
* Open the solution in Visual Studio 2015 or later.
* Run

Two folders will be created when running the test clinet - AltinnSimulator:
* Log
  Contains log.txt
* Inbox
  Contains the received messages

The Service will also log to Windows Application Log.

There is more documentation within the source files.

## AltinnReceiverWithXmlSerialization
Another service which receives form requests from Altinn. 

Contains a SoapUI test project with example xml requests.


* Make sure you have .NET 4.5.2 installed
* Open the solution in Visual Studio 2015 or later.
* Run

## AltinnSimulator
This console application simulates Altinn by fetching testdata and sending it to the AltinnBatchReceiverService.
It fetches data from the Testdata folder.
In order to run:

* Make sure you have .NET 4.5.2 installed
* Open the solution in Visual Studio 2015 or later.
* Run the AltinnBatchReceiverService
* Select Properties -> Debug, edit the Command Line: folder ..\\..\\Tesdata
* Run the console application
