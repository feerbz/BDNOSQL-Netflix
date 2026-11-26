# Tasks

Responsables: **DM** = Diana Paola Montes · **JC** = Jonathan Cervantes · **FB** = Fernanda Baez · **XJ** = Ximena Jasso. El calendario está en `cronograma.md`.

En la sesión de código, cada grupo es un PR.

## 1. Infraestructura (DM)

- [x] 1.1 Crear `docker-compose.yml` con `cassandra`, `cassandra-init`, `rabbitmq` (con `rabbitmq.conf`: `collect_statistics_interval = 1000`), `app` y `etl` (perfil `tools`). Verificar: `docker compose up -d` deja Cassandra y RabbitMQ healthy.
- [x] 1.2 Escribir `cassandra/schema.cql` con el keyspace y las 9 tablas de design D5, todo con `IF NOT EXISTS`. Verificar: `cassandra-init` termina con código 0, y otra vez con código 0 al reejecutarlo.

## 2. ETL en Python (DM + JC)

- [x] 2.1 (DM) Implementar `extract.py` y `transform.py` (reglas de D12, validación y reparto embebido). Verificar con pytest: 6,137 títulos leídos, géneros como lista, `NR`, seasons nulo en películas, y Rocky con Stallone.
- [x] 2.2 (DM) Implementar `aggregate.py` (kpis, by_year, by_genre, by_country, ranking, top_people). Verificar con pytest: KPIs 6,137 / 3,831 / 2,306 / 56,754, y que los títulos con menos de 1,000 votos quedan fuera del ranking IMDb.
- [x] 2.3 (JC) Implementar `publish.py` y `main.py` con pika: confirms, mensajes persistentes, `--rate`, KPIs al final y resumen en consola. Verificar: `docker compose run --rm etl` termina con código 0 y la UI de RabbitMQ muestra los mensajes en las 2 colas.

## 3. App .NET: mensajería y escritura (JC + FB)

- [x] 3.1 (FB) Crear `src/Netflix.App` (Blazor Web App Interactive Server, .NET 10) y `tests/Netflix.App.Tests`, con `CassandraSession` (sentencias preparadas y UDT `credit`). Verificar: `dotnet build` en verde y la app se conecta a Cassandra al arrancar.
- [x] 3.2 (JC) Configurar MassTransit: 2 colas con JSON plano, bindings y `Interval(3, 1s)`. Verificar: en la UI de RabbitMQ aparecen las colas con 1 consumidor.
- [x] 3.3 (JC) Implementar `TitleConsumer` + `TitleWriter` (validar, comprobar si existe, hacer upsert en las 3 tablas y emitir evento). Verificar con el test harness: título nuevo → evento "nuevo"; repetido → "ya existía" y una sola fila.
- [x] 3.4 (JC) Implementar `AggregateConsumer` y `FaultConsumer`. Verificar: tras una carga completa, `SELECT * FROM kpis` tiene valores y un mensaje sin `id` termina en `catalog-titles_error` con su evento de error.

## 4. App .NET: consultas y vistas (FB + XJ)

- [x] 4.1 (FB) Implementar `DashboardQueries` (un método por gráfica y caché del catálogo en memoria). Verificar con xUnit/integración: cada método devuelve datos y la búsqueda de "rocky" encuentra Rocky.
- [x] 4.2 (XJ) Crear el layout con MudBlazor: tema oscuro, barra lateral con 5 vistas y estados de carga y vacío. Verificar: la navegación funciona y aparece el mensaje de "sin datos" con la base vacía.
- [x] 4.3 (XJ) Vista Resumen (KPIs, área por año, barras de géneros con panel, top países, dona). Verificar: muestra 6,137 títulos y el clic en drama abre sus títulos.
- [x] 4.4 (XJ) Vista Catálogo con búsqueda, filtros y panel de detalle. Verificar: "rocky" + Películas → Rocky, con IMDb 8.1 y su reparto.
- [x] 4.5 (XJ) Vistas Ranking y Personas. Verificar: el top de series IMDb muestra 50 elementos y el clic en un actor muestra su filmografía.

## 5. Vista Mensajería (FB + XJ + JC)

- [x] 5.1 (FB) Implementar `LiveEvents` (singleton con eventos C#) y `QueueStatsPoller` (Management API cada 2 s). Verificar: con la app corriendo, el log muestra las colas y sus mensajes en espera.
- [x] 5.2 (XJ) Vista Mensajería con diagrama y contadores por cola, gráfica msg/s, barra de progreso y feed (100). Verificar: durante `docker compose run --rm etl --rate 60` la barra avanza y el feed se llena.
- [x] 5.3 (JC) Implementar `DemoPublisher` y los botones "Inyectar corrupto" y "Reenviar duplicado". Verificar: el corrupto llega a la cola de errores (contador +1) y el duplicado aparece como "ya existía" sin cambiar el KPI.

## 6. Integración y demo (todo el equipo)

- [x] 6.1 Prueba desde cero: `docker compose down -v && docker compose up -d --build && docker compose run --rm etl`. Verificar: el Resumen muestra 6,137 títulos.
- [x] 6.2 Prueba de durabilidad: `docker compose stop app` a mitad de la carga y luego `start`. Verificar: la cola baja a 0 y siguen siendo 6,137 títulos.
- [x] 6.3 Correr `pytest` y `dotnet test`. Verificar que todo pasa en verde.
- [ ] 6.4 Escribir el guion de demo (≤ 10 min) y ensayarlo. Actualizar el README con capturas.

## Workflow follow-up

- Sincronizar las specs (`/opsx:sync`) y archivar el change (`/opsx:archive`) cuando la entrega esté aprobada.
