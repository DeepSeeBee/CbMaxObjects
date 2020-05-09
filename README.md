# CbMaxObjects
CLR Adapter for cycling75.max objects and the CbChannelStrip project (dynamic signal flow graph)

## Clone

Clone the repository in your max path which may look like:
C:\Program Files\Cycling '74\Max 8\packages\max-sdk-8.0.3\externals\charly_beck

Using this path will ensure that the project runs though there are some hardcoded development paths around.

## Compile

There is an external package included not submitet to git. Select the 'CbMaxClrAdapter' project as active project and call tools->Nu Get Package manager console and type

> Install-Package UnmanagedExports -Version 1.2.7

Problem 1: It happened, that visual studio tells you the package is allready installed. In this case you must delete the assembly reference and the entry in the packages.config.

Problem 2: It happened, that visual studio installs the package in a random project in the workspace. In this case remove all projects from the solution only leaving the 'CbMaxClrAdapter' project and repeat the "Install package" command.

Problem 3: It happened visual studio opened the root solution when opening the project as single. This is why you should remove all projects except the one. Life could be so simple ;-p

## Quickstart

Look at CbChannelStrip\CChannelStrip.cs

This is a sample Max Object called 'External' implemented using the C# Adapter's (base) classes. The code should be ovious at this state.
