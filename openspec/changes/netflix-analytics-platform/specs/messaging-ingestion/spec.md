# Spec Delta: messaging-ingestion

## Purpose

Lleva los datos del ETL a Cassandra a través de RabbitMQ de forma confiable: mensajes durables, reintentos, una cola de errores para los mensajes inválidos y escrituras idempotentes. Además, informa a la UI de cada mensaje procesado.

## ADDED Requirements

### Requirement: Camino único de escritura
Solo los consumidores de mensajes SHALL escribir en Cassandra. El ETL y la interfaz no escriben directamente en la base de datos.

#### Scenario: Carga de datos
- **WHEN** el ETL publica el catálogo
- **THEN** los datos llegan a Cassandra únicamente a través de los consumidores

### Requirement: Colas durables
El broker SHALL tener un exchange de catálogo y dos colas durables, una para títulos y otra para agregados. Los mensajes pendientes sobreviven a un reinicio del broker o de la aplicación.

#### Scenario: Aplicación detenida
- **WHEN** la aplicación está detenida mientras el ETL publica
- **THEN** los mensajes se acumulan en la cola de títulos y se procesan todos al volver a arrancar la aplicación

#### Scenario: Enrutamiento
- **WHEN** el ETL publica un título y un agregado
- **THEN** cada mensaje llega solo a su cola

### Requirement: Persistencia de títulos
Al recibir un título válido, el sistema SHALL guardarlo en su detalle, en cada uno de sus géneros y en la filmografía de cada persona de su reparto.

#### Scenario: Título nuevo
- **WHEN** se procesa el mensaje del título `tm82169`
- **THEN** el título se puede consultar por id y en el género `drama`, y aparece en la filmografía de Sylvester Stallone

### Requirement: Persistencia de agregados
Al recibir un agregado, el sistema SHALL reemplazar las filas de ese agregado en su tabla.

#### Scenario: KPIs
- **WHEN** se procesa el agregado de KPIs
- **THEN** las tarjetas KPI muestran los valores recibidos

### Requirement: Sin duplicados
Procesar el mismo título más de una vez SHALL dejar una sola copia en cada tabla. El evento que se informa a la UI indica si el título era nuevo o ya existía.

#### Scenario: Mensaje duplicado
- **WHEN** llega dos veces el mensaje del mismo título
- **THEN** existe una sola fila de ese título y el segundo evento se informa como "ya existía"

#### Scenario: Doble carga completa
- **WHEN** el catálogo se carga dos veces
- **THEN** el Data Mart sigue teniendo 6,137 títulos

### Requirement: Reintentos y cola de errores
Si un mensaje falla, el sistema SHALL reintentarlo 3 veces con 1 segundo de espera. Si sigue fallando, lo mueve a la cola de errores de su cola sin bloquear los mensajes siguientes.

#### Scenario: Mensaje inválido
- **WHEN** llega un mensaje de título sin `id`
- **THEN** tras 3 reintentos el mensaje queda en la cola de errores, la UI recibe un evento de error con el motivo y los mensajes siguientes se procesan normalmente

### Requirement: Eventos para la interfaz
Por cada título procesado, el sistema SHALL emitir hacia la interfaz un evento con id, nombre, estado (nuevo, ya existía o error) y tiempo de procesamiento. También emite cada 2 segundos el estado de las colas: mensajes en espera, consumidores y tasas.

#### Scenario: Título procesado
- **WHEN** se guarda un título
- **THEN** la interfaz recibe su evento en menos de 1 segundo
