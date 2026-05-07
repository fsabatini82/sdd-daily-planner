# Library System — Implementation Plan

> **Sprint corrente**: Sprint 4 (2026-04-29 → 2026-05-13)
> **Ultimo aggiornamento manuale**: 2026-04-21

## Wave overview

| Wave | Area | Stato dichiarato | Target sprint |
|------|------|------------------|---------------|
| W1   | Catalog (SPEC-01)        | 🟡 IN PROGRESS  | Sprint 3 |
| W2   | Loans (SPEC-02)          | 🟡 IN PROGRESS  | Sprint 4 |
| W3   | Members (SPEC-03)        | 🔴 TODO         | Sprint 5 |
| W4   | Reservations (SPEC-04)   | ⚪ NEXT         | Sprint 6 |

---

## W1 — Catalog

**Stato dichiarato: 🟡 IN PROGRESS**

- [x] Modello `Book`
- [x] Repository in-memory
- [ ] Endpoint `GET /api/books`
- [ ] Endpoint `GET /api/books/{id}`
- [ ] Endpoint `POST /api/books` con validazione ISBN
- [ ] Endpoint `PUT /api/books/{id}/copies`
- [ ] Acceptance test AC-CAT-01..05

**Note**: Iniziato a metà Sprint 3, attendiamo conferma dal team su validatore ISBN-13 prima di chiudere.

---

## W2 — Loans

**Stato dichiarato: 🟡 IN PROGRESS**

- [x] Modello `Loan`
- [x] Apertura prestito (POST /api/loans)
- [x] Restituzione prestito
- [ ] Lista prestiti attivi
- [ ] Validazione max 3 prestiti per membro (BR-LOAN-02)
- [ ] Acceptance test AC-LOAN-01..05

**Note**: La logica di calcolo `DueDate` è stata dimenticata nel kick-off di sprint, da rivedere insieme al PO.

---

## W3 — Members

**Stato dichiarato: 🔴 TODO**

- [ ] Modello `Member`
- [ ] Endpoint registrazione
- [ ] Validazione età (BR-MEM-01)
- [ ] Validazione email univoca
- [ ] Disattivazione membro
- [ ] Acceptance test AC-MEM-01..04

**Note**: Da pianificare in Sprint 5. Dipendenze: nessuna sui catalog/loans.

---

## W4 — Reservations

**Stato dichiarato: ⚪ NEXT (non iniziata, spec non ancora redatta)**

Idea: code di prenotazione per libri con 0 copie disponibili. Da raccogliere requisiti.

---

## Risk register

| ID | Rischio | Mitigazione | Owner |
|----|---------|-------------|-------|
| R1 | Drift tra spec e codice — nessun controllo automatico | Da definire | TBD |
| R2 | Plan obsoleto rispetto al codice reale | Aggiornamento manuale settimanale | PM |
