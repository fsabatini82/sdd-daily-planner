# SPEC-02 — Loans

> **Area**: Prestiti
> **Owner**: backend-team
> **Stato**: Approved · v1.1

## Obiettivo
Gestire il ciclo di vita dei prestiti: apertura, restituzione, gestione ritardi.

## User Stories

### US-LOAN-01 — Apertura prestito
> Come bibliotecario, voglio aprire un prestito per un membro, scegliendo un libro disponibile.

### US-LOAN-02 — Restituzione
> Come bibliotecario, voglio registrare la restituzione di un libro, ripristinando la disponibilità.

### US-LOAN-03 — Prestiti attivi
> Come bibliotecario, voglio vedere la lista dei prestiti attivi (non ancora restituiti).

## Modello dati
```
Loan
- LoanId      : Guid          (PK)
- BookId      : Guid          (FK → Book)
- MemberId    : Guid          (FK → Member)
- LoanedOn    : DateTime      (UTC)
- DueDate     : DateTime      (UTC, calcolata)
- ReturnedOn  : DateTime?     (UTC, nullable)
```

## Regole di business

| ID | Regola |
|----|--------|
| BR-LOAN-01 | **Durata standard del prestito: 14 giorni di calendario** dalla data di apertura. `DueDate = LoanedOn + 14 days` |
| BR-LOAN-02 | **Un membro può avere al massimo 3 prestiti attivi contemporaneamente**. Tentativo di aprire un 4° prestito → `409 Conflict` |
| BR-LOAN-03 | Non è possibile aprire un prestito se `Book.AvailableCopies == 0` → `409 Conflict` |
| BR-LOAN-04 | Solo membri attivi (non disattivati) possono aprire prestiti |
| BR-LOAN-05 | Alla restituzione, `Book.AvailableCopies` viene incrementato |
| BR-LOAN-06 | Mora per ritardo: **€0.50 al giorno** dopo `DueDate` (non implementata in questa wave, ma la struttura dati deve supportarla) |

## Endpoint API

| Verbo | Path | Descrizione |
|-------|------|-------------|
| POST  | /api/loans                 | Apre un nuovo prestito |
| PUT   | /api/loans/{id}/return     | Registra la restituzione |
| GET   | /api/loans/active          | Lista prestiti attivi |
| GET   | /api/loans?memberId={id}   | Storico prestiti del membro |

## Acceptance Criteria

| ID | Criterio |
|----|----------|
| AC-LOAN-01 | Aprendo un prestito, `DueDate` è esattamente `LoanedOn + 14 giorni` |
| AC-LOAN-02 | Tentativo di aprire un prestito quando il membro ne ha già 3 attivi → `409 Conflict` con messaggio chiaro |
| AC-LOAN-03 | Tentativo di aprire un prestito su libro con 0 copie disponibili → `409 Conflict` |
| AC-LOAN-04 | Restituzione: `ReturnedOn` valorizzato e `AvailableCopies` del libro incrementato |
| AC-LOAN-05 | `GET /api/loans/active` ritorna solo prestiti con `ReturnedOn == null` |

## Out of scope (next wave)
- Calcolo automatico mora
- Notifiche di scadenza
- **Reservations**: code di prenotazione su libri non disponibili (vedere SPEC-04 in roadmap)
