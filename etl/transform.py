"""Reglas de limpieza (design D12) y reparto embebido en cada título."""
import ast
import re

ID_PATTERN = re.compile(r"^t[ms]\d+$")
TYPES = {"MOVIE", "SHOW"}


def _text(value):
    return str(value).strip()


def _float(value):
    try:
        return float(value)
    except (TypeError, ValueError):
        return None


def _int(value):
    number = _float(value)
    return None if number is None or number != number else int(number)


def _score(value):
    number = _float(value)
    return number if number is not None and 0 <= number <= 10 else None


def _list(value, normalize):
    """"['drama', 'sport']" -> ["drama", "sport"], sin duplicados."""
    items = ast.literal_eval(_text(value) or "[]")
    return list(dict.fromkeys(normalize(item.strip()) for item in items if item.strip()))


def clean_title(row):
    """Devuelve el título limpio, o None si la fila es inválida."""
    title_id, kind, year = _text(row["id"]), _text(row["type"]), _int(row["release_year"])
    if not ID_PATTERN.match(title_id) or kind not in TYPES or year is None or not 1900 <= year <= 2030:
        return None
    return {
        "id": title_id,
        "title": _text(row["title"]),
        "type": kind,
        "description": _text(row["description"]),
        "release_year": year,
        "age_certification": _text(row["age_certification"]) or "NR",
        "runtime": _int(row["runtime"]) or None,
        "seasons": _int(row["seasons"]) if kind == "SHOW" else None,
        "genres": _list(row["genres"], str.lower),
        "production_countries": _list(row["production_countries"], str.upper),
        "imdb_score": _score(row["imdb_score"]),
        "imdb_votes": _int(row["imdb_votes"]),
        "tmdb_popularity": _float(row["tmdb_popularity"]),
        "tmdb_score": _score(row["tmdb_score"]),
    }


def group_credits(credits):
    """{id de título: [créditos]}"""
    grouped = {}
    for row in credits.to_dict("records"):
        grouped.setdefault(_text(row["id"]), []).append({
            "person_id": int(row["person_id"]),
            "name": _text(row["name"]),
            "character": _text(row["character"]) or None,
            "role": _text(row["role"]),
        })
    return grouped


def transform(titles, credits):
    """Devuelve (títulos limpios con reparto, número de filas omitidas)."""
    cast = group_credits(credits)
    cleaned = [clean_title(row) for row in titles.to_dict("records")]
    valid = [title for title in cleaned if title is not None]
    for position, title in enumerate(valid, start=1):
        title["credits"] = cast.get(title["id"], [])
        title["position"] = position  # position / total_titles = barra de progreso
        title["total_titles"] = len(valid)
    return valid, len(cleaned) - len(valid)
