#!/bin/bash
sudo cp ./config/ProxyConfig.json /var/paws

sudo chmod +x /var/paws/ProxyApp

sudo iptables -A INPUT -p udp -m multiport --dports 53,514,1812,1813 -j ACCEPT
sudo iptables -A INPUT -p tcp -m multiport --dports 80,443,514,1810,2083,6514 -j ACCEPT
	
