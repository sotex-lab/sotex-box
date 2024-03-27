from flask import Flask, request, jsonify
import boto3
import sys

app = Flask(__name__)

client = boto3.resource("sqs",
						endpoint_url="http://sqs:9324",
						region_name="elasticmq",
						aws_secret_access_key='x',
						aws_access_key_id='x',
						use_ssl=False)
queue = client.get_queue_by_name(QueueName='nonprocessed')

@app.route("/webhook", methods=['POST'])
def webhook():
	try:
		data = request.data.decode('utf-8')
		sys.stderr.write('Request Body: {}\n'.format(data))
		sys.stderr.flush()

		response = queue.send_message(MessageBody=data)
		sys.stderr.write('Sent message to queue: {}\n'.format(response.get('MessageId')))
		sys.stderr.flush()

		return jsonify({'message': 'Event received and sent to SQS successfully'})
	except Exception as e:
		sys.stderr.write('Request Body: {}\n'.format(str(e)))
		sys.stderr.flush()
		return jsonify({'error': str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)
