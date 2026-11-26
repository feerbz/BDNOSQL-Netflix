# Spec Delta: dashboard-ui

## Purpose

Muestra las estadísticas del catálogo de Netflix en un dashboard web oscuro estilo Netflix con cinco vistas. Una de ellas, Mensajería, hace visible en tiempo real el uso de RabbitMQ.

## ADDED Requirements

### Requirement: Navegación
El dashboard SHALL tener una barra lateral con la marca "Netflix Analytics" y enlaces a Resumen, Catálogo, Ranking, Personas y Mensajería, resaltando la vista activa.

#### Scenario: Cambio de vista
- **WHEN** el usuario pulsa "Ranking"
- **THEN** se muestra la vista Ranking sin recargar la página

### Requirement: Tema oscuro
El dashboard SHALL usar fondo oscuro y acento rojo. En todas las gráficas, las películas van en rojo y las series en amarillo.

#### Scenario: Colores por tipo
- **WHEN** una gráfica separa películas y series
- **THEN** las películas aparecen en rojo y las series en amarillo

### Requirement: Vista Resumen
La vista Resumen SHALL mostrar:
- tarjetas KPI: títulos, películas, series, IMDb promedio y personas;
- un área de estrenos por año y tipo;
- barras de los géneros principales;
- barras del top 10 de países;
- una dona de películas vs. series.

#### Scenario: Resumen con datos
- **WHEN** el usuario abre Resumen con el catálogo cargado
- **THEN** la tarjeta de títulos muestra 6,137 y las gráficas tienen datos

#### Scenario: Clic en un género
- **WHEN** el usuario pulsa la barra de `drama`
- **THEN** se abre un panel con los mejores títulos de drama

### Requirement: Vista Catálogo
La vista Catálogo SHALL mostrar una tabla paginada de títulos con búsqueda por nombre y filtros por tipo y género.

#### Scenario: Búsqueda
- **WHEN** el usuario busca "rocky" con el filtro "Películas"
- **THEN** la tabla muestra solo películas cuyo nombre contiene "rocky"

### Requirement: Detalle de título
Al seleccionar un título, el dashboard SHALL abrir un panel con la sinopsis, el año, el tipo, la duración o las temporadas, las puntuaciones, los géneros y el reparto con sus personajes.

#### Scenario: Detalle de Rocky
- **WHEN** el usuario selecciona "Rocky"
- **THEN** el panel muestra IMDb 8.1 y el reparto con Sylvester Stallone

### Requirement: Vista Ranking
La vista Ranking SHALL mostrar el top 50 por IMDb o por popularidad, con filtro de todos, películas o series.

#### Scenario: Top series IMDb
- **WHEN** el usuario elige IMDb y "Series"
- **THEN** se muestran hasta 50 series numeradas

### Requirement: Vista Personas
La vista Personas SHALL mostrar el top 50 de actores y de directores por número de títulos. Al seleccionar una persona se muestra su filmografía.

#### Scenario: Filmografía
- **WHEN** el usuario selecciona un actor
- **THEN** se muestran sus títulos del más reciente al más antiguo

### Requirement: Vista Mensajería en tiempo real
La vista Mensajería SHALL mostrar, actualizados sin recargar la página:
- un diagrama ETL → exchange → colas → app → Cassandra;
- los mensajes en espera y los consumidores de cada cola, incluida la cola de errores;
- una gráfica de mensajes por segundo;
- una barra de progreso de la carga;
- un feed con los últimos 100 eventos (nuevo, ya existía, error).

#### Scenario: Carga en curso
- **WHEN** el ETL está publicando el catálogo
- **THEN** la barra de progreso avanza, el feed se llena y la gráfica muestra la tasa de mensajes

#### Scenario: Aplicación detenida y reanudada
- **WHEN** la aplicación se detiene a mitad de una carga y se vuelve a arrancar
- **THEN** al volver, la vista muestra la cola de títulos con mensajes pendientes que bajan hasta 0

### Requirement: Botones de demostración
La vista Mensajería SHALL tener dos botones:
- "Inyectar corrupto": envía un título inválido que termina en la cola de errores;
- "Reenviar duplicado": reenvía el último título y demuestra que no se duplica.

#### Scenario: Inyectar corrupto
- **WHEN** el usuario pulsa "Inyectar corrupto"
- **THEN** en pocos segundos aparece un error en el feed y el contador de la cola de errores sube en 1

#### Scenario: Reenviar duplicado
- **WHEN** el usuario pulsa "Reenviar duplicado"
- **THEN** el feed muestra "ya existía" y el KPI de títulos no cambia

### Requirement: Estados de carga y vacío
Cada vista SHALL mostrar un indicador mientras carga. Si aún no hay datos, muestra un mensaje que indica cómo cargar el catálogo.

#### Scenario: Sin datos
- **WHEN** el usuario abre Resumen antes de cargar el catálogo
- **THEN** se muestra "Aún no hay datos: ejecuta `docker compose run --rm etl`"
