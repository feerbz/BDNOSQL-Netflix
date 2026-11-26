import json

import pytest

from aggregate import aggregate


@pytest.fixture(scope="module")
def messages(titles):
    return aggregate(titles)


@pytest.fixture(scope="module")
def rows(messages):
    return {message["kind"]: message["rows"] for message in messages}


def test_kpis_al_final(messages):
    assert messages[-1]["kind"] == "kpis"


def test_kpis(rows):
    kpis = {row["name"]: row["value"] for row in rows["kpis"]}
    assert (kpis["titles"], kpis["movies"], kpis["shows"], kpis["people"]) == (6137, 3831, 2306, 56754)
    assert 0 < kpis["avg_imdb"] < 10


def test_generos_y_paises(rows):
    assert len(rows["by_genre"]) == 19
    assert len(rows["by_country"]) == 107
    assert all(row["movies"] + row["shows"] == row["total"] for row in rows["by_genre"])


def test_estrenos_por_anio(rows):
    assert sum(row["total"] for row in rows["by_year"]) == 6137


def test_ranking_exige_1000_votos(rows, titles):
    votes = {title["id"]: title["imdb_votes"] for title in titles}
    imdb = [row for row in rows["ranking"] if row["list_name"].startswith("imdb")]
    assert imdb and all(votes[row["id"]] >= 1000 for row in imdb)


def test_ranking_seis_listas_de_50(rows):
    lists = {}
    for row in rows["ranking"]:
        lists.setdefault(row["list_name"], []).append(row["position"])
    assert len(lists) == 6
    assert all(positions == list(range(1, 51)) for positions in lists.values())


def test_top_personas(rows):
    for role in ("ACTOR", "DIRECTOR"):
        top = [row for row in rows["top_people"] if row["role"] == role]
        assert [row["position"] for row in top] == list(range(1, 51))
        assert [row["total"] for row in top] == sorted((row["total"] for row in top), reverse=True)


def test_json_valido(messages, titles):
    json.dumps(messages, allow_nan=False)
    json.dumps(titles, allow_nan=False)
