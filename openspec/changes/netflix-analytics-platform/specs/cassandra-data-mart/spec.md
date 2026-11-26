# Spec Delta: cassandra-data-mart

## Purpose

Define el almacenamiento en Apache Cassandra como un Data Mart orientado a consultas: una tabla por pantalla del dashboard y agregados precalculados, para que cada consulta lea una sola partición sin JOINs.

## ADDED Requirements

### Requirement: Esquema reproducible
El sistema SHALL crear el keyspace `netflix_mart` y sus tablas con un script CQL que se puede ejecutar varias veces sin error y sin borrar datos.

#### Scenario: Primera ejecución
- **WHEN** el script se ejecuta contra un Cassandra vacío
- **THEN** existen el keyspace y las 9 tablas

#### Scenario: Reejecución
- **WHEN** el script se ejecuta de nuevo con datos cargados
- **THEN** termina sin error y los datos siguen intactos

### Requirement: Detalle de título en una lectura
El detalle de un título SHALL obtenerse con una sola lectura que incluya sus atributos y su reparto completo.

#### Scenario: Detalle
- **WHEN** se lee el título `tm82169`
- **THEN** una lectura devuelve sus métricas, géneros, países y créditos

### Requirement: Títulos por género ordenados
Los títulos de un género SHALL leerse de una sola partición, ordenados por puntuación IMDb descendente. Los títulos sin puntuación quedan al final.

#### Scenario: Mejores dramas
- **WHEN** se consultan los títulos del género `drama`
- **THEN** llegan ordenados por puntuación descendente, con los títulos sin puntuación al final

### Requirement: Filmografía por persona
La filmografía de una persona SHALL leerse de una sola partición, ordenada por año de estreno descendente.

#### Scenario: Filmografía
- **WHEN** se consulta la persona `3180`
- **THEN** se obtienen sus títulos del más reciente al más antiguo

### Requirement: Agregados listos para graficar
El Data Mart SHALL guardar en tablas propias:
- los KPIs;
- los títulos por año y tipo;
- los totales por género y por país;
- los rankings top 50;
- el top 50 de personas por rol.

Cada gráfica se dibuja con la lectura de una sola partición.

#### Scenario: Ranking de series
- **WHEN** se consulta el ranking IMDb de series
- **THEN** se obtienen hasta 50 títulos ya ordenados por posición

#### Scenario: Estrenos por año
- **WHEN** se consultan los estrenos de películas
- **THEN** se obtiene una fila por año con su total
