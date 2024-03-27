FROM python:3.10

WORKDIR /app

COPY requirements.txt /app/requirements.txt
RUN pip install -r requirements.txt

COPY python/local-pusher.py /app/minio_webhook_to_sqs.py

CMD ["python", "minio_webhook_to_sqs.py"]
