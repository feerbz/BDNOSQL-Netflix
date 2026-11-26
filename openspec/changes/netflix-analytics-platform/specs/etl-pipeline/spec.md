# Spec Delta: etl-pipeline

## Purpose

Convierte los CSV crudos del catálogo de Netflix en títulos limpios con su reparto embebido y en agregados analíticos, y los publica como mensajes confirmados hacia RabbitMQ.

## ADDED Requirements

### Requirement: Lectura de los datasets
El ETL SHALL leer `titles.csv` y `credits.csv` del directorio configurado, respetando campos multilínea y comillas, sin perder registros.

#### Scenario: Lectura completa
- **WHEN** el ETL lee los archivos de `archive/`
- **THEN** obtiene 6,137 títulos y 81,355 créditos

#### Scenario: Archivo faltante
- **WHEN** falta alguno de los dos archivos
- **THEN** el ETL termina con error indicando qué archivo falta y no publica nada

### Requirement: Limpieza de los datos
El ETL SHALL aplicar estas reglas de limpieza:
- convertir `genres` y `production_countries` de texto a listas;
- convertir puntuaciones vacías o fuera de 0–10 en nulo;
- convertir `runtime = 0` en nulo;
- reemplazar una `age_certification` vacía por `NR`;
- dejar `seasons` nulo en las películas.

#### Scenario: Géneros como texto
- **WHEN** un título trae `genres = "['drama', 'sport']"`
- **THEN** el título limpio tiene la lista `["drama", "sport"]`

#### Scenario: Clasificación vacía
- **WHEN** un título no tiene `age_certification`
- **THEN** el título limpio tiene `age_certification = "NR"`

### Requirement: Omisión de registros inválidos
El ETL SHALL omitir los títulos que no cumplan alguna de estas condiciones:
- id `tm…` o `ts…`;
- tipo `MOVIE` o `SHOW`;
- año entre 1900 y 2030.

Al terminar, informa en consola cuántos títulos omitió.

#### Scenario: Tipo inválido
- **WHEN** un título tiene `type = "EPISODE"`
- **THEN** no se publica y el resumen de consola lo cuenta como omitido

### Requirement: Reparto embebido en cada título
El ETL SHALL adjuntar a cada título la lista de sus créditos (actores y directores), de modo que cada título se publique como un documento completo.

#### Scenario: Título con reparto
- **WHEN** se procesa `tm82169`
- **THEN** su documento incluye a Sylvester Stallone como `ACTOR`

#### Scenario: Título sin créditos
- **WHEN** un título no tiene créditos
- **THEN** se publica con una lista de créditos vacía

### Requirement: Cálculo de agregados
El ETL SHALL calcular los agregados del Data Mart:
- KPIs;
- títulos por año y tipo;
- por género;
- por país;
- rankings top 50 por IMDb y por popularidad, para todos los títulos, películas y series;
- top 50 personas por rol.

#### Scenario: KPIs del dataset original
- **WHEN** se calculan los agregados del dataset original
- **THEN** los KPIs reportan 6,137 títulos, 3,831 películas, 2,306 series y 56,754 personas

#### Scenario: Votos insuficientes para ranking
- **WHEN** un título tiene menos de 1,000 votos IMDb
- **THEN** no aparece en los rankings por IMDb

### Requirement: Publicación confirmada
El ETL SHALL publicar cada título y después cada agregado como mensajes persistentes, esperando la confirmación del broker para cada uno. El agregado de KPIs se publica al final.

#### Scenario: Corrida exitosa
- **WHEN** el ETL corre con RabbitMQ disponible
- **THEN** publica un mensaje por título válido y uno por agregado, todos confirmados, y termina con código 0

#### Scenario: Mensaje no confirmado
- **WHEN** el broker no confirma un mensaje
- **THEN** el ETL termina con código distinto de cero indicando el mensaje que falló

### Requirement: Velocidad de publicación configurable
El ETL SHALL aceptar una tasa máxima de mensajes por segundo para que la ingesta se pueda observar en la demo. Sin tasa, publica a máxima velocidad.

#### Scenario: Tasa limitada
- **WHEN** se ejecuta con `--rate 60`
- **THEN** publica como máximo unos 60 títulos por segundo
