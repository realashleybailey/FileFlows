This is how you can configure FileFlows to run as a systemd service in linux.

Service file
```
[Unit]
Description=FileFlows

[Service]
# if /usr/bin/dotnet doesn't work, use 'which dotnet' to find correct dotnet executable path
ExecStart=/usr/bin/dotnet /home/USER_HERE/FileFlows/FileFlows.Server.dll --no-gui
SyslogIdentifier=FileFlows
WorkingDirectory=/home/USER_HERE/FileFlows
User=root
Restart=always
RestartSec=5
Environment=DOTNET_ROOT=/usr/lib64/dotnet

[Install]
WantedBy=multi-user.target
```
Save the systemd file under
/etc/systemd/system/fileflows.service

Then run
```
sudo systemctl daemon-reload
sudo systemctl start fileflows.service
```

To check the status
```
sudo systemctl status fileflows.service
```

To run it on startup, enable the service using
```
sudo systemctl enable fileflows.service
sudo systemctl daemon-reload
```


## Script to Download FileFlows and Configure systemd
NOTE: To update the USER field
```
#! /bin/bash

#INPUT USER BEFORE EXECUTING!
USER="USER_GOES_HERE"
DIR_HOME="/home/$USER/"
cd $DIR_HOME
DIR_FF="/home/$USER/FileFlows"
if [ -d "$DIR_FF" ]; then
  #$DIR exists
  echo "FileFlows folder found, deleting everything except Data for update..."
  cd $DIR_FF
  find -maxdepth 1 ! -name Data ! -name . -exec rm -rv {} \;
  cd $DIR_HOME
else
  echo "FileFlows folder not found, creating..."
  mkdir FileFlows
fi
wget -O FileFlows.zip     https://fileflows.com/downloads/server-zip
unzip FileFlows.zip -d FileFlows
rm FileFlows.zip
echo "Checking systemd service file"
FILE=/etc/systemd/system/fileflows.service
if test -f "$FILE"; then
  echo "$FILE exists, restarting service..."
  systemctl restart fileflows.service
else
  echo "FILE doesn't exist, creating..."
  cat > $FILE <<EOF
[Unit]
Description=FileFlows

[Service]
# if /usr/bin/dotnet doesnt work, use which dotnet to find correct dotnet executable path
ExecStart=/usr/bin/dotnet /home/$USER/FileFlows/FileFlows.Server.dll --no-gui
SyslogIdentifier=FileFlows
WorkingDirectory=/home/$USER/FileFlows
User=root
Restart=always
RestartSec=5
Environment=DOTNET_ROOT=/usr/lib64/dotnet

[Install]
WantedBy=multi-user.target
EOF
echo "Enabling and starting service..."
systemctl daemon-reload
systemctl enable fileflows.service
systemctl start fileflows.service
fi
echo "All done!"
```