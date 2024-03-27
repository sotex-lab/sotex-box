FROM docker:25.0.4-dind-alpine3.19

ENV PYTHONUNBUFFERED=1
RUN apk add --update --no-cache python3 py3-pip make git && ln -sf python3 /usr/bin/python
RUN pip install --no-cache --upgrade pip setuptools --break-system-packages
ENV CONTAINER_TOOL=docker

WORKDIR /sotex
COPY .env .env

RUN git config --global --add safe.directory /sotex
