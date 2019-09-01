FROM microsoft/dotnet:2.2-sdk-bionic

# Copy files
WORKDIR /root/
ADD . ./MediaSync-API/
WORKDIR /root/MediaSync-API

# Restore and build
RUN dotnet restore
RUN dotnet publish -c Docker
WORKDIR /root/MediaSync-API/bin/Docker/netcoreapp2.2/publish
RUN mkdir Media

# Install dependencies
RUN apt update
RUN apt install -y ffmpeg wget locales

# Generate and set locales for Unicode support
RUN sed -i -e 's/# en_US.UTF-8 UTF-8/en_US.UTF-8 UTF-8/' /etc/locale.gen && \
    locale-gen
ENV LANG en_US.UTF-8  
ENV LANGUAGE en_US:en  
ENV LC_ALL en_US.UTF-8     

# Download samples
# RUN wget https://yt-dl.org/latest/youtube-dl -O /usr/bin/youtube-dl
# RUN chmod a+x /usr/bin/youtube-dl
# RUN hash -r
# RUN youtube-dl -f bestvideo+bestaudio/best -o "Media/%(title)s.%(ext)s" 'https://www.youtube.com/watch?v=AufydOsiD6M'

EXPOSE 8080/tcp 

ENTRYPOINT ["dotnet", "MediaSync.dll"]