# Propuesta: Plataforma Netflix Analytics

## Why

El catálogo público de Netflix (Kaggle) llega como dos CSV planos: `titles.csv` con 6,137 títulos y `credits.csv` con 81,355 créditos. Contestar preguntas analíticas sobre ellos exige cruzarlos y agregarlos, lo que en un esquema relacional implica JOINs y GROUP BY costosos.

El curso de Bases de Datos NoSQL pide demostrar tres cosas:
- un modelo **orientado a consultas** en Apache Cassandra, alimentado por un ETL en Python;
- un backend .NET;
- una ingesta **transaccional** por mensajería.

La propuesta original (README) era vaga: mencionaba un "Data Mart" relacional y Power BI. Hay que redefinir el alcance **de la forma más simple que cumpla los requisitos**.

## What Changes

- **ETL en Python:** lee ambos CSV, los limpia (colecciones serializadas como texto, nulos, valores inválidos), embebe el reparto en cada título, precalcula agregados y publica todo en RabbitMQ con confirmación de entrega.
- **Mensajería con RabbitMQ + MassTransit:**
  - un exchange y dos colas durables (títulos y agregados);
  - reintentos simples y una cola de errores (DLQ) automática;
  - escrituras idempotentes por clave: un mensaje repetido no duplica datos.
- **Data Mart NoSQL orientado a consultas en Cassandra.** Reemplaza al "Data Mart" relacional: 9 tablas, cada una diseñada para una pantalla, sin JOINs.
- **Una sola aplicación .NET 10:**
  - aloja los consumidores, que son el único punto que escribe en Cassandra;
  - aloja los servicios de consulta, que solo leen;
  - aloja el dashboard Blazor (Interactive Server + MudBlazor + ApexCharts).
- **Dashboard oscuro estilo Netflix con 5 vistas:** Resumen, Catálogo, Ranking, Personas y **Mensajería**. Esta última muestra en vivo las colas, la tasa de mensajes y un feed de eventos, con dos botones de demo: mensaje corrupto → DLQ, y mensaje duplicado → sin duplicar.
- **Docker Compose:** levanta Cassandra, RabbitMQ y la app. El ETL se ejecuta bajo demanda.
- **Se elimina del alcance:** Power BI y el modelado dimensional en estrella.

## Capabilities

### New Capabilities

- `etl-pipeline`: extracción, limpieza, transformación y cálculo de agregados del catálogo en Python, y publicación confirmada como mensajes.
- `messaging-ingestion`: colas RabbitMQ, contratos de mensajes y consumidores .NET que persisten en Cassandra de forma idempotente, con reintentos y DLQ.
- `cassandra-data-mart`: keyspace y tablas orientadas a consultas y de agregados en Cassandra.
- `dashboard-ui`: vistas analíticas y vista de Mensajería en Blazor, con actualización en tiempo real.

### Modified Capabilities

_Ninguna._ No existen especificaciones previas en `openspec/specs/`.

## Impact

- **Repositorio:** se crean `docker-compose.yml`, `cassandra/schema.cql`, `etl/` (Python), `src/Netflix.App/` (.NET) y `tests/`. El README se corrige para reflejar este alcance.
- **Dependencias:**
  - Python: `pandas`, `pika`, `pytest`.
  - .NET: `MassTransit.RabbitMQ` 8.x, `CassandraCSharpDriver`, `MudBlazor`, `Blazor-ApexCharts` y `xUnit`.
- **Infraestructura:** imágenes `cassandra:5.0` y `rabbitmq:4-management`. Puertos 9042, 5672, 15672 y 8080.
- **Fuera de alcance:** API REST pública, autenticación, nube, clúster multinodo, mapa geográfico, edición de datos desde la UI y Power BI.
