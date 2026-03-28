# Utvikling i Team Avhørsflyt som en backend-utvikler.

## Designprinsipper

### Generelle prinsipper

- Immutability by default
- Composition (has a) over inheritance (is a)
- Given when then (GWT) testing ved bruk av BehaviorSpec
- Systemet skal fungere selv om tjenester vi avhenger av ikke er tilgjengelig.

### Monorepo

Vi ønsker at så mye som mulig av koden er lett tilgjengelig for utviklere i ett repo, slik at man enklere kan få
oversikt over hvordan ting henger sammen.

### Contract-first design

Vi ønsker å jobbe "Contract First", det betyr at vi sammen oppdaterer openapi filene under `openapi/` mappen.
Disse bygges automatisk til DTOer og klienter for frontend/backend slik at man kan jobbe hver for seg med en felles
kontrakt.

### Hexagonal (Ports and adapters) / Clean architecture / Domain driven design

Vi ønsker å splitte opp koden i en logisk struktur der vi har (som regel) følgende moduler:

#### Domain

Domenemodulen skal ikke innheolde noen eksterne avhengigheter med unntak av serialisering.
Den skal kun inneholde forretningslogikk og være uavhengig av infrastruktur.
Domenemodulen inneholder Ports, kommandoer og queries. Disse skal gjøre domenelogikk.

- Ports: Interfacer som har implementasjoner i andre moduler.
- Commands: Kommandoer som representerer handlinger i domenet, f.eks "CreateDocumentCommand".
- Queries: Spørringer som representerer lesing av data, f.eks "GetDocumentByIdQuery". Brukes ofte i mer kompleks
  domenelogikk som setter sammen flere elementer.

#### API

Om tjenesten/featuren skal ha et API skal denne gjøre all jobb med å generere DTOer fra openapi-filene.
Denne modulen avhenger av domenemodulen, hvor den bruker Commands, Queries og Ports til å interagere med de andre
modulene.

#### Data

Datamodulen kobler databaser og eksterne tjenester, og returnerer dette. Det kan være flere forskjellige "Datamoduler".
Disse
modulene implementerer ofte Porter fra domenemodulen.

#### Service / Module

Dette er modulen som kobler alt sammen. i en feature vil dette være en "module" og i en tjeneste vil dette være en "
service"

