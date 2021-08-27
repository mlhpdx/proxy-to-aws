# Proxy to Amazon Web Services (PAWS)

This repo contains a snapshot of version 1.0.9 of PAWS. PAWS is a console application and nuget library that proxy message-oriented network protocols to AWS services. The main purpose of PAWS is to allow binary messages to be processed in serverless AWS resources. Support for inbound UDP, ICMP, TCP and SSL is included as are outbound destinations in AWS Lambda, Firehose, SNS, SQS, S3 and StepFunctions.  If you want to proxy to non-serverless destinations like EC2 instances or IPs, this is not the project for you.

PAWS is a "work in progress" open source project, licensed under the GPL (see LICENSE file) for the time being.