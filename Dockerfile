FROM microsoft/dotnet:latest

WORKDIR /root/
ADD . ./MediaSync-API/
WORKDIR /root/MediaSync-API

RUN dotnet restore
RUN dotnet publish -c Release
WORKDIR /root/MediaSync-API/bin/Release/netcoreapp2.2/publish
run mkdir Media
run apt update
run apt install -y ffmpeg wget
run wget https://yt-dl.org/latest/youtube-dl -O /usr/bin/youtube-dl
run chmod a+x /usr/bin/youtube-dl
run hash -r
run youtube-dl -f bestvideo+bestaudio/best -o "Media/%(title)s.%(ext)s" 'https://www.youtube.com/watch?v=AufydOsiD6M'
run youtube-dl -f bestvideo+bestaudio/best -o "Media/%(title)s.%(ext)s" 'https://www.youtube.com/watch?v=vpYkz5WU1Vg'

EXPOSE 8080/tcp 

ENTRYPOINT ["dotnet", "MediaSync.dll"]