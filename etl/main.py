"""ETL del catálogo de Netflix: CSV -> limpieza -> agregados -> RabbitMQ."""
import argparse
import os
import sys
import time
from pathlib import Path

import pika

from aggregate import aggregate
from extract import extract
from publish import PublishError, connect, publish_all
from transform import transform


def positive(value):
    number = float(value)
    if number <= 0:
        raise argparse.ArgumentTypeError("debe ser mayor que 0")
    return number


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--data-dir", default=os.getenv("DATA_DIR", Path(__file__).parent.parent / "archive"))
    parser.add_argument("--rate", type=positive, help="máximo de títulos por segundo")
    args = parser.parse_args(argv)

    start = time.monotonic()
    try:
        raw_titles, raw_credits = extract(args.data_dir)
        titles, skipped = transform(raw_titles, raw_credits)
        messages = aggregate(titles)
        print(f"Leídos {len(raw_titles)} títulos y {len(raw_credits)} créditos. Publicando...", flush=True)
        connection = connect(os.getenv("RABBITMQ_HOST", "localhost"))
        try:
            publish_all(connection, titles, messages, args.rate)
        finally:
            if connection.is_open:
                connection.close()
    except (FileNotFoundError, PublishError, pika.exceptions.AMQPError) as error:
        print(f"ERROR: {error!r}", file=sys.stderr)
        return 1

    print(
        f"Resumen: {len(titles)} títulos y {len(messages)} agregados confirmados, "
        f"{skipped} títulos omitidos, {time.monotonic() - start:.1f} s."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
