FROM ghcr.io/prefix-dev/pixi:0.23.0

WORKDIR /app

COPY python/local-pusher.py /app/minio_webhook_to_sqs.py
COPY pixi.* /app/

RUN pixi install

CMD ["pixi", "run", "python", "minio_webhook_to_sqs.py"]
