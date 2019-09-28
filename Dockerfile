FROM acvcmaster/mediasync-base

# Copy files
WORKDIR /root/
ADD . ./MediaSync-API
WORKDIR /root/MediaSync-API

# Restore and build
RUN dotnet restore
RUN dotnet publish -c Docker
WORKDIR /root/MediaSync-API/bin/Docker/netcoreapp2.2/publish
RUN mkdir Media

EXPOSE 8080/tcp

ENTRYPOINT ["dotnet", "MediaSync.dll"]