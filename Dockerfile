FROM microsoft/dotnet:2.2-sdk-bionic

# Copy files
WORKDIR /root/
ADD . ./MediaSync-API
WORKDIR /root/MediaSync-API

# Restore and build
RUN dotnet restore
RUN dotnet publish -c Docker
WORKDIR /root/MediaSync-API/bin/Docker/netcoreapp2.2/publish
RUN mkdir Media

# Install dependencies
RUN apt update
RUN apt install -y ffmpeg locales

# Generate and set locales for Unicode support
RUN sed -i -e 's/# en_US.UTF-8 UTF-8/en_US.UTF-8 UTF-8/' /etc/locale.gen && \
    locale-gen
ENV LANG en_US.UTF-8 
ENV LANGUAGE en_US:en 
ENV LC_ALL en_US.UTF-8

EXPOSE 8080/tcp

ENTRYPOINT ["dotnet", "MediaSync.dll"]