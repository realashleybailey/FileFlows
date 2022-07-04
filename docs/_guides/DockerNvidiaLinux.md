---
title: Docker NVIDIA Linux
name: Docker NVIDIA Linux
permalink: /guides/docker-linux-nvidia
layout: default
---

This is a generic guide to installing NVIDIA support on a docker instance running on a linux machine.

### Step 1. Drivers

Install the latest NVIDIA Drivers

Should be something along the lines of this
```bash
sudo apt install nvidia-driver-515
```
Once installed, reboot.

### Step 2. NVIDIA Docker Runtime

Next you the NVIDIA runtime for docker 

https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/archive/1.10.0/install-guide.html#docker

This is a long guide, go slowly and follow each step closely.

### Step 3. FileFlows

Next its time to get FileFlows working.  This requires the "nvidia" runtime for docker from step 2.

```
sudo docker run -d \
--name=FileFlows \
-p 5000:5000 \
-v /mnt/docker/fileflows/data:/app/Data \
-v /mnt/docker/fileflows/logging:/app/Logs \
-v /mnt/docker/fileflows/temp:/temp \
-v /mnt/media:/media \
--runtime=nvidia \
--restart unless-stopped \
revenz/fileflows
```