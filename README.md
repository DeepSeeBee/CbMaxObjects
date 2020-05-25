# CbMaxObjects
CLR Adapter for cycling75.max objects and the CbChannelStrip project (dynamic signal flow graph)

## Clone

Clone the repository in your max path which may look like:
C:\Program Files\Cycling '74\Max 8\packages\max-sdk-8.0.3\externals\charly_beck

Using this path will ensure that the project runs though there are some hardcoded development paths around.

## IDE
Open .sln file in Visual Studio 2019.

## Compile

There is an external package included not submitet to git. Select the 'CbMaxClrAdapter' project as active project and call tools->Nu Get Package manager console and type

> Install-Package UnmanagedExports -Version 1.2.7

Problem 1: It happened, that visual studio tells you the package is allready installed. In this case you must delete the assembly reference and the entry in the packages.config.

Problem 2: It happened, that visual studio installs the package in a random project in the workspace. In this case remove all projects from the solution only leaving the 'CbMaxClrAdapter' project and repeat the "Install package" command.

Problem 3: It happened visual studio opened the root solution when opening the project as single. This is why you should remove all projects except the one. Life could be so simple ;-p

## Quickstart

Look at class CbChannelStrip.CTestObject

This is a sample Max Object called 'External' implemented using the C# Adapter's (base) classes. The code should be obvious at this state when knowing how max objects work.

## License

Trust me, you don't wanna use the project in this state. It's the beta of all betas ;) When it's ready i will think of a license. In the meantime: CCC Attribution, Non commercial, no derivates, free to use for your private device testing. 

## Feature requests
Are welcome.

## Beware

- The stuff is not yet thread protected. Thus receiving messages from overdrive thread could cause runtime errors or unpredictable results. switch overdrive off or don't receive timecritical messages from from metro or other such objects on the same inlet. don't send messages from different threads to the same outlet. Sync the stuff on your own but you prefer using a new System.object for sync(obj) {} sections. if you deal with multithreading be sure to understand the handling of the field CbMaxClrAdapter.CConnector.Messages for inlets and outlets.

