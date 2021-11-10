FROM ubuntu:20.04
#FROM alpine:3.14

# expose the ports we need
EXPOSE 5000

RUN apt-get update \
    && apt-get upgrade -y \
    && apt-get dist-upgrade -y \
    && apt-get install -fy \
    libssl-dev

# copy the publish into the app 
COPY /zpublish /app

# set the working directory
WORKDIR /app

# run the server
ENTRYPOINT [ "/app/Server", "--urls", "http://*:5000" ]