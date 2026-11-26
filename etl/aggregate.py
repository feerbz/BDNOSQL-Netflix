"""Agregados del Data Mart, calculados con pandas (design D7)."""
import pandas as pd

TOP = 50
MIN_VOTES = 1000


def _records(frame):
    """DataFrame -> lista de dicts con tipos nativos y None en lugar de NaN (JSON válido)."""
    return frame.astype(object).where(frame.notna(), None).to_dict("records")


def _frame(titles):
    frame = pd.DataFrame(titles)
    for column in ("imdb_score", "imdb_votes", "tmdb_popularity"):
        frame[column] = pd.to_numeric(frame[column])
    frame["imdb_votes"] = frame["imdb_votes"].astype("Int64")
    return frame


def _totals(frame, column):
    """Totales (y películas/series) por cada valor de una columna de listas."""
    exploded = frame.explode(column).dropna(subset=[column])
    return exploded.assign(
        movies=exploded["type"].eq("MOVIE"), shows=exploded["type"].eq("SHOW")
    ).groupby(column).agg(
        total=("id", "size"), movies=("movies", "sum"), shows=("shows", "sum"),
        avg_imdb=("imdb_score", "mean"),
    ).sort_values("total", ascending=False).reset_index()


def kpis(frame, credits):
    values = {
        "titles": len(frame),
        "movies": int(frame["type"].eq("MOVIE").sum()),
        "shows": int(frame["type"].eq("SHOW").sum()),
        "avg_imdb": round(float(frame["imdb_score"].mean()), 2),
        "people": int(credits["person_id"].nunique()),
    }
    return [{"name": name, "value": value} for name, value in values.items()]


def by_year(frame):
    return _records(frame.groupby(["type", "release_year"]).size().rename("total").reset_index())


def by_genre(frame):
    totals = _totals(frame, "genres").rename(columns={"genres": "genre"})
    return _records(totals.assign(avg_imdb=totals["avg_imdb"].round(2)))


def by_country(frame):
    totals = _totals(frame, "production_countries").rename(columns={"production_countries": "country"})
    return _records(totals.drop(columns="avg_imdb"))


def ranking(frame):
    """Top 50 por IMDb (con votos suficientes) y por popularidad: todos, películas y series."""
    ordered = {
        "imdb": frame[frame["imdb_votes"].ge(MIN_VOTES).fillna(False) & frame["imdb_score"].notna()]
        .sort_values(["imdb_score", "imdb_votes", "id"], ascending=[False, False, True]),
        "popularity": frame[frame["tmdb_popularity"].notna()]
        .sort_values(["tmdb_popularity", "id"], ascending=[False, True]),
    }
    columns = ["id", "title", "type", "release_year", "imdb_score", "imdb_votes", "tmdb_popularity"]
    rows = []
    for metric, data in ordered.items():
        for suffix, kind in (("all", None), ("movie", "MOVIE"), ("show", "SHOW")):
            top = data if kind is None else data[data["type"] == kind]
            top = top.head(TOP)[columns]
            top = top.assign(list_name=f"{metric}_{suffix}", position=range(1, len(top) + 1))
            rows += _records(top)
    return rows


def top_people(credits):
    """Top 50 personas por rol, según el número de títulos distintos."""
    counts = credits.groupby(["role", "person_id"]).agg(
        name=("name", "first"), total=("title_id", "nunique")
    ).reset_index().sort_values(["role", "total", "name", "person_id"], ascending=[True, False, True, True])
    top = counts.groupby("role").head(TOP)
    return _records(top.assign(position=top.groupby("role").cumcount() + 1))


def aggregate(titles):
    """Mensajes de agregados. "kpis" va AL FINAL: su llegada refresca la caché del catálogo."""
    frame = _frame(titles)
    credits = pd.DataFrame(
        [{**credit, "title_id": title["id"]} for title in titles for credit in title["credits"]],
        columns=["person_id", "name", "character", "role", "title_id"],
    )
    return [
        {"kind": "by_year", "rows": by_year(frame)},
        {"kind": "by_genre", "rows": by_genre(frame)},
        {"kind": "by_country", "rows": by_country(frame)},
        {"kind": "ranking", "rows": ranking(frame)},
        {"kind": "top_people", "rows": top_people(credits)},
        {"kind": "kpis", "rows": kpis(frame, credits)},
    ]
