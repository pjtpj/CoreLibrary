CoreLibrary
Copyright (C) 2006 - 2009 Teztech, Inc.
All Rights Reserved.

OVERVIEW

CoreLibrary is a C# library which contains standard routines used by Teztech 
C# applications. This library should be able to be used by:

UNIX  + Daemon Process, Command Line Tool or CGI Process
Win32 + Service, Command Line Tool, CGI Process or GUI Application

This is a large set of build targets, but we are not yet trying to provide 
architectural classes for services or GUI's. Hopefully the .Net CRTL will 
provide most of that (Mono on UNIX and .Net on Win32).

Application projects  used to contain independent copies of CoreLibrary. Starting 
6/13/2009, copies of CoreLibrary were similar enough that I decided to make 
CoreLibrary a versioned independently root level project. When applications
are updated, then should be modified to use the root level CoreLibrary project.
