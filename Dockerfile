FROM lsiobase/ubuntu:jammy AS base

ARG DEBIAN_FRONTEND=noninteractive
ENV PATH=$PATH:/root/.dotnet:/root/.dotnet/tools
ENV DOTNET_ROOT=/root/.dotnet

# Add intel hardware encoding support
ENV LIBVA_DRIVERS_PATH="/usr/lib/x86_64-linux-gnu/dri" \
    LD_LIBRARY_PATH="/usr/lib/x86_64-linux-gnu" \
    NVIDIA_DRIVER_CAPABILITIES="compute,video,utility" \
    NVIDIA_VISIBLE_DEVICES="all" \
    DOTNET_CLI_TELEMETRY_OPTOUT=true

ARG DEPS="git wget dos2unix libssl-dev comskip mkvtoolnix aom-tools svt-av1 x265 x264 nano"
ARG VAAPI_DEPS="vainfo intel-media-va-driver-non-free libva-dev libmfx-dev intel-media-va-driver-non-free intel-media-va-driver-non-free i965-va-driver-shaders mesa-va-drivers"
RUN apt-get update && \
    apt-get install -y software-properties-common && \
    add-apt-repository universe && \
    apt-get update && \
    #ARCH=$(dpkg --print-architecture) && \
    #if [ $ARCH -eq 'amd64' ]; \
    #then apt-get install -y ${DEPS} ${VAAPI_DEPS}; \
    #else apt-get install -y ${DEPS}; \
    #fi && \
    apt-get install -y ${DEPS} ${VAAPI_DEPS}; \
    rm -rf /var/lib/apt/lists/*

# Install ffmpeg from jellyfin
ARG FFMPEG_URL="https://github.com/jellyfin/jellyfin-ffmpeg/releases/download/v4.4.1-4/jellyfin-ffmpeg_4.4.1-4-jammy"
RUN ARCH=$(dpkg --print-architecture) && \
    wget "${FFMPEG_URL}_${ARCH}.deb" && \
    apt-get update && \
    apt-get install -y ./jellyfin-ffmpeg*.deb && \
    rm -rf ./jellyfin-ffmpeg*.deb && \
    ln /usr/lib/jellyfin-ffmpeg/ffmpeg /usr/local/bin/ffmpeg && \
    rm -rf /var/lib/apt/lists/* && \
    ffmpeg --help

##########################################
### actual FileFlows stuff happens now ###
##########################################
FROM base

# Install dotnet SDK
RUN wget https://dot.net/v1/dotnet-install.sh  && \
    bash dotnet-install.sh -c Current && \
    rm -f dotnet-install.sh

# copy the deploy file into the app directory
COPY /deploy /app
COPY /deploy/Plugins /app/Server/Plugins
COPY /docker-entrypoint.sh /app/docker-entrypoint.sh

# expose the ports we need
EXPOSE 5000

RUN dos2unix /app/docker-entrypoint.sh && \
    chmod +x /app/docker-entrypoint.sh

# add dotnet to path so can run dotnet anywhere
ENV PATH="/root/.dotnet:${PATH}"

# set the working directory
WORKDIR /app

ENTRYPOINT ["/app/docker-entrypoint.sh"]