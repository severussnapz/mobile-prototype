#!/bin/bash
# Creates the S3 bucket for artefact storage on LocalStack startup.
# Mounted into the container at /etc/localstack/init/ready.d/ so it runs once
# LocalStack reports ready.
awslocal s3 mb s3://genesis-ai-artefacts
echo "Created S3 bucket: genesis-ai-artefacts"

# Upload seed artefact content so local seed-local.sql metadata rows resolve.
if [ -d /etc/localstack/seed-artefacts ]; then
  awslocal s3 cp /etc/localstack/seed-artefacts/ s3://genesis-ai-artefacts/ --recursive
  echo "Uploaded seed artefacts to s3://genesis-ai-artefacts"
fi
