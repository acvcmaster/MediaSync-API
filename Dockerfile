FROM microsoft/dotnet:latest

WORKDIR /root/
ADD . ./MediaSync-API/
WORKDIR /root/MediaSync-API

RUN dotnet restore
RUN dotnet publish -c Release
WORKDIR /root/MediaSync-API/bin/Release/netcoreapp2.2/publish

EXPOSE 8080/tcp 

ENTRYPOINT ["dotnet", "MediaSync.dll"]