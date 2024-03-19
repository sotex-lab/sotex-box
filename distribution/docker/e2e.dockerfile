FROM docker:25.0.4-dind-alpine3.19

ENV PYTHONUNBUFFERED=1
RUN apk add --update --no-cache python3 py3-pip make git && ln -sf python3 /usr/bin/python
RUN pip install --no-cache --upgrade pip setuptools --break-system-packages
ENV CONTAINER_TOOL=docker

WORKDIR /sotex

COPY .git .git
COPY distribution distribution

COPY dotnet/backend dotnet/backend
COPY dotnet/model dotnet/model
COPY dotnet/persistence dotnet/persistence
COPY dotnet/sse-handler dotnet/sse-handler

COPY infra/config infra/config
COPY python python
COPY .env .env
COPY Makefile Makefile
COPY poetry.lock poetry.lock
COPY pyproject.toml pyproject.toml
COPY requirements.txt requirements.txt
COPY docker-compose.yaml docker-compose.yaml
