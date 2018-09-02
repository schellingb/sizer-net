# Sizer.Net
This is a tool that shows the size of things (types, methods, static arrays, etc.) in a .NET assembly.

![Screenshot](https://raw.githubusercontent.com/schellingb/sizer-net/master/README.png)

## Download
You can find a binary download under the [Releases page](https://github.com/schellingb/sizer-net/releases/latest).

## Usage
On launch it shows a file selection dialog with which you can load any kind of .NET assembly (exe or dll).  
Once loaded, it shows the things stored in the assembly in a tree view with the accumulated size on the right.  
The usual structure is:  
[assembly name] → [namespace] → [class] → [thing]

## Command line
If launched via command line, it takes a path to an exe or dll file as the first argument.

## Accuracy
The tool is not fully accurate as it uses the simple approach of inspection via .NET's built-in reflection.  
While the size of actual byte instructions of functions is correct, overhead from types, fields, etc. is estimated.

A more accurate method would be to use a custom .NET metadata reader library like dnlib that could read byte-accurate information.

## Unlicense
Sizer.Net is available under the [Unlicense](http://unlicense.org/).
