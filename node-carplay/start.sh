#!/bin/bash

#disable login shell
sudo systemctl stop getty@tty1

#disable cursor blink
echo -e '\033[?17;0;0c' > /dev/tty1

#mkfifo /tmp/video

node .
