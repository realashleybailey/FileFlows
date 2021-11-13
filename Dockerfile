FROM jrottenberg/ffmpeg:4.1-nvidia
#FROM ubuntu:20.04
#FROM alpine:3.14

# expose the ports we need
EXPOSE 5000

# install libssl-dev, needed for the asp.net application to run
RUN apt-get update \
    && apt-get upgrade -y \
    && apt-get dist-upgrade -y \
    && apt-get install -fy \
    libssl-dev

ENV MYSQL_ROOT_PASSWORD=root
ENV MYSQL_ROOT_USER=password
ENV DEBIAN_FRONTEND=noninteractive

# install mysql
# RUN apt-get update \
#     && apt-get install -y apt-utils \ 
#     && apt-get -q -y install mysql-server 
# RUN usermod -d /var/lib/mysql/ mysql
# RUN service mysql start 
RUN apt-get update && apt-get install -y mysql-server

# install ffmpeg
#RUN apt-get update \ 
#    && apt-get upgrade -y \
#    && apt-get install -y ffmpeg
#
#RUN echo $(ffmpeg -version)
#
## install nvidia driver
#ENV DEBIAN_FRONTEND noninteractive
#RUN apt update
#RUN apt install software-properties-common -y
#RUN add-apt-repository ppa:graphics-drivers 
#RUN apt install nvidia-driver-440 -y

# copy the publish into the app 
COPY /zpublish /app

# set mysql data directory
# RUN mkdir -p /app/Data/mysql
# RUN ln /app/Data/mysql /var/lib/mysql
#RUN chown mysql:mysql /app/Data/mysql
# RUN chown -R 777 /app/Data/mysql
RUN usermod -d /var/lib/mysql/ mysql
# RUN echo '[mysqld]\ndatadir = /app/Data/mysql' > /etc/my.cnf
# COPY my.cnf /etc/my.cnf
# RUN chown -R 777 /etc/my.cnf

# set the working directory
WORKDIR /app

# run the server
ENTRYPOINT [ "/app/Server", "--urls", "http://*:5000" ]