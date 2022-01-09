FROM lsiobase/ubuntu:focal
#FROM mcr.microsoft.com/dotnet/sdk:6.0-focal AS build

############################################################ 
### Prepare the docker with ffmpeg and hardware encoders ###
############################################################

ENV LIBVA_DRIVERS_PATH="/usr/lib/x86_64-linux-gnu/dri" \
    LD_LIBRARY_PATH="/usr/lib/x86_64-linux-gnu" \
    NVIDIA_DRIVER_CAPABILITIES="compute,video,utility" \
    NVIDIA_VISIBLE_DEVICES="all" 

# ffmpeg from jellyfin, a little older but precompiled for us
RUN apt-get update && \
    apt install -y wget && \
    wget https://repo.jellyfin.org/releases/server/ubuntu/versions/jellyfin-ffmpeg/4.3.2-1/jellyfin-ffmpeg_4.3.2-1-focal_amd64.deb && \
    apt install -y \
    ./jellyfin-ffmpeg_4.3.2-1-focal_amd64.deb && \
    # link to /user/local/bin to make it available globally
    ln -s /usr/lib/jellyfin-ffmpeg/ffmpeg /usr/local/bin/ffmpeg

#  add support for intel hardware enconding
RUN curl -s https://repositories.intel.com/graphics/intel-graphics.key | apt-key add - && \
    # add the intel repo to the sources
    echo 'deb [arch=amd64] https://repositories.intel.com/graphics/ubuntu focal main' > /etc/apt/sources.list.d/intel-graphics.list && \
    # update the apt-get repo
    apt-get update && \
    apt-get install -y --no-install-recommends \
    # do the actual intel install
    intel-media-va-driver-non-free vainfo mesa-va-drivers

# install libssl-dev, needed for the asp.net application to run
RUN apt-get update \
    && apt-get upgrade -y \
    && apt-get dist-upgrade -y \
    && apt-get install -fy \
    libssl-dev

RUN wget https://dot.net/v1/dotnet-install.sh \
    && bash dotnet-install.sh -c Current


##########################################
### actual FileFlows stuff happens now ###
##########################################

# expose the ports we need
EXPOSE 5000

# copy the deploy file into the app directory
COPY /deploy /app

# set the working directory
WORKDIR /app

# run the server
ENTRYPOINT [ "dotnet", "FileFlows.Server.dll", "--urls=http://*:5000", "--docker" ]