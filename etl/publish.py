"""Publicación confirmada en RabbitMQ (design D3)."""
import json
import time

import pika

EXCHANGE = "netflix.catalog"
# cola -> routing key
QUEUES = {"catalog-titles": "title", "catalog-aggregates": "aggregate"}


class PublishError(Exception):
    pass


def connect(host, user="guest", password="guest"):
    return pika.BlockingConnection(
        pika.ConnectionParameters(host=host, credentials=pika.PlainCredentials(user, password))
    )


def open_channel(connection):
    """Declara la topología y activa los publisher confirms.

    Las colas se declaran también aquí (igual que las declara la app) para que no se pierda
    nada si el ETL corre antes de que la app haya arrancado por primera vez.
    """
    channel = connection.channel()
    channel.exchange_declare(EXCHANGE, exchange_type="topic", durable=True)
    for queue, routing_key in QUEUES.items():
        channel.queue_declare(queue, durable=True)
        channel.queue_bind(queue, EXCHANGE, routing_key)
    channel.confirm_delivery()
    return channel


def publish(channel, routing_key, message, label):
    """Publica un mensaje persistente y espera la confirmación del broker."""
    try:
        channel.basic_publish(
            exchange=EXCHANGE,
            routing_key=routing_key,
            body=json.dumps(message, ensure_ascii=False, allow_nan=False).encode(),
            properties=pika.BasicProperties(content_type="application/json", delivery_mode=2),
            mandatory=True,  # un mensaje sin cola de destino también es un error
        )
    except pika.exceptions.AMQPError as error:
        raise PublishError(f"No se confirmó el mensaje {label}: {error!r}") from error


def publish_all(connection, titles, aggregates, rate=None):
    """Publica títulos y después agregados. `rate` limita los títulos por segundo."""
    channel = open_channel(connection)
    start = time.monotonic()
    for index, title in enumerate(titles):
        if rate:
            wait = start + index / rate - time.monotonic()
            if wait > 0:
                connection.sleep(wait)  # sigue atendiendo los heartbeats
        publish(channel, "title", title, title["id"])
    for message in aggregates:
        publish(channel, "aggregate", message, f"agregado {message['kind']}")
