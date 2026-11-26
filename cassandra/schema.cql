-- Data Mart NoSQL orientado a consultas (design D5): una tabla por pantalla.
-- Todo con IF NOT EXISTS: se puede reejecutar sin error y sin borrar datos.

CREATE KEYSPACE IF NOT EXISTS netflix_mart
  WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1};

USE netflix_mart;

CREATE TYPE IF NOT EXISTS credit (
  person_id int,
  name      text,
  character text,
  role      text
);

-- Detalle de título y carga del catálogo
CREATE TABLE IF NOT EXISTS titles_by_id (
  id                   text PRIMARY KEY,
  title                text,
  type                 text,
  description          text,
  release_year         int,
  age_certification    text,
  runtime              int,
  seasons              int,
  genres               list<text>,
  production_countries list<text>,
  imdb_score           double,
  imdb_votes           int,
  tmdb_popularity      double,
  tmdb_score           double,
  credits              list<frozen<credit>>
);

-- Resumen: clic en un género. Sin puntuación IMDb se guarda -1 (queda al final).
CREATE TABLE IF NOT EXISTS titles_by_genre (
  genre        text,
  imdb_score   double,
  id           text,
  title        text,
  type         text,
  release_year int,
  PRIMARY KEY ((genre), imdb_score, id)
) WITH CLUSTERING ORDER BY (imdb_score DESC, id ASC);

-- Personas: filmografía
CREATE TABLE IF NOT EXISTS credits_by_person (
  person_id    int,
  release_year int,
  title_id     text,
  title        text,
  type         text,
  role         text,
  character    text,
  imdb_score   double,
  PRIMARY KEY ((person_id), release_year, title_id, role)
) WITH CLUSTERING ORDER BY (release_year DESC, title_id ASC, role ASC);

-- Tarjetas KPI
CREATE TABLE IF NOT EXISTS kpis (
  name  text PRIMARY KEY,
  value double
);

-- Estrenos por año
CREATE TABLE IF NOT EXISTS agg_by_year (
  type         text,
  release_year int,
  total        int,
  PRIMARY KEY ((type), release_year)
);

-- Barras de géneros
CREATE TABLE IF NOT EXISTS agg_by_genre (
  scope    text,
  genre    text,
  total    int,
  movies   int,
  shows    int,
  avg_imdb double,
  PRIMARY KEY ((scope), genre)
);

-- Barras de top países
CREATE TABLE IF NOT EXISTS agg_by_country (
  scope   text,
  country text,
  total   int,
  movies  int,
  shows   int,
  PRIMARY KEY ((scope), country)
);

-- Ranking top 50. list_name: {imdb|popularity}_{all|movie|show}
CREATE TABLE IF NOT EXISTS ranking (
  list_name       text,
  position        int,
  id              text,
  title           text,
  type            text,
  release_year    int,
  imdb_score      double,
  imdb_votes      int,
  tmdb_popularity double,
  PRIMARY KEY ((list_name), position)
);

-- Personas: top 50 por rol
CREATE TABLE IF NOT EXISTS top_people (
  role      text,
  position  int,
  person_id int,
  name      text,
  total     int,
  PRIMARY KEY ((role), position)
);
