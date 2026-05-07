# SPEC-03 — Members

> **Area**: Anagrafica membri
> **Owner**: backend-team
> **Stato**: Approved · v1.0

## Obiettivo
Gestire l'anagrafica dei membri della biblioteca: registrazione, dettaglio, disattivazione.

## User Stories

### US-MEM-01 — Registrazione nuovo membro
> Come bibliotecario, voglio registrare un nuovo membro indicando nome, email e data di nascita.

### US-MEM-02 — Dettaglio membro
> Come bibliotecario, voglio vedere il dettaglio di un membro inclusi i prestiti attivi.

### US-MEM-03 — Disattivazione membro
> Come bibliotecario, voglio disattivare un membro che ha violato il regolamento, impedendogli nuovi prestiti.

## Modello dati
```
Member
- MemberId   : Guid           (PK)
- FullName   : string
- Email      : string         (UNIQUE)
- BirthDate  : DateOnly
- IsActive   : bool           (default: true)
- CreatedOn  : DateTime
```

## Regole di business

| ID | Regola |
|----|--------|
| BR-MEM-01 | **Età minima per la registrazione: 18 anni compiuti** alla data di registrazione |
| BR-MEM-02 | L'email deve essere univoca nel sistema |
| BR-MEM-03 | L'email deve avere formato valido (RFC 5322 base) |
| BR-MEM-04 | Un membro disattivato (`IsActive == false`) non può aprire nuovi prestiti |

## Endpoint API

| Verbo | Path | Descrizione |
|-------|------|-------------|
| POST  | /api/members                 | Registra nuovo membro |
| GET   | /api/members/{id}            | Dettaglio membro |
| PUT   | /api/members/{id}/deactivate | Disattiva un membro |

## Acceptance Criteria

| ID | Criterio |
|----|----------|
| AC-MEM-01 | Tentativo di registrare un membro minorenne → `400 Bad Request` con messaggio "Member must be at least 18 years old" |
| AC-MEM-02 | Tentativo di registrare email già esistente → `409 Conflict` |
| AC-MEM-03 | Email malformata → `400 Bad Request` |
| AC-MEM-04 | Disattivazione: `IsActive` diventa `false`, ma i prestiti attivi non vengono cancellati |
