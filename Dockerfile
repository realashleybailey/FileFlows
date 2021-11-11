FROM ubuntu:20.04
#FROM alpine:3.14

# expose the ports we need
EXPOSE 5000

# install libssl-dev, needed for the asp.net application to run
RUN apt-get update \
    && apt-get upgrade -y \
    && apt-get dist-upgrade -y \
    && apt-get install -fy \
    libssl-dev

# install nvidia driver
ENV DEBIAN_FRONTEND noninteractive
RUN apt update
RUN apt install software-properties-common -y
RUN add-apt-repository ppa:graphics-drivers 
RUN apt install nvidia-driver-440 -y

# install ffmpeg
RUN apt-get install -y ffmpeg

# copy the publish into the app 
COPY /zpublish /app

# set the working directory
WORKDIR /app

# run the server
ENTRYPOINT [ "/app/Server", "--urls", "http://*:5000" ]