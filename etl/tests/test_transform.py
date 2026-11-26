import pytest

from extract import extract
from transform import clean_title, transform

ROW = {
    "id": "tm1", "title": " Prueba ", "type": "MOVIE", "description": "", "release_year": "2000",
    "age_certification": "", "runtime": "0", "genres": "['Drama', 'drama', 'sport']",
    "production_countries": "['us']", "seasons": "2.0", "imdb_score": "11", "imdb_votes": "1500.0",
    "tmdb_popularity": "1.5", "tmdb_score": "",
}


def test_lectura_completa(raw):
    titles, credits = raw
    assert len(titles) == 6137
    assert len(credits) == 81355


def test_archivo_faltante(tmp_path):
    (tmp_path / "titles.csv").write_text("id\n")
    with pytest.raises(FileNotFoundError, match="credits.csv"):
        extract(tmp_path)


def test_reglas_de_limpieza():
    title = clean_title(ROW)
    assert title["title"] == "Prueba"
    assert title["genres"] == ["drama", "sport"]
    assert title["production_countries"] == ["US"]
    assert title["age_certification"] == "NR"
    assert title["runtime"] is None
    assert title["seasons"] is None  # es una película
    assert title["imdb_score"] is None  # fuera de 0-10
    assert title["tmdb_score"] is None
    assert title["imdb_votes"] == 1500
    assert title["description"] == ""


@pytest.mark.parametrize("change", [
    {"type": "EPISODE"}, {"id": "xx1"}, {"release_year": "1800"}, {"release_year": ""},
])
def test_fila_invalida(change):
    assert clean_title({**ROW, **change}) is None


def test_omitidos_se_cuentan(raw):
    titles, credits = raw
    sample = titles.head(3).copy()
    sample.loc[sample.index[0], "type"] = "EPISODE"
    valid, skipped = transform(sample, credits)
    assert (len(valid), skipped) == (2, 1)
    assert all(title["total_titles"] == 2 for title in valid)
    assert [title["position"] for title in valid] == [1, 2]


def test_catalogo_completo(titles):
    assert len(titles) == 6137
    assert all(isinstance(title["genres"], list) for title in titles)
    assert all(title["seasons"] is None for title in titles if title["type"] == "MOVIE")
    assert sum(title["age_certification"] == "NR" for title in titles) == 2743
    assert any(title["credits"] == [] for title in titles)


def test_rocky(titles):
    rocky = next(title for title in titles if title["id"] == "tm82169")
    assert rocky["title"] == "Rocky"
    assert rocky["genres"] == ["drama", "sport"]
    assert rocky["imdb_score"] == 8.1
    assert {"person_id": 3180, "name": "Sylvester Stallone",
            "character": 'Robert "Rocky" Balboa', "role": "ACTOR"} in rocky["credits"]
