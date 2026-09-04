# Propuesta Técnica: Data Mart NoSQL de Impartición de Justicia Criminal

## 1. Nombre del Data Mart
**Data Mart NoSQL de Estadística Judicial y Caracterización de Delitos Penales (EHRIIJ 2022)**

---

## 2. Problema Actual
El Instituto Nacional de Estadística y Geografía (INEGI) publica los datos del *Esquema Homologado de Recolección de Información de Impartición de Justicia 2022 (EHRIIJ 2022)* en archivos CSV desagregados: una tabla transaccional masiva con **404,265 registros de delitos** y **12 archivos independientes de catálogos** (`delirie8`, `ubicgeoc`, `forcomis`, `ele_comi`, `consumac`, etc.).

Esta estructura genera las siguientes problemáticas para el análisis de datos:
1. **Alto Costo Computacional y Complejidad**: Para responder preguntas analíticas básicas (ej. *"¿Cuántos delitos dolosos cometidos con arma de fuego ocurrieron en el municipio X?"*), las herramientas tradicionales deben realizar múltiples consultas cruzadas (`JOINs`) combinando la tabla principal con hasta 12 catálogos diferentes.
2. **Ineficiencia en Herramientas de Business Intelligence (BI)**: Importar múltiples archivos CSV tabulares sin un modelo analítico optimizado satura la memoria de herramientas como Power BI y dificulta la creación de modelos flexibles.
3. **Rigidez de Esquema**: La estructura relacional dificulta la inclusión de variables heterogéneas o evoluciones futuras en las encuestas sin alterar tablas y esquemas preexistentes.

---

## 3. Objetivos

### Objetivo General
Diseñar e implementar un **Data Mart no relacional (NoSQL) orientado a documentos en MongoDB**, apoyado en scripts de procesamiento **ETL con Python** y visualización analítica en **Power BI**, para centralizar, desnormalizar y analizar de forma eficiente la información estadística de los delitos registrados en las causas penales de los Juzgados de Control en México durante el año 2022.

### Objetivos Específicos
1. **Análisis Geográfico de Incidencia Delictiva**: Determinar la concentración territorial y municipal de los delitos de mayor impacto (ej. violencia de género, homicidio, delitos patrimoniales) para identificar zonas de alta prioridad que fundamenten la asignación estratégica de jueces, personal de control y recursos públicos.
2. **Análisis de Modus Operandi y Nivel de Violencia**: Evaluar la relación entre los elementos de comisión utilizados (armas de fuego, armas blancas, fuerza física) y la forma de comisión (dolosa vs. culposa), permitiendo caracterizar la severidad y violencia de los patrones delictivos para la toma de decisiones en políticas de prevención y seguridad pública.
3. **Análisis de Tendencias y Temporalidad de la Ocurrencia**: Identificar la estacionalidad, los días de la semana y meses de mayor ocurrencia de los delitos registrados, facilitando la predicción de picos en la carga de trabajo de los Juzgados de Control y la planificación operativa del sistema de justicia.

---

## 4. Diagrama de Arquitectura de Solución


