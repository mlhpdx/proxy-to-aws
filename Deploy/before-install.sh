#!/bin/bash
sudo mkdir /var/paws

sudo useradd -s /sbin/nologin pawsuser
sudo chown -R pawsuser:pawsuser /var/paws

echo continue