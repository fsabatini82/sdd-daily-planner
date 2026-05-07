# SPEC-01 — Catalog

> **Area**: Catalogo libri
> **Owner**: backend-team
> **Stato**: Approved · v1.0

## Obiettivo
Gestire l'anagrafica dei libri della biblioteca: ricerca, dettaglio, registrazione di nuove copie.

## User Stories

### US-CAT-01 — Ricerca libri
> Come bibliotecario, voglio cercare libri per titolo o autore, per trovare velocemente la collocazione.

### US-CAT-02 — Dettaglio libro
> Come bibliotecario, voglio vedere il dettaglio di un libro, incluso il numero di copie disponibili.

### US-CAT-03 — Registrazione nuovo libro
> Come bibliotecario, voglio registrare un nuovo libro nel catalogo, indicando ISBN, titolo, autore e numero di copie iniziali.

### US-CAT-04 — Aggiornamento copie
> Come bibliotecario, voglio aggiornare il numero di copie disponibili di un libro (carico/scarico).

## Modello dati
```
Book
- BookId       : Guid                  (PK)
- Isbn         : string                (UNIQUE, formato ISBN-13)
- Title        : string
- Author       : string
- TotalCopies  : int                   (>= 0)
- AvailableCopies : int                (>= 0, <= TotalCopies)
```

## Endpoint API

| Verbo | Path | Descrizione |
|-------|------|-------------|
| GET   | /api/books                 | Lista libri con filtri opzionali `?title=` `?author=` |
| GET   | /api/books/{id}            | Dettaglio singolo libro |
| POST  | /api/books                 | Registra nuovo libro |
| PUT   | /api/books/{id}/copies     | Aggiorna numero totale copie |

## Acceptance Criteria

| ID | Criterio |
|----|----------|
| AC-CAT-01 | L'ISBN deve essere univoco. Tentativo di insert duplicato → `409 Conflict` |
| AC-CAT-02 | `AvailableCopies` non può mai essere negativo |
| AC-CAT-03 | `AvailableCopies` non può mai superare `TotalCopies` |
| AC-CAT-04 | La ricerca per titolo è case-insensitive e supporta substring match |
| AC-CAT-05 | Eliminare un libro con prestiti attivi è proibito → `409 Conflict` |

## Note
- Il formato ISBN va validato (regex ISBN-13).
- Non sono richiesti audit log in questa wave.
