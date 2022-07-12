---
name: Touch
title: Touch
permalink: /plugins/basic-nodes/touch
layout: default
plugin: Basic Nodes
parent: Basic Nodes
grand_parent: Plugins
---

{% include node.html input=1 outputs=1 icon="fas fa-hand-point-right" name="Touch" type="Process" %}

Touches a file or directory and sets the last write time to now.

### File Name
Full filename of file or folder to touch.
If left blank the working file will be used.