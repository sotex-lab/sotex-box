import boto3
import json

client = boto3.client("sqs",
						endpoint_url="http://localhost:9324",
						region_name="elasticmq",
						aws_secret_access_key='x',
						aws_access_key_id='x',
						use_ssl=False)

response = client.receive_message(
    QueueUrl='/000000000000/nonprocessed',
    MaxNumberOfMessages=5,
    VisibilityTimeout=5,
    WaitTimeSeconds=5,
)

print(response)
