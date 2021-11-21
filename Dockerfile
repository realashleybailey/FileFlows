FROM jrottenberg/ffmpeg:4.1-nvidia

# expose the ports we need
EXPOSE 5000

# install libssl-dev, needed for the asp.net application to run
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