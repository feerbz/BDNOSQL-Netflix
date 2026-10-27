# Propuesta Técnica: Data Mart NoSQL de el catálogo de Netflix 
1. Definición del problema
El procesamiento y análisis de volúmenes significativos de datos provenientes de
fuentes heterogéneas (como catálogos y créditos de producciones) a menudo
enfrenta limitaciones de rendimiento y flexibilidad en esquemas relacionales
tradicionales. Se requiere implementar una solución de almacenamiento NoSQL
que permita optimizar los tiempos de respuesta de lectura y estructurar un Data
Mart especializado para consultas analíticas eficientes.
2. Objetivo general
Diseñar e implementar una arquitectura de datos basada en NoSQL utilizando
Apache Cassandra, soportada por un pipeline ETL en Python, para procesar,
modelar y explotar información analítica proveniente de datasets públicos de
Kaggle.
3. Objetivos específicos
• Extraer, limpiar, transformar y preparar los datos originales de los archivos
CSV mediante un script automatizado en Python.
• Modelar el almacenamiento NoSQL en Apache Cassandra adaptado
estrictamente a los patrones de consulta requeridos.
• Construir un Data Mart estructurado que consolide la información
procesada para facilitar su acceso.
• Evaluar y ejecutar consultas analíticas sobre el repositorio NoSQL,
integrando de forma opcional herramientas de visualización como Power
BI.
4. Especificación y Análisis de la Fuente de Datos
El pipeline de ingesta procesa información estructurada proveniente de la
plataforma Kaggle, compuesta por dos datasets relacionales que representan los
metadatos y el elenco de producciones audiovisuales.
4.1 Dataset Principal: titles.csv
Este archivo almacena la información descriptiva y métricas de rendimiento de
cada título.
• Volumen: 6,137 registros.
• Estructura: 15 atributos.
• Atributos Clave:
-Identificación y Clasificación: id, title, type, release_year, age_certification.
-Métricas y Dimensiones: runtime, genres, production_countries, seasons.
-Evaluaciones de Calidad: imdb_score, imdb_votes, tmdb_popularity,
tmdb_score.
4.2 Dataset Relacional: credits.csv
Este archivo almacena el detalle del reparto y equipo de producción asociado a
cada contenido.
• Volumen: 81,355 registros.
• Estructura: 5 atributos.
• Atributos: person_id, id, name, character, role.
4.3. Modelo de Asociación en Origen
Las tablas de origen se relacionan mediante un esquema de uno a muchos (1:N) a
través de la clave primaria y foránea id, donde un registro de título (titles.csv) está
asociado a múltiples registros de créditos (credits.csv).
### 5. Stack tecnológico

| Componente | Rol en la Arquitectura | Descripción del Proceso |
|---|---|---|
| Python | Capa de Procesamiento (ETL) | Extracción de los archivos `titles.csv` y `credits.csv`, limpieza, transformación y preparación de los datos originales. |
| Apache Cassandra | Capa de Almacenamiento NoSQL | Modelado de datos orientado a consultas y almacenamiento estructurado del Data Mart. |
| Power BI (Opcional) | Capa de Presentación | Generación de paneles de visualización y consulta avanzada sobre el Data Mart. |
6. Arquitectura del Flujo de Datos
Fase del Flujo Componente /
Herramienta
Descripción Técnica de la Etapa
1. Origen de
Datos
Archivos CSV
(Kaggle)
Ingesta de los archivos fuente (titles.csv y
credits.csv) con los datos originales.
2. Pipeline ETL Python Ejecución de scripts para la extracción, limpieza,
transformación y estructuración de los datos.
3. Modelado
NoSQL
Apache
Cassandra
Diseño del esquema desnormalizado orientado a
consultas para optimizar el rendimiento del
almacenamiento.
4. Capa
Analítica
Data Mart Consolidación de los datos organizados
específicamente para responder a las consultas
del negocio.
5. Expresión /
BI (Opcional)
Power BI Conexión opcional al Data Mart para la
visualización gráfica de los resultados y tableros
de control
### 6. Arquitectura del Flujo de Datos

| Fase del Flujo | Componente / Herramienta | Descripción Técnica de la Etapa |
|---|---|---|
| 1. Origen de Datos | Archivos CSV (Kaggle) | Ingesta de los archivos fuente (`titles.csv` y `credits.csv`) con los datos originales. |
| 2. Pipeline ETL | Python | Ejecución de scripts para la extracción, limpieza, transformación y estructuración de los datos. |
| 3. Modelado NoSQL | Apache Cassandra | Diseño del esquema desnormalizado orientado a consultas para optimizar el rendimiento del almacenamiento. |
| 4. Capa Analítica | Data Mart | Consolidación de los datos organizados específicamente para responder a las consultas del negocio. |
| 5. Explotación / BI (Opcional) | Power BI | Conexión opcional al Data Mart para la visualización gráfica de los resultados y la creación de tableros de control. |
### 7. Arquitectura propuesta
