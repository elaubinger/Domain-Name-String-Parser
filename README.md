# Domain-Name-String-Parser
Finds valid top level domains to split into a string (i.e cleverme -> clever.me)

Syntax
------
dotnet_path DomainStringParser.dll name [-offline]

Sample: "C:\Program Files\dotnet\dotnet.exe" DomainStringParser.dll cleverme

Arguments
---------
name: Name of file to check against, required argument

-offline: Optional flag which forces deferment to the offline copy of top level domains
