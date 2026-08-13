# GrandNode 2 — Audyt architektoniczny i roadmapa

**Wersja dokumentu:** 1.0
**Data:** 2026-08-12
**Gałąź:** `develop` (commit bazowy `81ff8e2d8`)
**Wersja produktu:** 2.4.0, .NET 10 (`net10.0`), SDK 10.0.100
**Zakres audytu:** całe repozytorium `src/` — 4701 plików `.cs`, 76 projektów (bez `bin`/`obj`)

Dokument jest źródłem prawdy dla prac architektonicznych. Każde zadanie ma być wykonywalne pojedynczo,
przez człowieka lub agenta AI, bez ponownej analizy całego repozytorium.

---

## 1. Executive Summary

### Czym GrandNode jest dzisiaj

GrandNode 2 to **Modular Monolith** na .NET 10, z bazą dokumentową (MongoDB, z alternatywnym
sterownikiem LiteDB), zbudowany wokół czterech hostów webowych (`Grand.Web`, `Grand.Web.Admin`,
`Grand.Web.Store`, `Grand.Web.Vendor`), warstwy usług biznesowych (`Grand.Business.*`), własnego
mediatora (`Grand.Mediator`) i systemu wtyczek ładowanych z dysku (`Grand.Infrastructure.Plugins`).

Struktura projektów jest czytelna i konsekwentna:

```
Grand.SharedKernel  →  Grand.Domain  →  Grand.Data  →  Grand.Infrastructure
                                                   ↘
                       Grand.Business.Core  →  Grand.Business.{Catalog,Checkout,…}
                                                   ↘
                       Grand.Web.Common  →  Grand.Web.AdminShared  →  hosty webowe
```

Moduły biznesowe **nie referencują się nawzajem** — komunikują się przez interfejsy, komendy, zapytania
i zdarzenia zadeklarowane w `Grand.Business.Core`. To jest realna, wyegzekwowana przez kompilator granica
i jest to najmocniejszy element architektury.

### Poziom dojrzałości

Projekt jest **dojrzały technologicznie i aktywnie utrzymywany**, ale **niedojrzały operacyjnie**.
Stack jest aktualny (net10.0, MongoDB.Driver 3.10, centralne zarządzanie pakietami, Aspire, OpenTelemetry,
CodeQL, SonarQube). Ostatnie ~20 commitów to systematyczny hardening: eliminacja blokowania wątków na
sterowniku Mongo, walidacja antiforgery, uszczelnienie parsera zapytań API, współdzielony `MongoClient`,
naprawa bramki wersji wtyczek. Zespół wie, gdzie są problemy, i je adresuje.

Jednocześnie: **brak testów integracyjnych, brak testów architektonicznych, brak readiness health checks,
brak metryk domenowych, brak transakcji, brak kontroli współbieżności**. To nie są braki „clean code" —
to braki, które w produkcji materializują się jako duplikaty numerów zamówień, rozjechane stany magazynowe
i niediagnozowalne incydenty.

### Największe problemy

1. **Potrójna, ręcznie zsynchronizowana duplikacja panelu administracyjnego.** `Grand.Web.Admin` (70
   kontrolerów), `Grand.Web.Store` (36), `Grand.Web.Vendor` (11) plus `Grand.Web.AdminShared` (592 pliki).
   `ProductController` istnieje w trzech kopiach (2478 / 2625 / 2584 linii), `ProductViewModelService` w dwóch
   (2571 / 2381 linii). Zmierzona różnica między `AdminShared` a `Vendor` to 1768 linii diffa na ~2500 linii
   pliku. Każda poprawka w katalogu produktów musi być zaaplikowana ręcznie w 2–3 miejscach.
2. **Brak atomowości na krytycznych ścieżkach danych.** Numer zamówienia jest nadawany przez
   read-max-then-increment (`OrderService.InsertOrder`), stany magazynowe przez read-modify-write
   (`InventoryManageService.UpdateStockProduct`), a `PlaceOrderCommandHandler` (996 linii, 31 zależności
   w konstruktorze) zapisuje kilkanaście dokumentów bez żadnej sesji ani kompensacji.
3. **Ochrona przed XSS oparta na czarnej liście.** `NoScriptsAttribute` to regex blacklist użyty w 56
   miejscach, a treści autorskie (`FullDescription` itd.) trafiają na storefront przez `Html.Raw`
   (80 wystąpień w `Grand.Web/Views`) i `v-html`. Vendor i Store manager to konta o niższym zaufaniu niż
   główny administrator — mają wektor stored XSS na klientów i na admina.
4. **Izolacja najemcy (vendor/store) realizowana ad hoc.** 88 ręcznych porównań
   `x.VendorId != CurrentVendor.Id` rozsianych po kontrolerach. Jedno pominięcie = IDOR.
5. **Obserwowalność szczątkowa.** Health check zwraca tylko `self` — nie sprawdza MongoDB ani Redis.
   Zero `ActivitySource`, zero `Meter`, zero metryk domenowych. Za to e-mail klienta trafia jako tag
   do trace'ów (`ContextLoggingMiddleware`) — czyli PII w telemetrii.

### Największe ryzyka

| Ryzyko | Materializacja |
|---|---|
| Duplikaty `OrderNumber` przy równoległym checkoucie | Konflikt numeracji, problemy księgowe, rozjazd z ERP |
| Lost update na stanie magazynowym | Sprzedaż poniżej zera, oversell |
| Częściowo zapisane zamówienie po wyjątku w środku `PlaceOrder` | Zamówienie bez pozycji / płatność bez zamówienia |
| Stored XSS przez wtyczkę/vendora | Przejęcie sesji administratora |
| Refaktoryzacja bez siatki bezpieczeństwa | 1826 testów jednostkowych na mockach nie wykryje regresji w integracji |
| Niediagnozowalny incydent produkcyjny | Brak metryk i readiness — MTTR liczony w godzinach |

### Największe mocne strony

- **Granice modułów biznesowych są realne** i wyegzekwowane przez graf referencji projektów.
- **Własny `Grand.Mediator`** — ~10 plików zamiast zewnętrznej zależności z niepewną licencją. Świadoma,
  dobrze wykonana decyzja.
- **Hardening parsera zapytań API** (`ApiQueryOptions`) — whitelist członków modelu, limit długości,
  `ParsingConfig` blokujący `new`, rozwiązywanie typów i keywords kontekstowe. Bardzo dobra robota.
- **Antiforgery globalnie** przez `[AutoValidateAntiforgeryToken]` na wszystkich klasach bazowych kontrolerów.
- **Infrastruktura budowania wtyczek** (`src/Build/Grand.Plugin.props`) — jedno miejsce z listą assembly
  hosta, `Private=false`, `ExcludeAssets=runtime`. Rozwiązuje realny problem, dobrze udokumentowane.
- **Kultura komentarza „dlaczego"** — komentarze w kodzie tłumaczą decyzje, nie powtarzają kodu.
- **CI**: build + pełny zestaw testów na PR + CodeQL + SonarQube + obrazy Docker.

### Najważniejsze kierunki zmian

1. **Faza 0:** zamknąć dziury w integralności danych (numeracja zamówień, stany magazynowe) i najostrzejsze
   luki bezpieczeństwa. Bez tego reszta jest budowaniem na ruchomym piasku.
2. **Faza 1–2:** siatka bezpieczeństwa (testy integracyjne + architektoniczne), a dopiero potem konsolidacja
   trzech paneli administracyjnych. Kolejność jest nieodwracalna — konsolidacja bez testów integracyjnych
   to gwarantowana regresja.
3. **Faza 6:** obserwowalność, bo bez niej nie da się zweryfikować efektów faz 4 i 7.

**Czego NIE robić:** nie wprowadzać rich domain model, nie dodawać pipeline behaviors do mediatora „bo
MediatR tak miał", nie zastępować `IRepository<T>` czymś „czystszym", nie dzielić na mikroserwisy.
Szczegóły w sekcji 15.

### Oceny

| Obszar | Ocena |
| ------------------- | ----: |
| Architektura | 6/10 |
| Bezpieczeństwo | 6/10 |
| Maintainability | 5/10 |
| Performance | 6/10 |
| Reliability | 5/10 |
| Testability | 6/10 |
| Observability | 4/10 |
| Scalability | 6/10 |
| Plugin Architecture | 5/10 |
| **Overall Maturity** | **6/10** |

#### Uzasadnienia ocen

**Architektura — 6/10.** Za: czytelny podział na warstwy, moduły biznesowe bez wzajemnych referencji,
konsekwentny wzorzec `IStartupApplication` do składania kontenera, `Grand.Mediator` jako świadomie
ograniczona abstrakcja. Przeciw: `Grand.Business.Core` jest wspólnym kontraktem dla *wszystkich* domen
(368 plików: `Interfaces`, `Commands`, `Queries`, `Events`, `Dto`, `Utilities`) — zmiana kontraktu katalogu
przekompilowuje checkout; `Grand.Web.Common` referencuje wszystkie dziewięć projektów `Grand.Business.*`,
więc każdy host ładuje cały monolit; `Grand.Web` referencuje `Grand.Web.Admin`, `.Store` i `.Vendor`, przez
co storefront zawiera kod wszystkich paneli; `ValidateScopes = false` w każdym `Program.cs` wyłącza jedyną
automatyczną kontrolę poprawności grafu zależności.

**Bezpieczeństwo — 6/10.** Za: antiforgery globalnie, PBKDF2 z pepperem i on-login upgrade, fail-fast na
słabym kluczu JWT (`ApiSecurityStartup`), whitelist w parserze zapytań API, `[DenySystemAccount]`,
ograniczenie IP dla panelu, CodeQL w CI. Przeciw: XSS na czarnej liście przy 80 `Html.Raw`, izolacja
najemcy ad hoc (88 ręcznych sprawdzeń), niebezpieczne domyślne ustawienia (`UseDefaultSecurityHeaders`,
`UseHsts`, `UseHttpsRedirection`, `CookieSecurePolicyAlways` — wszystkie `false`), upload wczytujący
całe ciało do `byte[]` przed sprawdzeniem limitu rozmiaru, PII w telemetrii.

**Maintainability — 5/10.** Najniżej oceniony obszar poza obserwowalnością i to niemal wyłącznie z powodu
duplikacji paneli. Poza tym kod jest czytelny, spójnie sformatowany, z sensownymi komentarzami i tylko
19 znacznikami TODO/HACK/FIXME w 4701 plikach. Ale `PlaceOrderCommandHandler` z 31 zależnościami,
`MessageProviderService` z 1974 liniami i trzy kopie `ProductController` po ~2500 linii to koszt zmiany,
który rośnie liniowo z każdą funkcją.

**Performance — 6/10.** Za: aktywna praca nad wykonywaniem zapytań na sterowniku zamiast blokowania
wątków (commity #771, #776), cache katalogu po grupie klienta zamiast po kliencie (#768), read-through
cache z `SemaphoreSlim` per klucz, kompresja Brotli/Gzip, Redis pub/sub do inwalidacji cache między
instancjami. Przeciw: brak `SizeLimit` na `IMemoryCache`, blokujące `.Result` w eksporcie, brak
paginacji w części ścieżek administracyjnych, indeksy tworzone wyłącznie przy instalacji.

**Reliability — 5/10.** Brak transakcji, brak kontroli współbieżności (`BaseEntity` nie ma pola wersji),
scheduler in-process z pętlą, która potrafi trwale zakończyć się przez `break`, `catch (Exception)`
połykający błędy w pętli schedulera, brak retry/circuit breaker wokół wywołań do bramek płatności.
Za: atomowe przejęcie zadania przez `TryClaimTaskRun` przy wielu instancjach — to jest zrobione dobrze.

**Testability — 6/10.** 1826 metod `[TestMethod]` w 309 plikach, 21 projektów testowych, sensowne pokrycie
warstwy biznesowej (Catalog 354, Checkout 231, Mapping 238). Ale: to prawie wyłącznie testy jednostkowe na
Moq. Zero testów integracyjnych z prawdziwym MongoDB (mimo że CI *uruchamia* kontener Mongo), zero testów
architektonicznych, 9 testów dla całego `Grand.Web`, 15 dla `Grand.Web.Common`. Znany problem z równoległym
uruchamianiem (Customers/Marketing/Messages).

**Observability — 4/10.** `Aspire.ServiceDefaults` daje poprawny szkielet OpenTelemetry (traces, metrics,
logs, eksport OTLP i Azure Monitor), ale nikt z niego nie korzysta na poziomie domeny: zero `ActivitySource`,
zero `Meter`, zero liczników zamówień/płatności/koszyków. Health check to jedna linia zwracająca `Healthy`.
Nie ma `/health/ready`. Nie ma korelacji z ID zamówienia. Jest za to e-mail klienta w tagach trace'a.

**Scalability — 6/10.** Architektura *pozwala* na skalowanie poziome: Redis pub/sub synchronizuje cache
między instancjami, `MongoClient` jest singletonem, Data Protection można persystować do Redis/Azure,
zadania cykliczne mają atomowe przejęcie, a `Aspire.AppHost` uruchamia dwie repliki właśnie po to, żeby
to testować. Ogranicza: shadow copy wtyczek wymaga wyłączenia przy współdzielonym content root, brak
kontroli współbieżności czyni wiele instancji groźniejszymi niż jedną, `AsyncLocal` w `ContextAccessor`
nie przenosi kontekstu do zadań w tle uruchomionych spoza pipeline'u.

**Plugin Architecture — 5/10.** Za: prosty, przewidywalny model (`IPlugin`, `IProvider`, `PluginInfoAttribute`,
`IStartupApplication`), dobrze rozwiązany problem budowania (`Grand.Plugin.props`), 16 wtyczek referencyjnych
w repo pokrywających płatności, wysyłkę, podatki, widżety, autoryzację i motyw. Przeciw: wtyczki ładowane do
`AssemblyLoadContext.Default` — brak izolacji, brak wyładowania, konflikt wersji zależności nie do rozwiązania;
zgodność wersji to **dokładne porównanie stringów** `Major.Minor`, więc każde wydanie minor unieważnia
wszystkie wtyczki; brak jawnie wersjonowanego, udokumentowanego publicznego API — kontraktem jest de facto
całe `Grand.Business.Core` plus `Grand.Web.Common`; `Theme.Modern` referencuje `Grand.Web.csproj`, czyli
wtyczka zależy od całej aplikacji hosta.

---

## 2. Najważniejsze problemy

Ranking według iloczynu (prawdopodobieństwo materializacji × koszt materializacji), a nie według
„odległości od wzorca".

---

### DATA-001 – Nieatomowe nadawanie numeru zamówienia

**Priorytet:** P0
**Kategoria:** Data
**Dotyczy:** `Grand.Business.Checkout` / `OrderService` / `InsertOrder`
**Status:** Zrealizowane — PR #778, zadania `DATA-011` i `DATA-012`

#### Problem

Numer zamówienia był wyznaczany przez odczytanie największego istniejącego numeru i dodanie jedynki,
w osobnym zapytaniu, poza jakąkolwiek transakcją, tuż przed wstawieniem dokumentu.

#### Dowody

`src/Business/Grand.Business.Checkout/Services/Orders/OrderService.cs:209-217`

```csharp
public virtual async Task InsertOrder(Order order)
{
    ArgumentNullException.ThrowIfNull(order);

    var orderExists = _orderRepository.Table.OrderByDescending(x => x.OrderNumber).Select(x => x.OrderNumber)
        .FirstOrDefault();
    order.OrderNumber = orderExists != 0 ? orderExists + 1 : 1;

    await _orderRepository.InsertAsync(order);
    ...
}
```

Dwa niezależne defekty w trzech linijkach:

1. `FirstOrDefault()` bez `Async` — synchroniczne wykonanie zapytania LINQ na sterowniku MongoDB, czyli
   zablokowany wątek puli w ścieżce checkoutu. To dokładnie ta klasa problemu, którą commity #771 i #776
   usuwały gdzie indziej.
2. Odczyt-i-inkrementacja bez atomowości — dwa równoległe checkouty odczytają tę samą wartość.

Indeks na `Order.OrderNumber` istniał (malejący, `InstallationService.CreateIndexes`), ale nie był
unikalny — baza nie miała jak odrzucić duplikatu.

#### Dlaczego to jest problem

`OrderNumber` jest identyfikatorem biznesowym pokazywanym klientowi, drukowanym na fakturze i używanym
do integracji. Duplikat nie jest wykrywany przez bazę (brak unikalnego indeksu), więc materializuje się
dopiero na etapie księgowości albo synchronizacji z ERP — czyli tygodnie po zdarzeniu, gdy naprawa
oznacza ręczną korektę dokumentów.

#### Ryzyko

- **Dane:** dwa zamówienia z tym samym numerem, niewykrywalne przez bazę.
- **Użytkownicy:** klient dostaje potwierdzenie z numerem wskazującym na cudze zamówienie.
- **Utrzymanie:** naprawa post factum wymaga ręcznej renumeracji i korekt księgowych.
- **Wydajność:** blokada wątku puli przy każdym zamówieniu; pod obciążeniem to wprost thread pool starvation.
- **Rozwój:** blokuje skalowanie poziome — im więcej instancji, tym większe okno wyścigu.

#### Rozwiązanie (zrealizowane)

Numer nadal wynika z kolekcji `Order` — nie ma drugiego źródła prawdy. Atomowość zapewnia baza:
indeks na `OrderNumber` jest unikalny, a `InsertOrder` po odrzuceniu wstawienia odczytuje numer
ponownie i ponawia próbę (limit pięciu podejść). Odczyt idzie przez `FirstOrDefaultAsync`, czyli
na sterowniku, a indeks malejący sprowadza go do jednego trafienia zamiast skanu rosnącego z liczbą
zamówień. Odrzucone `InsertOne` nic nie zapisuje, więc numeracja pozostaje ciągła i nie ma czego
sprzątać.

Oba repozytoria tłumaczą błąd duplikatu klucza na `DuplicateKeyGrandException`
(`src/Core/Grand.SharedKernel`), dzięki czemu warstwa biznesowa reaguje na kolizję bez referencji
do `MongoDB.Driver` ani `LiteDB`.

#### Rozważone i odrzucone

- **Sekwencer na dedykowanej kolekcji liczników** (`FindOneAndUpdate` z `$inc` i `IsUpsert`) — poprawny
  i dający twardą gwarancję zamiast probabilistycznej, ale wprowadza drugie źródło prawdy, nową encję,
  nowy serwis i migrację danych. Pozostaje ścieżką eskalacji razem z wariantem blokowym (Hi/Lo),
  gdyby logi kiedykolwiek pokazały realne kolizje; unikalny indeks zostaje wtedy bez zmian.
- **Transakcje MongoDB** — nie rozwiązują tego wyścigu. Dwie transakcje odczytają to samo maksimum
  i obie wstawią własny dokument; konflikt zapisu wykrywany jest tylko przy modyfikacji tego samego
  dokumentu. Bez unikalnego indeksu transakcja nic nie zmienia, z nim jest zbędna.
- **Blokada rozproszona** — dałaby twardą gwarancję bez ponawiania, ale w repozytorium nie ma żadnej
  infrastruktury blokad; trzeba by ją zbudować, co jest droższe niż licznik.
- **Numer generowany bezkolizyjnie z konstrukcji** (schemat typu Snowflake) — wymaga zmiany
  `Order.OrderNumber` z `int` na `long`, czyli breaking change w encji, modelach zaplecza, eksporcie
  i integracjach, a numer przestaje być czytelny dla człowieka.

#### Pozostałe ryzyko

Gwarancja jest probabilistyczna: po wyczerpaniu pięciu prób checkout kończy się wyjątkiem. Okno
kolizji to milisekundy między odczytem a wstawieniem, więc przy realnym tempie zamówień zapas jest
duży — ale to jest różnica względem licznika i warto ją mieć zapisaną.

Na bazie, która już zawiera duplikaty numerów, migracja indeksu nie powstaje: raportuje numery
i zostawia stary indeks. Do czasu ręcznej korekty instalacja nie ma ochrony.

#### Koszt

**S** (zrealizowane)

#### Breaking Change

**Nie** — sygnatura `InsertOrder` bez zmian, żaden kontrakt repozytorium nie został zmieniony.
`GrandException` dostał dodatkowy konstruktor `(string, Exception)` — zmiana addytywna.

---

### DATA-002 – Read-modify-write na stanach magazynowych bez kontroli współbieżności

**Status:** Częściowo zrealizowane — PR #777 scalił cztery zapisy w jeden. Atomowość (`$inc`) nadal
przed nami: zadanie `DATA-013`.

**Priorytet:** P0
**Kategoria:** Data
**Dotyczy:** `Grand.Business.Catalog` / `InventoryManageService` / `UpdateStockProduct`, `AdjustReserved`, `BookReservedInventory`

#### Problem

Stany magazynowe są modyfikowane w pamięci na obiekcie `Product` odczytanym wcześniej, a następnie
zapisywane przez `UpdateField`, który nadpisuje pole wartością bezwzględną. Nie ma pola wersji, nie ma
warunku na zapisie, nie ma użycia atomowego `$inc`.

#### Dowody

Stan po PR #777 — `src/Business/Grand.Business.Catalog/Services/Products/InventoryManageService.cs:692-707`:

```csharp
await _productRepository.UpdateOneAsync(x => x.Id == product.Id,
    UpdateBuilder<Product>.Create()
        .Set(x => x.StockQuantity, product.StockQuantity)
        .Set(x => x.ReservedQuantity, product.ReservedQuantity)
        .Set(x => x.LowStock, ...));
```

Cztery osobne round-tripy zostały zastąpione jednym zapisem — **połowa problemu jest rozwiązana**.
Pozostaje druga: `Set` zapisuje wartość wyliczoną z odczytu sprzed nieokreślonego czasu, a nie
przyrost. Modyfikacje w pamięci: linie 60, 74, 88, 102, 184, 194, 207, 227, 298 i dalej — wszystkie
w formie `x.StockQuantity -= n` / `+= n`.

`IRepository<T>.IncField` — atomowy `$inc` — **istnieje** (`src/Core/Grand.Data/IRepository.cs:78`,
implementacja `src/Core/Grand.Data/Mongo/MongoRepository.cs:168`), ale w całej warstwie biznesowej jest
użyty **dokładnie raz**: `src/Business/Grand.Business.Catalog/Services/Products/ProductService.cs:370`.

`BaseEntity` (`src/Core/Grand.Domain/BaseEntity.cs`) nie ma pola wersji ani znacznika współbieżności —
tylko `CreatedOnUtc`, `CreatedBy`, `UpdatedOnUtc`, `UpdatedBy`, `UserFields`.

#### Dlaczego to jest problem

Klasyczny lost update. Dwa równoległe zamówienia na ten sam produkt odczytują `StockQuantity = 10`,
oba odejmują 1, oba zapisują 9. Sprzedano dwie sztuki, magazyn pokazuje jedną mniej. Przy sprzedaży
poniżej `MinStockQuantity` skutkiem jest oversell — realna strata finansowa i obsługa reklamacji.

Drugi problem — niespójny stan dokumentu między pierwszym a czwartym zapisem (nowy `StockQuantity`,
stary `LowStock`), widoczny dla innych żądań — zniknął wraz z PR #777.

#### Ryzyko

- **Dane:** narastający rozjazd stanu magazynowego względem rzeczywistości.
- **Użytkownicy:** zamówienie przyjęte na towar, którego nie ma; anulowanie po fakcie.
- **Bezpieczeństwo:** wektor nadużycia — celowo równoległe zamówienia na towar deficytowy.
- **Rozwój:** blokuje skalowanie poziome.
- **Utrzymanie:** rekoncyliacja stanów wymaga ręcznej inwentaryzacji.

#### Rekomendacja

1. **Krótkoterminowo (P0):** ~~scalić wielopolowe zapisy w jedno `UpdateOneAsync` z `UpdateBuilder`~~
   (zrobione — PR #777). Zostaje zamiana ścieżek inkrementalnych na `IncField` (atomowy `$inc`),
   czyli przekazywanie do bazy przyrostu zamiast wyliczonej wartości. Warunek „nie schodź poniżej zera"
   wyrazić filtrem w `UpdateOneAsync` (`x.Id == id && x.StockQuantity >= n`) i sprawdzać liczbę
   zmodyfikowanych dokumentów.
2. **Średnioterminowo (P1):** dodać pole wersji do `BaseEntity` i wsparcie optimistic concurrency
   w `IRepository<T>`. Stosować tam, gdzie `$inc` nie wystarcza — przy zapisach obiektów zagnieżdżonych
   (`ProductWarehouseInventory`, `ProductAttributeCombination`).

#### Koszt

**M** (krok 1) / **L** (krok 2 — dotyka `BaseEntity`, czyli wszystkich encji, serializacji i migracji)

#### Breaking Change

**Częściowo.** Krok 1 nie zmienia kontraktów. Krok 2 dodaje pole do `BaseEntity`, zmieniając kształt
każdego dokumentu i wymuszając migrację; zewnętrzne implementacje `IRepository<T>` przestaną się kompilować.

---

### DATA-003 – Brak atomowości i kompensacji przy składaniu zamówienia

**Priorytet:** P1
**Kategoria:** Data
**Dotyczy:** `Grand.Business.Checkout` / `PlaceOrderCommandHandler` / `Handle`

#### Problem

`PlaceOrderCommandHandler` zapisuje kilkanaście dokumentów w wielu kolekcjach (zamówienie, transakcja
płatnicza, rezerwacje, stany magazynowe, punkty lojalnościowe, bony podarunkowe, kupony rabatowe,
koszyk, aukcje) sekwencyjnie, bez sesji, bez transakcji i bez ścieżki kompensacji. Wyjątek w połowie
zostawia trwały, częściowy stan.

#### Dowody

`src/Business/Grand.Business.Checkout/Commands/Handlers/Orders/PlaceOrderCommandHandler.cs` — 996 linii,
**31 zależności w konstruktorze** (`IOrderService`, `IInventoryManageService`, `IPaymentService`,
`IPaymentTransactionService`, `IGiftVoucherService`, `IDiscountService`, `IProductReservationService`,
`IAuctionService`, `ICustomerService`, `IMessageProviderService` i dalej).

Nigdzie w `src/Core/Grand.Data/` nie występuje `IClientSessionHandle`, `StartSession`, `StartTransaction`
ani `WithTransaction`. `IRepository<T>` nie ma pojęcia jednostki pracy — każda metoda to osobny zapis.
`IDatabaseContext` też nie eksponuje sesji.

#### Dlaczego to jest problem

MongoDB od 4.0 obsługuje transakcje wielodokumentowe na replica set. GrandNode ich nie używa — co jest
uzasadnioną decyzją dla operacji CRUD, ale nie dla składania zamówienia, które jest dokładnie tym
przypadkiem, dla którego transakcje istnieją.

Wyjątek po zapisaniu `Order`, a przed zapisaniem `PaymentTransaction`, daje zamówienie bez płatności.
Wyjątek po zdjęciu stanu magazynowego, a przed zapisaniem zamówienia, daje zniknięty towar bez zamówienia.
Obie sytuacje wymagają ręcznej interwencji i nie są w żaden sposób raportowane.

#### Ryzyko

- **Dane:** trwale niespójny stan między kolekcjami, bez mechanizmu wykrywania.
- **Użytkownicy:** obciążona karta bez zamówienia albo zamówienie bez rezerwacji towaru.
- **Utrzymanie:** brak narzędzia do wykrycia sierot — trzeba pisać skrypty ad hoc.
- **Rozwój:** 31 zależności czyni handler praktycznie niemodyfikowalnym.
- **Contributorzy:** nikt z zewnątrz nie zmodyfikuje bezpiecznie 996-liniowej metody z 31 zależnościami.

#### Rekomendacja

1. **Rozbić handler** na jawne fazy (`ValidateOrder`, `BuildOrder`, `PersistOrder`, `ApplySideEffects`,
   `Notify`) jako osobne, testowalne klasy. To warunek wykonalności kroków 2 i 3.
2. **Wprowadzić opcjonalne wsparcie transakcji** w `IRepository<T>`/`IDatabaseContext` — sesja przekazywana
   jawnie, aktywna tylko gdy `DatabaseConfig` deklaruje replica set. Standalone MongoDB i LiteDB muszą
   dalej działać (degradacja do obecnego zachowania).
3. **Objąć transakcją fazę `PersistOrder`.** Efekty uboczne nietransakcyjne (mail, wywołanie bramki
   płatniczej) zostawić poza nią — one wymagają outboxa, nie transakcji.

Jeśli krok 2 okaże się zbyt kosztowny: minimum akceptowalne to **jawna kompensacja** — zapis kroków
w dokumencie zamówienia i zadanie cykliczne wykrywające zamówienia zatrzymane w stanie niepełnym.

#### Koszt

**XL**

#### Breaking Change

**Częściowo.** Rozbicie handlera zmienia klasy wewnętrzne `Grand.Business.Checkout` (kontraktem jest
`PlaceOrderCommand` w `Grand.Business.Core`, ten się nie zmienia). Wsparcie transakcji rozszerza
`IRepository<T>`, co złamie zewnętrzne implementacje.

---

### ARCH-001 – Potrójna duplikacja panelu administracyjnego

**Priorytet:** P1
**Kategoria:** Architecture / Maintainability
**Dotyczy:** `Grand.Web.Admin`, `Grand.Web.Store`, `Grand.Web.Vendor`, `Grand.Web.AdminShared`

#### Problem

Istnieją trzy panele zaplecza obsługujące w dużej mierze te same encje, z trzema oddzielnymi zestawami
kontrolerów, modeli, walidatorów i widoków, synchronizowanymi ręcznie.

#### Dowody

| Projekt | Kontrolery | Pliki `.cs` | Widoki `.cshtml` |
|---|---:|---:|---:|
| `Grand.Web.Admin` | 70 | 89 | 553 |
| `Grand.Web.Store` | 36 | 65 | 368 |
| `Grand.Web.Vendor` | 11 | 88 | 102 |
| `Grand.Web.AdminShared` | 1 | 592 | 0 |

Trzy kopie `ProductController`:

- `src/Web/Grand.Web.Admin/Controllers/ProductController.cs` — 2478 linii
- `src/Web/Grand.Web.Store/Controllers/ProductController.cs` — 2625 linii
- `src/Web/Grand.Web.Vendor/Controllers/ProductController.cs` — 2584 linii

Dwie kopie `ProductViewModelService`:

- `src/Web/Grand.Web.AdminShared/Services/ProductViewModelService.cs` — 2571 linii
- `src/Web/Grand.Web.Vendor/Services/ProductViewModelService.cs` — 2381 linii

Zmierzone różnice (diff po usunięciu białych znaków):

- `Admin/ProductController` vs `Store/ProductController` → **580 linii różnicy**
- `AdminShared/ProductViewModelService` vs `Vendor/ProductViewModelService` → **1768 linii różnicy**

`Grand.Web.Store` nie ma własnego katalogu `Services` — korzysta z `AdminShared`. `Grand.Web.Vendor` ma
pięć własnych serwisów, każdy duplikujący odpowiednik z `AdminShared` (`OrderViewModelService`,
`ProductViewModelService`, `ShipmentViewModelService`, `MerchandiseReturnViewModelService`,
`VendorReviewViewModelService`).

#### Dlaczego to jest problem

`AdminShared` powstał jako odpowiedź na tę duplikację i częściowo zadziałał — `Grand.Web.Store` faktycznie
z niego korzysta. Ale kontrolery pozostały skopiowane, a `Vendor` poszedł w drugą stronę i skopiował także
serwisy. Efekt: poprawka błędu w edytorze produktu wymaga trzech niezależnych zmian w plikach po ~2500
linii, które już rozjechały się o kilkaset linii, więc nie da się ich zmergować automatycznie.

To jest największy pojedynczy hamulec tempa rozwoju w tym repozytorium.

#### Ryzyko

- **Utrzymanie:** koszt każdej zmiany w katalogu/zamówieniach ×3.
- **Bezpieczeństwo:** poprawka luki zaaplikowana w dwóch z trzech paneli. Historia repo to potwierdza —
  commity #754 („validate antiforgery token on admin POST actions") i #765 („Validate antiforgery on the
  admin file manager") to dokładnie takie pominięcia.
- **Contributorzy:** zewnętrzny kontrybutor nie wie, że musi zmienić trzy pliki; PR wraca do poprawki.
- **Rozwój:** każda nowa funkcja zaplecza kosztuje trzy razy tyle albo trafia tylko do jednego panelu.

#### Rekomendacja

**Nie łączyć paneli w jeden host.** Rozdzielenie jest sensowne — różne modele uwierzytelniania, różne
zakresy danych, możliwość osobnego wdrażania.

Zamiast tego: przenieść logikę kontrolerów do `Grand.Web.AdminShared` jako klasy bazowe sparametryzowane
**strategią zakresu danych** (`IAdminDataScope` zwracający filtr vendor/store/global). Kontrolery w trzech
hostach schodzą do cienkich klas pochodnych deklarujących atrybuty autoryzacji, trasę i zakres — i nic
więcej. Widoki: `AdminShared` dostaje widoki domyślne, hosty nadpisują tylko te, które faktycznie się
różnią (mechanizm `ViewLocationExpander` już to potrafi — jest używany dla motywów).

Kolejność ma znaczenie: **najpierw testy integracyjne pokrywające obecne zachowanie trzech paneli**
(TEST-002), potem konsolidacja. Bez tego to gwarantowana regresja, w tym regresja bezpieczeństwa —
różnice między panelami to w dużej części właśnie sprawdzenia zakresu danych.

#### Koszt

**XL**

#### Breaking Change

**Tak.** Zmienia namespace'y i typy kontrolerów, na których mogą polegać wtyczki nadpisujące widoki
lub rejestrujące pozycje menu. Wymaga wpisu w nocie migracyjnej wtyczek.

---

### SEC-001 – Ochrona przed XSS oparta na czarnej liście przy renderowaniu przez Html.Raw

**Priorytet:** P1
**Kategoria:** Security
**Dotyczy:** `Grand.Web.Common` / `NoScriptsAttribute`, widoki `Grand.Web/Views/**`

#### Problem

Treści HTML wprowadzane przez użytkowników zaplecza (admin, store manager, vendor) są walidowane
regexem-czarną-listą, a następnie renderowane na storefroncie bez enkodowania.

#### Dowody

`src/Web/Grand.Web.Common/Validators/NoScriptsAttribute.cs`:

```csharp
private const string Pattern =
    "<script.*?>.*?</script>|javascript:[^\\s]*|onload=|onerror=|onmouseover=|onclick=|onchange=|onsubmit=";
```

Atrybut jest użyty w **56 miejscach** w modelach `Grand.Web.AdminShared` i `Grand.Web.Vendor`.

Renderowanie: **80 wystąpień `Html.Raw`** w `src/Web/Grand.Web/Views/**/*.cshtml`, m.in.

- `src/Web/Grand.Web/Views/Product/ProductLayout.Simple.cshtml:190` → `@Html.Raw(Model.FullDescription)`
- `src/Web/Grand.Web/Views/Product/ProductLayout.Grouped.cshtml:123` → `@Html.Raw(Model.FullDescription)`
- `src/Web/Grand.Web/Views/Product/CompareProducts.cshtml:115` → `<div v-html="product.FullDescription">`

`FullDescription` jest edytowalne z panelu vendora
(`src/Web/Grand.Web.Vendor/Models/Catalog/ProductModel.cs:50-51`).

W repozytorium nie ma żadnej biblioteki sanityzacji HTML (brak `HtmlSanitizer`, `AngleSharp`
czy odpowiednika w `Directory.Packages.props`).

#### Dlaczego to jest problem

Czarna lista wyrażeń regularnych nie jest mechanizmem obrony przed XSS — jest mechanizmem, który sprawia
wrażenie obrony. Wzorzec nie łapie m.in. `onfocus=`, `onmouseenter=`, `<svg onload\n=…>` (znak nowej linii
przed `=`), `<iframe srcdoc=…>`, `<object data=…>`, `<a href="&#106;avascript:…">` (encje HTML) ani
wariantów z separatorami. Każdy z nich przechodzi walidację i trafia do `Html.Raw`.

Kluczowe: **vendor i store manager to konta o niższym poziomie zaufania niż główny administrator**.
Sklep multi-vendor z założenia wpuszcza obcych ludzi do edytora opisu produktu. Wstrzyknięty skrypt
wykonuje się w kontekście storefrontu (kradzież sesji klienta, przechwycenie danych z formularza płatności)
oraz w podglądzie produktu w panelu administratora (eskalacja do pełnych uprawnień).

#### Ryzyko

- **Bezpieczeństwo:** stored XSS → przejęcie sesji administratora → pełna kompromitacja instalacji.
- **Użytkownicy:** kradzież danych klientów, potencjalnie danych płatniczych.
- **Dane:** manipulacja treścią widzianą przez innych użytkowników zaplecza.
- **Utrzymanie:** dopisywanie kolejnych wzorców do regexa to bieg za własnym ogonem.

#### Rekomendacja

1. **Zastąpić `NoScriptsAttribute` sanityzacją na białej liście** przy zapisie, opartą o dojrzałą
   bibliotekę (`HtmlSanitizer` / `Ganss.Xss`). Biała lista tagów i atrybutów, konfigurowalna przez
   ustawienie — sklepy potrzebują `<table>`, `<img>`, `<iframe>` do YouTube; sanityzator na to pozwala
   w kontrolowany sposób, regex nie.
2. **Sanityzować przy zapisie, nie przy odczycie** — jedno miejsce, jednorazowy koszt, istniejąca treść
   naprawiana migracją.
3. **Dodać CSP jako drugą warstwę.** `UseDefaultSecurityHeaders` ustawia już
   `object-src 'none'; form-action 'self'; frame-ancestors 'none'`, ale nie `script-src` — a to jest ta
   dyrektywa, która ogranicza skutki XSS.
4. **Zachować `Html.Raw`** — treść ma być bogatym HTML-em i to jest poprawna decyzja. Naprawiamy wejście,
   nie wyjście.

#### Koszt

**M**

#### Breaking Change

**Częściowo.** Sanityzacja odrzuci konstrukcje HTML, które dotąd przechodziły. Migracja istniejących
treści powinna raportować, co zostało usunięte, zamiast robić to po cichu.

---

### SEC-002 – Domyślna konfiguracja bezpieczeństwa jest wyłączona

**Priorytet:** P1
**Kategoria:** Security
**Dotyczy:** `src/Web/Grand.Web/App_Data/appsettings.json`, sekcja `Security`

#### Problem

Wszystkie mechanizmy bezpieczeństwa transportu i ciasteczek są w dostarczanej konfiguracji wyłączone.
Instalacja „z pudełka" postawiona na produkcji jest niezabezpieczona.

#### Dowody

`src/Web/Grand.Web/App_Data/appsettings.json`:

```jsonc
"UseDefaultSecurityHeaders": false,   // brak X-Content-Type-Options, X-Frame-Options, CSP, Referrer-Policy
"UseHsts": false,
"UseHttpsRedirection": false,
"CookieSecurePolicyAlways": false,    // ciasteczko sesji bez flagi Secure
"CookieSameSite": "Lax",
"ForceUseHTTPS": false,
"AllowedHosts": "*"
```

Nagłówki są zaimplementowane (`GrandCommonStartup.Configure` → `application.UseDefaultSecurityHeaders()`,
pakiet `NetEscapades.AspNetCore.SecurityHeaders` w `Directory.Packages.props`) — po prostu domyślnie
nieaktywne.

Dla kontrastu: `ApiSecurityStartup` **przerywa start** aplikacji poza Development, gdy klucz JWT jest
słaby. Ten sam wzorzec nie został zastosowany do reszty ustawień bezpieczeństwa.

#### Dlaczego to jest problem

Domyślne wartości są tym, z czym większość instalacji zostanie na zawsze. Ustawienia są dobrze
udokumentowane komentarzami w `appsettings.json`, ale dokumentacja nie chroni instalacji, w której nikt
nie przeczytał komentarza.

Powód takich domyślnych wartości jest zrozumiały: `UseHttpsRedirection` na maszynie deweloperskiej bez
certyfikatu psuje uruchomienie. To jest prawdziwy problem — ale rozwiązaniem jest różnicowanie po
środowisku, nie wyłączanie wszędzie.

#### Ryzyko

- **Bezpieczeństwo:** przechwycenie ciasteczka sesji przez HTTP, clickjacking, MIME sniffing, brak CSP.
- **Użytkownicy:** przejęcie konta klienta w sieci publicznej.
- **Utrzymanie:** brak sygnału, że konfiguracja jest niebezpieczna — cisza aż do incydentu.

#### Rekomendacja

1. **Odwrócić domyślne wartości** dla `UseDefaultSecurityHeaders`, `UseHsts`, `UseHttpsRedirection`
   i `CookieSecurePolicyAlways` na `true`, a w `appsettings.Development.json` ustawić je na `false`.
2. **Dodać startup ostrzegający** (wzorowany na `ApiSecurityStartup`): poza Development logować
   `LogWarning` dla każdego wyłączonego mechanizmu, z nazwą klucza konfiguracyjnego. Nie przerywać
   startu — to zablokowałoby legalne wdrożenia za terminatorem TLS.
3. **`AllowedHosts: "*"`** zostawić — filtrowanie hostów przy odwrotnym proxy jest zwykle redundantne,
   a zła wartość psuje wdrożenie w sposób trudny do zdiagnozowania. Wystarczy ostrzeżenie.

#### Koszt

**S**

#### Breaking Change

**Częściowo.** Instalacje aktualizujące `appsettings.json` z repo dostaną włączone przekierowanie na
HTTPS — za proxy bez `UseForwardedHeaders` to może dać pętlę przekierowań. Wymaga wyraźnej noty
w release notes.

---

### SEC-003 – Izolacja vendora i sklepu realizowana ręcznie w kontrolerach

**Priorytet:** P1
**Kategoria:** Security
**Dotyczy:** `Grand.Web.Vendor`, `Grand.Web.Store` — wszystkie kontrolery

#### Problem

Nie istnieje scentralizowany mechanizm egzekwowania zakresu danych najemcy. Każda akcja kontrolera sama
sprawdza, czy encja należy do bieżącego vendora lub sklepu. Pominięcie sprawdzenia w jednej akcji daje IDOR.

#### Dowody

W `src/Web/Grand.Web.Vendor` występuje **88 odwołań do `CurrentVendor`**, w większości jako ręczne
porównanie. Sam `ProductController` (2584 linie):

```
:86    return product.VendorId != _contextAccessor.WorkContext.CurrentVendor.Id
:166   if (product == null || product.VendorId != _contextAccessor.WorkContext.CurrentVendor.Id)
:194   if (product == null || product.VendorId != _contextAccessor.WorkContext.CurrentVendor.Id)
:231   if (product == null || product.VendorId != _contextAccessor.WorkContext.CurrentVendor.Id)
:265   if (originalProduct == null || originalProduct.VendorId != ...)
:326   if (products[i].VendorId != _contextAccessor.WorkContext.CurrentVendor.Id) continue;
:1006  if (associatedProduct == null || associatedProduct.VendorId != ...)
:1025  if (product == null || product.VendorId != ...)
:1105  if (product.VendorId != _contextAccessor.WorkContext.CurrentVendor.Id)
```

Analogicznie w `Grand.Web.Store` — sprawdzenia oparte o `CurrentCustomer.StaffStoreId` i
`entity.Stores.Contains(...)` powtarzane w każdym kontrolerze (`BlogController.cs:133`,
`BrandController.cs:138`, `CategoryController.cs:118`, `CheckoutAttributeController.cs:76` i dalej).

`AuthorizeVendorAttribute` weryfikuje **tylko** to, że użytkownik jest aktywnym vendorem — nie ma związku
z konkretnymi rekordami. `AclService.Authorize<T>(entity, storeId)` istnieje i jest poprawny, ale jego
wywołanie zależy od pamięci autora kontrolera.

#### Dlaczego to jest problem

To architektura, w której bezpieczeństwo najemcy zależy od dyscypliny przy pisaniu każdej z ~120 akcji
panelu vendora i ~250 panelu sklepu. Przy takiej liczbie miejsc pominięcia są pewne, nie prawdopodobne.
Historia repozytorium to potwierdza — commity #767 („make the store scope explicit on the category, brand
and collection lists") i #773 („Scope CMS lookups to the store").

#### Ryzyko

- **Bezpieczeństwo:** IDOR — vendor odczytuje lub modyfikuje produkty, zamówienia i dane klientów innego vendora.
- **Dane:** modyfikacja cudzych rekordów bez śladu.
- **Użytkownicy:** wyciek danych osobowych klientów między najemcami (naruszenie RODO).
- **Contributorzy:** nowa akcja napisana przez kontrybutora domyślnie **nie** jest zabezpieczona.

#### Rekomendacja

Odwrócić domyślną wartość: zakres ma być egzekwowany, chyba że akcja jawnie z niego rezygnuje.

1. **`IDataScopeProvider`** zwracający filtr (`Expression<Func<T,bool>>`) dla bieżącego kontekstu —
   global, store, vendor. Wstrzykiwany do serwisów widoku, nie do kontrolerów.
2. **Filtr autoryzacyjny `[ScopedResource]`** wiążący parametr trasy z encją i weryfikujący zakres przed
   wejściem do akcji. Akcje bez zakresu deklarują to jawnie (`[ScopedResource(Ignore = true)]`) — wzorzec
   już stosowany przez `AuthorizeVendorAttribute(bool ignore)`.
3. **Test architektoniczny** (TEST-003) wymuszający, że każda publiczna akcja w `Grand.Web.Vendor`
   i `Grand.Web.Store` ma atrybut zakresu albo jawne wyłączenie. To jedyny sposób, żeby ta reguła
   przetrwała kolejny rok.

Zadanie jest bezpośrednio powiązane z ARCH-001 — konsolidacja kontrolerów jest naturalnym momentem na
wprowadzenie strategii zakresu, bo to właśnie zakres jest główną różnicą między panelami.

#### Koszt

**L**

#### Breaking Change

**Nie** dla API publicznego. Zmienia wewnętrzną strukturę kontrolerów paneli.

---

### ARCH-002 – Grand.Business.Core jest wspólnym kontraktem wszystkich domen

**Priorytet:** P2
**Kategoria:** Architecture
**Dotyczy:** `Grand.Business.Core`

#### Problem

Wszystkie interfejsy, komendy, zapytania, zdarzenia i DTO dziewięciu modułów biznesowych leżą w jednym
projekcie, który każdy moduł referencuje w całości.

#### Dowody

`src/Business/Grand.Business.Core` — 368 plików `.cs`, katalogi najwyższego poziomu: `Interfaces`,
`Commands`, `Queries`, `Events`, `Dto`, `Enums`, `Extensions`, `Utilities`. Podkatalogi obejmują
`Catalog`, `Checkout`, `Cms`, `Common`, `Customers`, `Marketing`, `Messages`, `Storage`, `System`,
`ExportImport`, `Authentication`.

Wszystkie dziewięć projektów `Grand.Business.*` referencuje `Grand.Business.Core.csproj` — i **nic więcej**
z warstwy biznesowej. Ta część jest dobra: moduły faktycznie się nie widzą.

#### Dlaczego to jest problem

Granica między modułami jest wyegzekwowana, ale granica **kontraktów** nie. Zmiana `ICategoryService`
przekompilowuje `Grand.Business.Checkout`, `Grand.Business.Messages` i wszystko inne. Kompilator nie
odróżnia „Checkout używa kontraktów Catalogu" od „Checkout mógłby użyć kontraktów Catalogu" — jedno
i drugie wygląda identycznie w grafie referencji.

Praktyczne skutki: pełny rebuild przy każdej zmianie kontraktu, brak informacji o rzeczywistych
zależnościach między domenami, brak możliwości niezależnego wersjonowania kontraktu jednej domeny.

**To nie jest pilny problem.** Obecny układ działa i jest zrozumiały.

#### Ryzyko

- **Rozwój:** czas budowania rośnie z rozmiarem kontraktu; przy 368 plikach to jeszcze nie boli.
- **Utrzymanie:** brak sygnału o niezamierzonym sprzężeniu między domenami.
- **Contributorzy:** trudność w ocenie zasięgu zmiany kontraktu.

#### Rekomendacja

**Nie rozbijać projektu.** Zamiast tego wyegzekwować i udokumentować zależności testem architektonicznym:
zadeklarowana macierz dozwolonych zależności między domenami (np. Checkout → Catalog: tak,
Catalog → Checkout: nie), weryfikowana w CI przez analizę `using` w plikach źródłowych modułów.

Rozbicie kontraktów rozważyć dopiero, gdy `Grand.Business.Core` przekroczy ~800 plików albo gdy pojawi
się realna potrzeba wersjonowania kontraktu jednej domeny — nie wcześniej. To przypadek, w którym obecne
rozwiązanie jest **wystarczająco dobre** (patrz sekcja 15).

#### Koszt

**S** (test architektoniczny) / **XL** (faktyczne rozbicie — niezalecane)

#### Breaking Change

**Nie** dla testu architektonicznego. **Tak** dla rozbicia.

---

### ARCH-003 – Wyłączona walidacja kontenera DI i budowanie tymczasowych kontenerów

**Priorytet:** P2
**Kategoria:** Architecture / Reliability
**Dotyczy:** wszystkie `Program.cs`, `PluginManager`, `ModuleLoader`, `Grand.Web.Common/Startup/StartupApplication`

#### Problem

Każdy host wyłącza walidację zakresów i walidację przy budowaniu kontenera. Równolegle w kilku miejscach
budowane są tymczasowe kontenery `BuildServiceProvider()` w trakcie konfiguracji.

#### Dowody

`src/Web/Grand.Web/Program.cs`, `Grand.Web.Admin/Program.cs`, `Grand.Web.Store/Program.cs`,
`Grand.Web.Vendor/Program.cs` — identycznie:

```csharp
builder.Host.UseDefaultServiceProvider((_, options) =>
{
    options.ValidateScopes = false;
    options.ValidateOnBuild = false;
});
```

`BuildServiceProvider()` w konfiguracji:

- `src/Core/Grand.Infrastructure/Modules/ModuleLoader.cs:108`
- `src/Core/Grand.Infrastructure/Plugins/PluginManager.cs:58`
- `src/Modules/Grand.Module.Api/Infrastructure/OpenApiStartup.cs:36,37,38` (trzy osobne kontenery)
- `src/Web/Grand.Web.Common/Startup/StartupApplication.cs:127`

#### Dlaczego to jest problem

`ValidateScopes = false` wyłącza wykrywanie captive dependencies — singletonu trzymającego referencję do
usługi scoped. Taki błąd nie powoduje wyjątku; powoduje, że jedna instancja usługi scoped żyje przez całe
życie procesu i obsługuje żądania wszystkich użytkowników. W systemie, w którym `IWorkContext` niesie
tożsamość zalogowanego klienta, to jest potencjalny wyciek danych między sesjami, nie tylko problem
wydajnościowy.

`ValidateOnBuild = false` przesuwa błędy rejestracji ze startu aplikacji na pierwsze żądanie do konkretnej
akcji — czyli często na produkcję.

`BuildServiceProvider()` w trakcie konfiguracji tworzy **drugi, niezależny kontener**. Każdy singleton
zarejestrowany do tego momentu zostaje w nim utworzony po raz drugi i nigdy nie zwolniony. W `OpenApiStartup`
dzieje się to trzy razy w trzech kolejnych linijkach.

Uczciwie: wyłączenie walidacji jest tu **zrozumiałe**. GrandNode ładuje wtyczki dynamicznie, a wtyczka
z niepoprawną rejestracją wywaliłaby cały start przy `ValidateOnBuild = true`. To realny kompromis,
nie zaniedbanie — ale kompromis, który powinien być świadomy i różnicowany po środowisku.

#### Ryzyko

- **Bezpieczeństwo:** captive dependency na `IWorkContext`/`IStoreContext` = wyciek danych między użytkownikami.
- **Reliability:** błędy rejestracji ujawniają się w produkcji zamiast na starcie.
- **Wydajność:** zduplikowane singletony, potencjalnie `IConnectionMultiplexer` i `IMongoClient`.
- **Utrzymanie:** brak automatycznej weryfikacji grafu przy 76 projektach i 16 wtyczkach.

#### Rekomendacja

1. **Włączyć `ValidateScopes` i `ValidateOnBuild` w środowisku Development.** Deweloper dostaje błąd
   natychmiast, produkcja zachowuje odporność na wadliwą wtyczkę.
2. **Dodać test integracyjny** budujący pełny kontener z walidacją i rozwiązujący każdą zarejestrowaną
   usługę. To właściwe miejsce na tę weryfikację — CI, nie runtime produkcyjny.
3. **Usunąć `BuildServiceProvider()`** tam, gdzie się da: w `OpenApiStartup` konfiguracje można wiązać
   bezpośrednio z `IConfiguration` (są to POCO wiązane w `RegisterConfigurations`); w `PluginManager`
   i `ModuleLoader` przekazać `ILoggerFactory` utworzoną jednorazowo (`LoggerFactory.Create(...)`),
   zamiast budować kontener dla jednego loggera.

#### Koszt

**M**

#### Breaking Change

**Nie.**

---

### REL-001 – Scheduler in-process potrafi trwale zatrzymać zadanie

**Priorytet:** P2
**Kategoria:** Reliability
**Dotyczy:** `Grand.Web.Common` / `BackgroundServiceTask` / `ExecuteAsync`

#### Problem

Pętla zadania cyklicznego wychodzi przez `break` w sytuacjach odwracalnych — wtedy zadanie nie wznowi
się aż do restartu procesu. Dodatkowo zewnętrzny `catch (Exception)` połyka każdy błąd bez logowania.

#### Dowody

`src/Web/Grand.Web.Common/Infrastructure/BackgroundServiceTask.cs`:

```csharp
if (task.Enabled && (string.IsNullOrEmpty(task.LeasedByMachineName) ||
                     machineName == task.LeasedByMachineName))
{
    ... // wykonanie zadania
    await Task.Delay(TimeSpan.FromMinutes(timeInterval), stoppingToken);
}
else
{
    break;          // zadanie wyłączone lub przypisane do innej maszyny => koniec, na zawsze
}
```

oraz:

```csharp
catch (Exception)
{
    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);   // brak logowania
}
```

i wcześniej:

```csharp
if (task == null)
{
    logger.LogInformation("Task {TaskName} is not exists in the database", Name);
    break;          // zadanie jeszcze niezaseedowane => koniec, na zawsze
}
```

#### Dlaczego to jest problem

Trzy realne scenariusze:

1. Administrator wyłącza zadanie w panelu, po godzinie włącza z powrotem — zadanie nie ruszy, bo pętla
   zakończyła się przy pierwszym sprawdzeniu. Wymaga restartu, o czym nikt nie wie.
2. `LeasedByMachineName` wskazuje maszynę, która została zdjęta — zadanie umiera na wszystkich pozostałych
   instancjach i nie wykona się już nigdy.
3. Wyjątek w `GetTaskByName` (np. chwilowa niedostępność bazy) jest połykany bez śladu w logach; przy
   powtarzalnym błędzie zadanie pozornie działa, a w rzeczywistości kręci się w pętli co minutę.

Dla `QueuedMessagesSendScheduleTask` skutkiem są niewysłane maile potwierdzające zamówienie — cicho,
bez alertu.

Co jest tu zrobione **dobrze**: `TryClaimTaskRun` z `InstanceId` to poprawne, atomowe przejęcie
uruchomienia przy wielu instancjach, a `OperationCanceledException` przy zamykaniu aplikacji jest jawnie
odróżniony od błędu zadania. Problem dotyczy sterowania pętlą, nie modelu współbieżności.

#### Ryzyko

- **Reliability:** ciche zatrzymanie zadań krytycznych (maile, anulowanie zamówień, kursy walut, sitemap).
- **Użytkownicy:** brak potwierdzenia zamówienia.
- **Utrzymanie:** brak logu = brak diagnozy; problem wykrywany przez reklamację klienta.
- **Rozwój:** granularność minutowa uniemożliwia zadania częstsze niż raz na minutę i harmonogramy typu
  cron („codziennie o 3:00").

#### Rekomendacja

1. **Zastąpić `break` ponowieniem sprawdzenia po interwale.** Zadanie wyłączone lub wydzierżawione innej
   maszynie ma spać i sprawdzić ponownie, a nie kończyć pętlę.
2. **Logować w zewnętrznym `catch`** (`LogError` z wyjątkiem) i zastosować backoff wykładniczy zamiast
   stałej minuty.
3. **Rozważyć wyrażenia cron** zamiast `TimeInterval` w minutach — funkcjonalność, o którą pytają
   użytkownicy (raporty nocne), nie do wyrażenia obecnym modelem. **Nice to have.**
4. **Rozważyć oddzielny host workera** — obecnie zadania działają w każdym procesie webowym, co miesza
   profil obciążenia. `Aspire.AppHost` już rozdziela procesy, więc infrastruktura na to jest.
   **Nice to have.**

#### Koszt

**S** (1–2) / **M** (3) / **L** (4)

#### Breaking Change

**Nie** dla punktów 1–2. Punkt 3 zmienia schemat encji `ScheduleTask` — wymaga migracji.

---

### PLG-001 – Brak izolacji wtyczek i sztywne dopasowanie wersji

**Priorytet:** P2
**Kategoria:** Architecture / Plugin
**Dotyczy:** `Grand.Infrastructure.Plugins` / `PluginManager`, `PluginVersionResolver`, `GrandVersion`

#### Problem

Wtyczki są ładowane do domyślnego `AssemblyLoadContext` procesu, bez izolacji i bez możliwości wyładowania.
Zgodność wersji sprowadza się do porównania stringów `Major.Minor` na dokładną równość.

#### Dowody

`src/Core/Grand.Infrastructure/Plugins/PluginManager.cs:247`:

```csharp
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(plug.FullName);
```

Bramka wersji, `PluginManager.cs:98`:

```csharp
if (plugin.SupportedVersion != GrandVersion.SupportedPluginVersion)
{
    _logger.LogInformation("Incompatible plugin {PluginSystemName}", plugin.SystemName);
    referencedPlugins.Add(plugin);
    continue;
}
```

gdzie `GrandVersion.SupportedPluginVersion = $"{MajorVersion}.{MinorVersion}"`
(`src/Core/Grand.Infrastructure/GrandVersion.cs`).

Dla porównania — moduły **mają** własny kontekst: `ModuleLoader.ModuleLoadContext : AssemblyLoadContext`
z `isCollectible: true` (`src/Core/Grand.Infrastructure/Modules/ModuleLoader.cs:126-132`). Wtyczki nie.

`src/Plugins/Theme.Modern/Theme.Modern.csproj` referencuje `Grand.Web.csproj` — motyw zależy od całej
aplikacji storefrontu.

#### Dlaczego to jest problem

**Brak izolacji:** dwie wtyczki wymagające różnych wersji tej samej biblioteki są nie do pogodzenia —
wygrywa ta załadowana pierwsza, druga rzuca `TypeLoadException` w losowym momencie działania. Wtyczki nie
można też wyładować, więc jej odinstalowanie wymaga `applicationLifetime.StopApplication()` (i tak właśnie
robi `PluginController.UploadPlugin`).

**Sztywna wersja:** wydanie 2.5.0 unieważnia **wszystkie** istniejące wtyczki, nawet jeśli żaden kontrakt
się nie zmienił. Autor wtyczki musi przebudować i wydać ją przy każdym minorze. Dla ekosystemu wtyczek
zewnętrznych to bariera wejścia — a ekosystem wtyczek jest jedną z głównych wartości GrandNode.

**Brak zdefiniowanego API:** kontraktem jest faktycznie wszystko publiczne w `Grand.Business.Core`,
`Grand.Infrastructure` i `Grand.Web.Common`. Nie ma sposobu, żeby zmienić cokolwiek wewnętrznego bez
ryzyka złamania wtyczki, ani żeby powiedzieć autorowi wtyczki, na czym może polegać.

#### Ryzyko

- **Bezpieczeństwo:** wtyczka działa z pełnymi uprawnieniami procesu, ma dostęp do `IRepository<T>`
  wszystkich encji, do konfiguracji i do systemu plików. Nie ma granicy zaufania.
- **Reliability:** konflikt zależności ujawnia się losowo w runtime.
- **Rozwój:** każdy minor wymusza rewizję wszystkich wtyczek.
- **Contributorzy:** wysoka bariera dla autorów wtyczek zewnętrznych.
- **Utrzymanie:** brak jawnego API oznacza, że każda zmiana wewnętrzna to potencjalny breaking change.

#### Rekomendacja

W kolejności rosnącego kosztu:

1. **Zakres wersji zamiast dokładnej równości** (koszt S, zysk duży). `PluginInfoAttribute` dostaje
   `MinSupportedVersion`/`MaxSupportedVersion`; brak deklaracji zachowuje obecne zachowanie.
2. **Jawny kontrakt wtyczki** (koszt M) — udokumentowana lista typów i namespace'ów stanowiących publiczne
   API, plus test architektoniczny pilnujący, że wtyczki referencyjne w `src/Plugins/` nie sięgają poza nią.
   Bez tego punkt 1 jest obietnicą bez pokrycia.
3. **Izolacja w `AssemblyLoadContext`** (koszt L). Wzorzec jest już w repo — `ModuleLoader`. Trudność:
   typy współdzielone (`IPlugin`, encje domenowe) muszą pochodzić z kontekstu hosta, inaczej DI nie zadziała.
   Wymaga `AssemblyDependencyResolver` i jawnej listy assembly delegowanych do hosta. **Wykonalne, ale nie pilne.**
4. **Wyładowywanie wtyczek bez restartu** — pochodna punktu 3. **Nice to have**; restart procesu przy
   instalacji wtyczki jest akceptowalnym kompromisem.

#### Koszt

**S** (1) / **M** (2) / **L** (3) / **XL** (4)

#### Breaking Change

**Nie** dla punktów 1–2 (rozszerzenie, zgodność wsteczna zachowana). **Częściowo** dla punktu 3 —
wtyczki polegające na współdzieleniu statycznego stanu z hostem przestaną działać.

---

### SEC-004 – Upload plików wczytuje całe ciało do pamięci przed sprawdzeniem limitu

**Priorytet:** P2
**Kategoria:** Security / Performance
**Dotyczy:** `Grand.Web` / `ContactController.UploadFileContactAttribute`, `ProductController.UploadFileProductAttribute`, `ShoppingCartController.UploadFileCheckoutAttribute`

#### Problem

Endpointy uploadu dostępne z publicznego storefrontu materializują cały plik w tablicy bajtów, a dopiero
potem sprawdzają limit rozmiaru. Gdy atrybut nie ma skonfigurowanej listy rozszerzeń, akceptowane jest
dowolne rozszerzenie.

#### Dowody

`src/Web/Grand.Web/Controllers/ContactController.cs:138-167` (wzorzec powtórzony w dwóch pozostałych
kontrolerach):

```csharp
if (!string.IsNullOrEmpty(attribute.ValidationFileAllowedExtensions))
{
    var allowedFileExtensions = attribute.ValidationFileAllowedExtensions.Split(...);
    if (!allowedFileExtensions.IsAllowedMediaFileType(fileExtension))
        return Json(new { success = false, ... });
}
// pusta lista => brak jakiejkolwiek walidacji rozszerzenia

var fileBinary = file.GetDownloadBits();   // CAŁY plik do byte[]

if (attribute.ValidationFileMaximumSize.HasValue)
{
    var maxFileSizeBytes = attribute.ValidationFileMaximumSize.Value * 1024;
    if (fileBinary.Length > maxFileSizeBytes)   // limit sprawdzany PO wczytaniu
        return Json(new { success = false, ... });
}
```

`GetDownloadBits` (`src/Web/Grand.Web.Common/Extensions/StorageExtensions.cs:15`) kopiuje strumień
do `byte[]`.

`IsAllowedMediaFileType` (`src/Core/Grand.SharedKernel/Extensions/AllowedFileExtensions.cs:11-14`)
to zwykłe porównanie stringów rozszerzenia — brak weryfikacji rzeczywistej zawartości (magic bytes)
i brak weryfikacji `ContentType` (zmienna `contentType` w `ContactController` jest odczytana, ale nieużywana).

`MaxRequestBodySize` w `appsettings.json` jest `null` — obowiązuje domyślny limit Kestrela 30 MB, ale tylko on.

Atrybut `[DenySystemAccount]` jest obecny, natomiast **nie ma ograniczenia częstotliwości** ani wymogu
uwierzytelnienia — formularz kontaktowy jest dostępny dla gościa.

#### Dlaczego to jest problem

Nieuwierzytelniony użytkownik może wysyłać pliki po ~30 MB, z których każdy jest w całości alokowany
na Large Object Heap zanim zostanie odrzucony. Kilkadziesiąt równoległych żądań to wyczerpanie pamięci
procesu — tania odmowa usługi bez uwierzytelnienia.

Druga część: pusta lista rozszerzeń oznacza „wszystko dozwolone". Plik nie jest wykonywany po stronie
serwera (trafia do `Download` w bazie), więc nie jest to RCE — ale jest to składowanie dowolnej zawartości
i, przy późniejszym pobraniu z niewłaściwym `Content-Type`, wektor XSS przez plik `.html`/`.svg`.

#### Ryzyko

- **Bezpieczeństwo:** DoS pamięciowy bez uwierzytelnienia; składowanie dowolnych treści.
- **Wydajność:** alokacje na LOH, presja na GC.
- **Użytkownicy:** niedostępność sklepu.
- **Dane:** zaśmiecanie kolekcji `Download`.

#### Rekomendacja

1. **Sprawdzać `file.Length` przed odczytem** — jest znane z nagłówków multipart, nie wymaga materializacji.
2. **Twardy limit globalny** niezależny od konfiguracji atrybutu (np. 10 MB), plus dolna granica na
   `ValidationFileMaximumSize`.
3. **Domyślna biała lista rozszerzeń**, gdy `ValidationFileAllowedExtensions` jest puste — obecne
   zachowanie („brak konfiguracji = wszystko wolno") jest odwrotne do bezpiecznego.
4. **Rate limiting** na endpointach uploadu dostępnych bez uwierzytelnienia (`RateLimiter` z .NET 7+
   jest w standardzie, nie wymaga nowej zależności).
5. **Weryfikacja sygnatury pliku** (magic bytes) dla typów obrazów. **Nice to have** — punkty 1–4
   zamykają realne ryzyko.

#### Koszt

**S**

#### Breaking Change

**Częściowo.** Domyślna biała lista odrzuci uploady, które wcześniej przechodziły w instalacjach
z niewypełnioną konfiguracją atrybutu.

---

### OBS-001 – Obserwowalność ograniczona do szkieletu

**Priorytet:** P2
**Kategoria:** Observability
**Dotyczy:** `Grand.Web.Common` / `ServiceCollectionExtensions.AddGrandHealthChecks`, `ContextLoggingMiddleware`, cała warstwa biznesowa

#### Problem

Infrastruktura telemetrii jest skonfigurowana, ale nie jest używana. Health check nie sprawdza żadnej
zależności. Nie ma metryk domenowych ani własnych span'ów.

#### Dowody

`src/Web/Grand.Web.Common/Infrastructure/ServiceCollectionExtensions.cs:267-271`:

```csharp
public static void AddGrandHealthChecks(this IServiceCollection services)
{
    var hcBuilder = services.AddHealthChecks();
    hcBuilder.AddCheck("self", () => HealthCheckResult.Healthy());
}
```

`src/Web/Grand.Web.Common/Infrastructure/ApplicationBuilderExtensions.cs:196-198` — wystawiony jest
wyłącznie `/health/live`, brak `/health/ready`.

Wyszukanie `ActivitySource` i `Meter(` w całym `src/` poza `Aspire.ServiceDefaults`: **zero wyników**.
Żadnego własnego span'a, żadnej metryki domenowej — brak licznika zamówień, płatności, porzuconych
koszyków, nieudanych logowań.

`Aspire.ServiceDefaults` konfiguruje przy tym poprawnie `AddAspNetCoreInstrumentation`,
`AddHttpClientInstrumentation`, `AddRuntimeInstrumentation` oraz eksport OTLP i Azure Monitor — szkielet
jest dobry, po prostu nikt na nim nie budował.

#### Dlaczego to jest problem

Health check zwracający zawsze `Healthy` jest gorszy niż jego brak: orkiestrator uzna instancję za sprawną,
gdy MongoDB jest nieosiągalne, i będzie kierował do niej ruch. Brak `/health/ready` oznacza, że instancja
zaczyna przyjmować żądania zanim będzie gotowa.

Brak metryk domenowych oznacza, że pytanie „czy po wczorajszym wdrożeniu spadła konwersja checkoutu"
nie ma odpowiedzi w telemetrii — trzeba pytać bazę danych.

#### Ryzyko

- **Reliability:** ruch kierowany do niesprawnych instancji; brak automatycznego restartu.
- **Utrzymanie:** MTTR liczony w godzinach zamiast minut.
- **Rozwój:** brak danych do weryfikacji efektu optymalizacji — co czyni całą fazę 7 tej roadmapy niemierzalną.
- **Użytkownicy:** dłuższe awarie.

#### Rekomendacja

1. **Prawdziwe health checks:** `/health/live` (proces żyje — obecne zachowanie jest tu poprawne)
   i `/health/ready` sprawdzające MongoDB (`ping`), Redis (gdy `RedisPubSubEnabled`) oraz stan instalacji
   bazy. Krótkie timeouty, żeby check sam nie stał się problemem.
2. **`ActivitySource` w kluczowych ścieżkach:** `PlaceOrder`, przetwarzanie płatności, wyszukiwanie
   w katalogu. Span z atrybutami `order.code`, `store.id`, `payment.method` — **nigdy** z danymi osobowymi.
3. **`Meter` z licznikami domenowymi:** złożone zamówienia, nieudane płatności (z wymiarem metody
   płatności), nieudane logowania, trafienia/pudła cache, czas wykonania zadań cyklicznych.
4. **Naprawić PII w telemetrii** — patrz SEC-005.

#### Koszt

**M**

#### Breaking Change

**Nie.**

---

### SEC-005 – Dane osobowe w atrybutach telemetrii

**Priorytet:** P2
**Kategoria:** Security / Compliance
**Dotyczy:** `Grand.Web.Common` / `ContextLoggingMiddleware` / `InvokeAsync`

#### Problem

Adres e-mail zalogowanego klienta jest dodawany jako tag do bieżącego `Activity`, czyli trafia do każdego
eksportowanego trace'a.

#### Dowody

`src/Web/Grand.Web.Common/Middleware/ContextLoggingMiddleware.cs`:

```csharp
activity.AddTag(CustomerPropertyName, workContext?.CurrentCustomer?.Email);
activity.AddTag(StorePropertyName, storeContext?.CurrentStore?.Name);
activity.AddTag(CurrencyPropertyName, workContext?.WorkingCurrency?.Name);
activity.AddTag(LanguagePropertyName, workContext?.WorkingLanguage?.Name);
```

Middleware jest domyślnie włączony — `"EnableContextLoggingMiddleware": true` w `appsettings.json`.
Eksport idzie do OTLP i/lub Azure Monitor (`Aspire.ServiceDefaults`), czyli poza granicę aplikacji.

#### Dlaczego to jest problem

Trace'y są przechowywane w systemach obserwowalności o innej polityce retencji, innej kontroli dostępu
i innym zakresie geograficznym niż baza aplikacji. Wyeksportowanie adresu e-mail do zewnętrznego APM to
przetwarzanie danych osobowych poza zadeklarowanym celem — trudne do obrony pod RODO i praktycznie
nieodwracalne (nie da się zrealizować prawa do usunięcia w retencji APM).

Dodatkowo: inżynier diagnozujący incydent dostaje dostęp do adresów e-mail wszystkich klientów, mimo że
do diagnozy wystarczy stabilny identyfikator.

#### Ryzyko

- **Bezpieczeństwo:** rozszerzenie powierzchni wycieku danych osobowych na systemy trzecie.
- **Compliance:** naruszenie zasady minimalizacji danych; niewykonalne żądanie usunięcia.
- **Utrzymanie:** dostęp do PII dla ról, które go nie potrzebują.

#### Rekomendacja

Zamienić `Email` na `CurrentCustomer.Id` (identyfikator nieujawniający tożsamości, wystarczający do
korelacji i do wyszukania klienta w bazie przez uprawnioną osobę). Nazwę sklepu zamienić na `Store.Id`
dla spójności. Ustawienie `EnableContextLoggingMiddleware` zostawić — po naprawie tagi są bezpieczne
i przydatne.

#### Koszt

**S**

#### Breaking Change

**Nie** (zmienia się zawartość telemetrii, nie kontrakt kodu). Dashboardy filtrujące po `Customer` = e-mail
przestaną działać — do odnotowania w release notes.

---

### TEST-001 – Brak testów integracyjnych i architektonicznych

**Priorytet:** P1
**Kategoria:** Testing
**Dotyczy:** `src/Tests/**`, `azure-pipelines.yml`, `.github/workflows/aspnetcore.yml`

#### Problem

1826 testów to niemal wyłącznie testy jednostkowe na mockach. Nie ma testów wykonujących rzeczywiste
zapytania do MongoDB, nie ma testów przechodzących przez pipeline HTTP, nie ma testów pilnujących reguł
architektonicznych.

#### Dowody

Rozkład testów (`[TestMethod]`):

| Projekt | Testy |
|---|---:|
| Grand.Business.Catalog.Tests | 354 |
| Grand.Mapping.Tests | 238 |
| Grand.Business.Checkout.Tests | 231 |
| Grand.Business.Common.Tests | 129 |
| Grand.Business.Marketing.Tests | 126 |
| Grand.Business.Customers.Tests | 117 |
| Grand.Infrastructure.Tests | 94 |
| Grand.Business.Cms.Tests | 92 |
| Grand.Data.Tests | 67 |
| **Grand.Web.Admin.Tests** | **59** |
| **Grand.Web.Store.Tests** | **17** |
| **Grand.Web.Common.Tests** | **15** |
| **Grand.Web.Tests** | **9** |

Cała warstwa webowa — 1424 pliki `.cs` i 1294 widoki `.cshtml` w pięciu projektach — ma **100 testów**.

CI **uruchamia kontener MongoDB** (`azure-pipelines.yml` sekcja `services: mongo`,
`.github/workflows/aspnetcore.yml` krok `docker run -d -p 27017:27017 mongo`) — infrastruktura jest,
ale poza `Grand.Data.Tests` nikt z niej nie korzysta.

Brak `NetArchTest`, `ArchUnitNET` czy odpowiednika w `Directory.Packages.props`.
Brak `Microsoft.AspNetCore.Mvc.Testing` — czyli brak `WebApplicationFactory`.

Znany problem: testy `Customers`/`Marketing`/`Messages` zawodzą losowo przy równoległym uruchomieniu
całego rozwiązania, przechodzą osobno — współdzielą stan statyczny.

#### Dlaczego to jest problem

Testy na mockach weryfikują, że kod wywołuje to, co autor testu założył, że powinien wywołać. Nie
weryfikują, że zapytanie LINQ przekłada się na poprawną agregację MongoDB, że filtr autoryzacji faktycznie
odrzuca żądanie, ani że zakres vendora jest egzekwowany.

To jest **problem blokujący**, nie kosmetyczny. ARCH-001 (konsolidacja trzech paneli), DATA-003 (rozbicie
`PlaceOrderCommandHandler`) i SEC-003 (centralizacja zakresu danych) to zmiany na tysiącach linii kodu,
których poprawność jest weryfikowalna **wyłącznie** przez testy integracyjne.

#### Ryzyko

- **Rozwój:** cała roadmapa architektoniczna jest zablokowana albo obarczona nieakceptowalnym ryzykiem.
- **Bezpieczeństwo:** regresja w autoryzacji przechodzi przez CI niezauważona.
- **Dane:** regresja w zapytaniach ujawnia się dopiero w produkcji.
- **Contributorzy:** PR wygląda na bezpieczny (zielone CI), a nie jest.
- **Utrzymanie:** niestabilne testy uczą zespół ignorowania czerwonego CI.

#### Rekomendacja

W kolejności, bo kolejność jest tu istotna:

1. **Naprawić niestabilność** przy równoległym uruchomieniu — zielone CI musi znaczyć „zielone", inaczej
   reszta nie ma wartości. Przyczyną jest współdzielony stan statyczny (`DataSettingsManager.Instance`,
   `PluginManager.ReferencedPlugins`, `AutoMapperConfig`, `MemoryCacheBase._resetCacheToken`).
2. **Testy architektoniczne** (TEST-003) — najtańsza rzecz o największym zwrocie.
3. **Baza testów integracyjnych** z `WebApplicationFactory` + MongoDB z kontenera — najpierw pokrycie
   autoryzacji i zakresu danych trzech paneli, bo to jest dokładnie to, co złamie ARCH-001.
4. **Testy kontraktowe API** dla `Grand.Module.Api`.

#### Koszt

**L**

#### Breaking Change

**Nie.**

---

## 3. Lista inicjatyw architektonicznych

Inicjatywy to logiczne całości, nie pojedyncze zmiany. Każda ma własny numer i jest rozbita na zadania
w sekcji 4.

---

### INIT-01 – Integralność danych na ścieżkach transakcyjnych

**Cel**

Doprowadzić do stanu, w którym równoległe operacje na zamówieniach i stanach magazynowych nie mogą
prowadzić do utraty zapisu ani do duplikatu identyfikatora biznesowego.

**Problem**

`OrderService.InsertOrder` nadaje numer przez read-max-then-increment. `InventoryManageService` modyfikuje
stany przez read-modify-write, czterema osobnymi zapisami. `BaseEntity` nie ma znacznika współbieżności.
Nie ma indeksu unikalnego chroniącego `Order.OrderNumber`.

**Zakres**

- `Grand.Core/Grand.Data` — `IRepository<T>`, `MongoRepository<T>`, `LiteDBRepository<T>`, `UpdateBuilder<T>`
- `Grand.Core/Grand.Domain` — `BaseEntity`
- `Grand.Business.Checkout` — `OrderService`
- `Grand.Business.Catalog` — `InventoryManageService`, `ProductService`
- `Grand.Module.Migration` — migracja indeksów i pola wersji
- `Grand.Module.Installer` — `InstallationService.CreateIndexes`

**Korzyści**

- Eliminacja duplikatów numerów zamówień i oversellu.
- Warunek konieczny bezpiecznego skalowania poziomego.
- Mniej round-tripów do bazy w ścieżce checkoutu (efekt uboczny: szybszy checkout).

**Ryzyka**

- Dodanie pola do `BaseEntity` zmienia kształt każdego dokumentu — wymaga migracji i regresji serializacji.
- LiteDB nie ma odpowiednika wszystkich operacji atomowych MongoDB — degradacja musi być jawna
  i udokumentowana, nie cicha.
- Zewnętrzne implementacje `IRepository<T>` przestaną się kompilować.

**Zależności**

- INIT-05 (testy) — dla kroku z polem wersji; kroki atomowe (`$inc`) można wykonać bez niej.

**Szacowany effort**

**L**

---

### INIT-02 – Hardening bezpieczeństwa aplikacji

**Cel**

Zamknąć realne wektory ataku: stored XSS, niebezpieczne domyślne ustawienia, DoS przez upload, PII
w telemetrii.

**Problem**

Ochrona przed XSS oparta na czarnej liście regex przy 80 wywołaniach `Html.Raw`. Wszystkie mechanizmy
bezpieczeństwa transportu domyślnie wyłączone. Upload materializuje plik przed sprawdzeniem rozmiaru.
E-mail klienta w tagach trace'a.

**Zakres**

- `Grand.Web.Common` — `NoScriptsAttribute`, `ContextLoggingMiddleware`, `ServiceCollectionExtensions`
- `Grand.Web` — `ContactController`, `ProductController`, `ShoppingCartController`
- `Grand.Web/App_Data/appsettings.json` + nowy `appsettings.Development.json`
- `Grand.Business.Common` — punkt wejścia sanityzacji
- `Directory.Packages.props` — nowa zależność (sanitizer HTML)

**Korzyści**

- Zamknięcie ścieżki eskalacji uprawnień z vendora do administratora.
- Bezpieczna instalacja domyślna.
- Zgodność z zasadą minimalizacji danych.

**Ryzyka**

- Sanityzacja odrzuci treści HTML, które dotąd działały — migracja musi raportować zmiany.
- Włączone przekierowanie HTTPS może dać pętlę za źle skonfigurowanym proxy.
- Nowa zależność (`HtmlSanitizer`) zwiększa powierzchnię utrzymania.

**Zależności**

Brak — inicjatywa jest niezależna i może iść równolegle z INIT-01.

**Szacowany effort**

**M**

---

### INIT-03 – Centralizacja i egzekwowanie zakresu danych najemcy

**Cel**

Sprawić, że izolacja vendora i sklepu jest domyślna i weryfikowalna automatycznie, a nie zależna od
dyscypliny autora każdej akcji.

**Problem**

88 ręcznych porównań `VendorId` w `Grand.Web.Vendor` plus analogiczne sprawdzenia `StaffStoreId`
w `Grand.Web.Store`. Brak centralnego mechanizmu, brak testu wykrywającego pominięcie.

**Zakres**

- `Grand.Web.Common` — nowe `IDataScopeProvider`, `ScopedResourceAttribute`
- `Grand.Web.AdminShared` — serwisy widoku przyjmujące zakres
- `Grand.Web.Vendor`, `Grand.Web.Store` — wszystkie kontrolery
- `Grand.Business.Core` — ewentualne rozszerzenie `IAclService`
- nowy projekt testów architektonicznych

**Korzyści**

- Zamknięcie klasy podatności IDOR, nie pojedynczych wystąpień.
- Warunek wykonalności INIT-04 (konsolidacji paneli) — zakres jest główną różnicą między nimi.
- Nowa akcja jest domyślnie bezpieczna.

**Ryzyka**

- Zbyt agresywny zakres domyślny zablokuje legalne operacje administratora globalnego.
- Wymaga przejścia przez ~370 akcji — duża powierzchnia zmian bez siatki testowej.

**Zależności**

- INIT-05 (testy integracyjne autoryzacji) — **twarda zależność**, nie do pominięcia.

**Szacowany effort**

**L**

---

### INIT-04 – Konsolidacja paneli zaplecza

**Cel**

Doprowadzić do stanu, w którym logika edycji encji zaplecza istnieje w jednym miejscu, a trzy hosty
różnią się wyłącznie autoryzacją, trasami i zakresem danych.

**Problem**

Trzy kopie `ProductController` (2478/2625/2584 linii), dwie kopie `ProductViewModelService`
(2571/2381 linii), rozjechane o setki linii. Poprawka wymaga trzech zmian; pominięcie jednej
to regresja, historycznie także regresja bezpieczeństwa.

**Zakres**

- `Grand.Web.AdminShared` — generyczne kontrolery bazowe i serwisy widoku
- `Grand.Web.Admin`, `Grand.Web.Store`, `Grand.Web.Vendor` — kontrolery redukowane do klas pochodnych
- widoki: przeniesienie domyślnych do `AdminShared`, `ViewLocationExpander` dla nadpisań
- `Grand.Web.Vendor/Services` — usunięcie pięciu zduplikowanych serwisów

**Korzyści**

- Trzykrotna redukcja kosztu każdej zmiany w zapleczu.
- Eliminacja klasy błędów „poprawka trafiła tylko do dwóch paneli".
- Niższa bariera dla kontrybutorów zewnętrznych.

**Ryzyka**

- **Największa pojedyncza zmiana w tej roadmapie.** Bez testów integracyjnych to gwarantowana regresja.
- Zmiana typów kontrolerów łamie wtyczki nadpisujące widoki lub menu.
- Możliwa nadmierna generalizacja — trzy panele różnią się realnie, a nie tylko przypadkowo.

**Zależności**

- INIT-05 (testy integracyjne) — **twarda zależność**.
- INIT-03 (zakres danych) — powinien poprzedzać, bo zakres jest parametrem generalizacji.

**Szacowany effort**

**XL**

---

### INIT-05 – Siatka bezpieczeństwa: testy integracyjne i architektoniczne

**Cel**

Zapewnić, że zmiany INIT-01, INIT-03 i INIT-04 są wykonalne bez regresji, i że reguły architektoniczne
przetrwają rotację zespołu.

**Problem**

1826 testów na mockach, 100 testów na całą warstwę webową, zero testów integracyjnych, zero testów
architektonicznych, niestabilność przy równoległym uruchomieniu.

**Zakres**

- nowy projekt `Grand.Architecture.Tests`
- nowy projekt `Grand.IntegrationTests` (`WebApplicationFactory` + MongoDB z kontenera)
- `src/Tests/**` — izolacja stanu statycznego
- `azure-pipelines.yml`, `.github/workflows/aspnetcore.yml` — nowe kroki
- `Directory.Packages.props` — `Microsoft.AspNetCore.Mvc.Testing`, `NetArchTest.Rules` lub `Testcontainers`

**Korzyści**

- Odblokowanie całej reszty roadmapy.
- Wykrywanie regresji autoryzacji w CI.
- Reguły architektoniczne egzekwowane maszynowo, nie w code review.

**Ryzyka**

- Wydłużenie czasu CI — wymaga podziału na szybki (PR) i pełny (nocny) zestaw.
- Testy integracyjne są droższe w utrzymaniu; źle napisane stają się hamulcem.

**Zależności**

Brak. **To jest pierwsza inicjatywa do realizacji po INIT-01/INIT-02.**

**Szacowany effort**

**L**

---

### INIT-06 – Obserwowalność produkcyjna

**Cel**

Umożliwić diagnozę incydentu produkcyjnego z telemetrii, bez sięgania do bazy danych, i mierzalną
weryfikację efektów optymalizacji.

**Problem**

Health check zwraca zawsze `Healthy`. Zero metryk domenowych, zero własnych span'ów. PII w tagach.

**Zakres**

- `Grand.Web.Common` — health checks, `ContextLoggingMiddleware`
- `Grand.Infrastructure` — nowe `GrandDiagnostics` (`ActivitySource`, `Meter`)
- `Grand.Business.Checkout`, `Grand.Business.Catalog` — instrumentacja kluczowych ścieżek
- `Aspire.ServiceDefaults` — rejestracja źródeł

**Korzyści**

- Skrócenie MTTR.
- Poprawne działanie orkiestratorów (K8s, App Service).
- Mierzalność fazy 7 (wydajność).

**Ryzyka**

- Nadmierna kardynalność metryk (np. wymiar `product.id`) potrafi wywrócić backend telemetrii.
- Instrumentacja w gorących ścieżkach ma koszt — wymaga próbkowania.

**Zależności**

Brak.

**Szacowany effort**

**M**

---

### INIT-07 – Stabilizacja przetwarzania w tle

**Cel**

Zadania cykliczne mają być samonaprawialne, diagnozowalne i możliwe do harmonogramowania w sposób,
którego oczekują operatorzy sklepów.

**Problem**

`break` kończący pętlę na stałe, `catch (Exception)` bez logowania, granularność minutowa, brak cron,
brak retry wokół wywołań zewnętrznych.

**Zakres**

- `Grand.Web.Common` — `BackgroundServiceTask`
- `Grand.Module.ScheduledTasks` — `ScheduleTaskService`, zadania
- `Grand.Domain.Tasks` — `ScheduleTask` (przy wprowadzeniu cron)
- `Grand.Module.Migration` — migracja schematu zadań

**Korzyści**

- Koniec cichych zatrzymań wysyłki maili.
- Diagnozowalność błędów zadań.
- Harmonogramy typu „codziennie o 3:00".

**Ryzyka**

- Zmiana schematu `ScheduleTask` wymaga migracji i dotyka wtyczek rejestrujących własne zadania.

**Zależności**

- INIT-06 (metryki zadań) — komplementarne, nie blokujące.

**Szacowany effort**

**M**

---

### INIT-08 – Dojrzały kontrakt wtyczek

**Cel**

Umożliwić autorom wtyczek zewnętrznych utrzymanie wtyczki przez więcej niż jedno wydanie minor, przy
jasno zdefiniowanym API.

**Problem**

Dokładne porównanie wersji `Major.Minor` unieważnia wszystkie wtyczki przy każdym minorze. Brak jawnego
publicznego API. Brak izolacji `AssemblyLoadContext`.

**Zakres**

- `Grand.Infrastructure.Plugins` — `PluginInfoAttribute`, `PluginVersionResolver`, `PluginManager`
- `docs/` — dokument publicznego API wtyczek
- `Grand.Architecture.Tests` — test pilnujący granic API
- `src/Plugins/**` — aktualizacja 16 wtyczek referencyjnych

**Korzyści**

- Realna możliwość istnienia ekosystemu wtyczek zewnętrznych.
- Swoboda refaktoryzacji tego, co nie jest API.

**Ryzyka**

- Deklaracja API to zobowiązanie — od tego momentu jego zmiana jest breaking change.
- Izolacja ALC to trudny technicznie krok z ryzykiem subtelnych błędów typów.

**Zależności**

- INIT-05 (testy architektoniczne) dla egzekwowania granic API.

**Szacowany effort**

**L**

---

### INIT-09 – Wydajność oparta na dowodach

**Cel**

Usunąć potwierdzone wąskie gardła; nie optymalizować niczego bez pomiaru.

**Problem**

Blokujące `.Result` w eksporcie, brak limitu rozmiaru `IMemoryCache`, indeksy tworzone wyłącznie przy
instalacji, potencjalnie nieindeksowane zapytania `$filter` w API.

**Zakres**

- `Grand.Business.*/Services/ExportImport` — schematy eksportu
- `Grand.Infrastructure.Caching` — `MemoryCacheBase`
- `Grand.Module.Installer`, `Grand.Module.Migration` — zarządzanie indeksami
- `Grand.Module.Api` — limity zapytań

**Korzyści**

- Stabilna praca pod obciążeniem.
- Przewidywalne zużycie pamięci.

**Ryzyka**

- Limit rozmiaru cache źle dobrany degraduje wydajność bardziej, niż jej brak.
- Optymalizacja bez pomiaru to zwykle strata czasu — stąd zależność od INIT-06.

**Zależności**

- INIT-06 (metryki) — **twarda zależność** dla wszystkiego poza pozycjami oznaczonymi jako Confirmed.

**Szacowany effort**

**M**

---

## 4. Konkretne zadania implementacyjne

Zadania są atomowe: każde da się wykonać, zweryfikować i scalić niezależnie, o ile spełnione są jego
zależności.

---

### DATA-011 – Atomowy sekwencer numeru zamówienia

**Cel:**
`Order.OrderNumber` ma być nadawany atomowo, bez wyścigu i bez blokowania wątku puli.

**Zakres:**
Nowa kolekcja liczników (`Counter` z polami `Id`, `Value`). Nowa metoda w `IRepository<T>` albo dedykowany
`ISequenceService` używający `FindOneAndUpdate` z `$inc` i `IsUpsert = true`, `ReturnDocument.After`.
Podmiana wyznaczania numeru w `OrderService.InsertOrder`. Implementacja awaryjna dla LiteDB
(blokada procesowa — LiteDB jest jednoprocesowy, więc to wystarcza).

**Dotknięte projekty:**

* `Grand.Data`
* `Grand.Domain`
* `Grand.Business.Core`
* `Grand.Business.Common`
* `Grand.Business.Checkout`

**Dotknięte pliki/klasy:**

* `src/Business/Grand.Business.Checkout/Services/Orders/OrderService.cs` → `InsertOrder`
* `src/Core/Grand.Data/Mongo/MongoRepository.cs`
* `src/Core/Grand.Data/LiteDb/LiteDBRepository.cs`
* nowy `src/Core/Grand.Domain/Common/Counter.cs`
* nowy `ISequenceService` / `SequenceService`

**Zależności:**

Brak.

**Kryteria akceptacji:**

* [ ] `InsertOrder` nie wykonuje synchronicznego zapytania LINQ (`FirstOrDefault()` bez `Async`)
* [ ] Test integracyjny: 100 równoległych `InsertOrder` daje 100 unikalnych `OrderNumber`
* [ ] Numeracja zachowuje ciągłość dla istniejących instalacji (licznik inicjowany z aktualnego maksimum)
* [ ] `MaxOrderNumberCommandHandler` (ustawienie numeru startowego w panelu) nadal działa i aktualizuje licznik
* [ ] Ścieżka LiteDB działa i ma test

**Ryzyko:** Średnie
**Breaking Change:** Nie
**Effort:** S

---

### DATA-012 – Unikalny indeks na numerze zamówienia

**Cel:**
Baza ma odrzucać duplikat `OrderNumber` niezależnie od poprawności kodu aplikacji.

**Zakres:**
Dodanie tworzenia indeksu unikalnego w instalatorze oraz migracja dla istniejących instalacji.
Migracja musi wykryć istniejące duplikaty i zaraportować je w wyniku migracji zamiast przerywać upgrade.

**Dotknięte projekty:**

* `Grand.Module.Installer`
* `Grand.Module.Migration`

**Dotknięte pliki/klasy:**

* `src/Modules/Grand.Module.Installer/Services/InstallationService.cs` → `CreateIndexes`
* nowa migracja w `src/Modules/Grand.Module.Migration/Migrations/2.5/`

**Zależności:**

* `DATA-011`

**Kryteria akceptacji:**

* [ ] Nowa instalacja ma indeks unikalny na `Order.OrderNumber`
* [ ] Migracja tworzy indeks w istniejącej instalacji
* [ ] Migracja na danych z duplikatami kończy się `MigrationResult` z opisem, nie wyjątkiem
* [ ] Indeks pomija dokumenty z `Deleted = true` używane jako znacznik numeru startowego (partial index)

**Ryzyko:** Średnie
**Breaking Change:** Nie
**Effort:** S

---

### DATA-013 – Atomowe modyfikacje stanów magazynowych

**Cel:**
Zmiany `StockQuantity` i `ReservedQuantity` mają być atomowe i wykonywane w jednym zapisie.

**Zakres:**
Zamiana ścieżek inkrementalnych w `InventoryManageService` na `IncField` / `UpdateOneAsync`
z `UpdateBuilder`. Warunek „nie schodź poniżej zera" wyrażony filtrem w zapytaniu, z weryfikacją liczby
zmodyfikowanych dokumentów. Cztery osobne `UpdateField` w `UpdateStockProduct` scalone w jedno wywołanie.

**Dotknięte projekty:**

* `Grand.Business.Catalog`
* `Grand.Data`

**Dotknięte pliki/klasy:**

* `src/Business/Grand.Business.Catalog/Services/Products/InventoryManageService.cs` →
  `UpdateStockProduct`, `AdjustReserved`, `BookReservedInventory`, `ReverseBookedInventory`
* `src/Core/Grand.Data/UpdateBuilder.cs` (rozszerzenie o `Inc`, jeśli brak)

**Zależności:**

* `TEST-021` (testy integracyjne magazynu) — zalecane przed, nie blokujące

**Kryteria akceptacji:**

* [ ] `UpdateStockProduct` wykonuje jeden zapis, nie cztery
* [ ] Test integracyjny: 50 równoległych sprzedaży po 1 szt. z zapasu 50 kończy się stanem 0, nie wyższym
* [ ] Sprzedaż powyżej dostępnego zapasu jest odrzucana, gdy `AllowOutOfStockOrders = false`
* [ ] Zachowane zdarzenia `EntityUpdated` i inwalidacja cache
* [ ] Ścieżka LiteDB działa (degradacja do read-modify-write z jawnym komentarzem)

**Ryzyko:** Wysokie
**Breaking Change:** Nie
**Effort:** M

---

### DATA-014 – Znacznik współbieżności w BaseEntity

**Cel:**
Umożliwić optimistic concurrency tam, gdzie operacja atomowa na pojedynczym polu nie wystarcza.

**Zakres:**
Pole wersji w `BaseEntity`. Rozszerzenie `IRepository<T>.UpdateAsync` o wariant z warunkiem na wersji,
rzucający dedykowany wyjątek przy niezgodności. Migracja ustawiająca wartość początkową.
Zastosowanie w `InventoryManageService` dla zapisów obiektów zagnieżdżonych.

**Dotknięte projekty:**

* `Grand.Domain`
* `Grand.Data`
* `Grand.Business.Catalog`
* `Grand.Module.Migration`

**Dotknięte pliki/klasy:**

* `src/Core/Grand.Domain/BaseEntity.cs`
* `src/Core/Grand.Data/IRepository.cs`
* `src/Core/Grand.Data/Mongo/MongoRepository.cs`
* `src/Core/Grand.Data/LiteDb/LiteDBRepository.cs`
* nowa migracja

**Zależności:**

* `DATA-013`
* `TEST-021`

**Kryteria akceptacji:**

* [ ] Wersja jest inkrementowana przy każdym `UpdateAsync` i `UpdateOneAsync`
* [ ] Zapis z nieaktualną wersją rzuca `ConcurrencyException`
* [ ] Istniejące dokumenty bez pola wersji są obsłużone (traktowane jako wersja 0)
* [ ] Zdefiniowana i udokumentowana strategia dla LiteDB
* [ ] Testy serializacji nie regresują (`Grand.Data.Tests`)

**Ryzyko:** Wysokie
**Breaking Change:** Tak
**Effort:** L

---

### DATA-015 – Opcjonalne wsparcie transakcji MongoDB

**Cel:**
Umożliwić objęcie grupy zapisów jedną transakcją, gdy baza to obsługuje, bez łamania instalacji standalone
i LiteDB.

**Zakres:**
`IDatabaseContext` dostaje metodę otwierającą sesję. `IRepository<T>` dostaje warianty metod przyjmujące
sesję. Wykrycie zdolności transakcyjnej przy starcie (replica set) i degradacja do zachowania obecnego,
gdy niedostępna — jawnie zalogowana.

**Dotknięte projekty:**

* `Grand.Data`
* `Grand.Infrastructure`

**Dotknięte pliki/klasy:**

* `src/Core/Grand.Data/IDatabaseContext.cs`
* `src/Core/Grand.Data/Mongo/MongoDBContext.cs`
* `src/Core/Grand.Data/IRepository.cs`
* `src/Core/Grand.Data/Mongo/MongoRepository.cs`
* `src/Core/Grand.Data/LiteDb/LiteDBRepository.cs`
* `src/Core/Grand.Infrastructure/Configuration/DatabaseConfig.cs`

**Zależności:**

* `TEST-020` (infrastruktura testów integracyjnych z replica set)

**Kryteria akceptacji:**

* [ ] Kod bez jawnej sesji zachowuje się dokładnie jak dotąd
* [ ] Transakcja wycofuje wszystkie zapisy przy wyjątku (test integracyjny na replica set)
* [ ] Standalone MongoDB i LiteDB działają bez zmian, z wpisem w logu o niedostępności transakcji
* [ ] Brak wycieku sesji przy wyjątku (`using`/`await using`)

**Ryzyko:** Wysokie
**Breaking Change:** Częściowo
**Effort:** L

---

### DATA-016 – Rozbicie PlaceOrderCommandHandler na fazy

**Cel:**
Uczynić proces składania zamówienia zrozumiałym, testowalnym i możliwym do objęcia transakcją.

**Zakres:**
Podział 996-liniowego handlera na jawne fazy jako osobne klasy: walidacja, budowa agregatu zamówienia,
utrwalenie, efekty uboczne, powiadomienia. Handler staje się orkiestratorem. Liczba zależności
w konstruktorze każdej klasy nie przekracza rozsądnego progu (~8).

**Dotknięte projekty:**

* `Grand.Business.Checkout`

**Dotknięte pliki/klasy:**

* `src/Business/Grand.Business.Checkout/Commands/Handlers/Orders/PlaceOrderCommandHandler.cs`
* nowe klasy w `src/Business/Grand.Business.Checkout/Services/Orders/Placement/`

**Zależności:**

* `TEST-022` (testy integracyjne checkoutu) — **twarda zależność**

**Kryteria akceptacji:**

* [ ] `PlaceOrderCommand` i `PlaceOrderResult` (kontrakt w `Grand.Business.Core`) bez zmian
* [ ] Żadna nowa klasa nie ma więcej niż 8 zależności w konstruktorze
* [ ] Istniejące testy `Grand.Business.Checkout.Tests` przechodzą bez modyfikacji asercji biznesowych
* [ ] Testy integracyjne checkoutu (happy path + 5 ścieżek błędu) przechodzą
* [ ] Zdarzenia (`OrderPlacedEvent` i pozostałe) publikowane w niezmienionej kolejności

**Ryzyko:** Wysokie
**Breaking Change:** Nie
**Effort:** L

---

### DATA-017 – Transakcyjne utrwalenie zamówienia

**Cel:**
Faza utrwalenia zamówienia ma być atomowa: albo zapisane jest wszystko, albo nic.

**Zakres:**
Objęcie fazy `PersistOrder` z DATA-016 sesją z DATA-015. Efekty uboczne nietransakcyjne (mail, bramka
płatnicza) pozostają poza transakcją. Jawna obsługa sytuacji, w której transakcje są niedostępne.

**Dotknięte projekty:**

* `Grand.Business.Checkout`

**Dotknięte pliki/klasy:**

* `src/Business/Grand.Business.Checkout/Services/Orders/Placement/` (klasa fazy utrwalenia)

**Zależności:**

* `DATA-015`
* `DATA-016`

**Kryteria akceptacji:**

* [ ] Wyjątek w środku fazy utrwalenia nie zostawia dokumentu `Order` w bazie (test na replica set)
* [ ] Na instalacji bez transakcji zachowanie jest identyczne z obecnym, z ostrzeżeniem w logu
* [ ] Wywołanie bramki płatniczej pozostaje poza transakcją
* [ ] Czas składania zamówienia nie rośnie o więcej niż 15% (pomiar przed/po)

**Ryzyko:** Wysokie
**Breaking Change:** Nie
**Effort:** M

---

### SEC-011 – Sanityzacja HTML na białej liście

**Cel:**
Zastąpić czarną listę regex sanityzacją opartą na dojrzałej bibliotece, wykonywaną przy zapisie.

**Zakres:**
Dodanie zależności sanityzatora HTML. Nowy `IHtmlSanitizationService` z konfigurowalną białą listą tagów
i atrybutów. Zamiana `NoScriptsAttribute` na mechanizm sanityzujący (nie odrzucający) w miejscach
dotyczących pól bogatego HTML; pozostawienie walidacji odrzucającej dla pól, które nie powinny zawierać
HTML w ogóle. Migracja sanityzująca istniejące treści z raportem zmian.

**Dotknięte projekty:**

* `Grand.SharedKernel` lub `Grand.Business.Common`
* `Grand.Web.Common`
* `Grand.Web.AdminShared`
* `Grand.Web.Vendor`
* `Grand.Module.Migration`
* `Directory.Packages.props`

**Dotknięte pliki/klasy:**

* `src/Web/Grand.Web.Common/Validators/NoScriptsAttribute.cs`
* 56 miejsc użycia `[NoScripts]` w modelach `AdminShared` i `Vendor`
* nowy `IHtmlSanitizationService` / `HtmlSanitizationService`
* nowa migracja sanityzująca

**Zależności:**

Brak.

**Kryteria akceptacji:**

* [ ] Zestaw testów z payloadami XSS (min. 20 wariantów: `onfocus`, `svg onload` z nową linią, `iframe srcdoc`, encje HTML, `data:` URI) — wszystkie neutralizowane
* [ ] Legalny HTML sklepowy (tabele, obrazy, osadzone wideo) przechodzi bez zmian
* [ ] Biała lista jest konfigurowalna ustawieniem, z bezpieczną wartością domyślną
* [ ] Migracja raportuje, ile rekordów i jakie tagi zostały zmienione
* [ ] Pola nieprzeznaczone na HTML (nazwa, meta tytuł) nadal odrzucają HTML zamiast go sanityzować

**Ryzyko:** Średnie
**Breaking Change:** Częściowo
**Effort:** M

---

### SEC-012 – Bezpieczne domyślne ustawienia bezpieczeństwa

**Cel:**
Instalacja domyślna ma być bezpieczna; środowisko deweloperskie ma pozostać wygodne.

**Zakres:**
Odwrócenie wartości domyślnych w `appsettings.json` dla `UseDefaultSecurityHeaders`, `UseHsts`,
`UseHttpsRedirection`, `CookieSecurePolicyAlways`. Utworzenie `appsettings.Development.json`
przywracającego wartości deweloperskie. Nowy startup ostrzegający o wyłączonych mechanizmach poza
Development.

**Dotknięte projekty:**

* `Grand.Web`, `Grand.Web.Admin`, `Grand.Web.Store`, `Grand.Web.Vendor`
* `Grand.Web.Common`

**Dotknięte pliki/klasy:**

* `src/Web/Grand.Web/App_Data/appsettings.json` (i odpowiedniki w pozostałych hostach)
* nowy `src/Web/Grand.Web/App_Data/appsettings.Development.json`
* nowy `src/Web/Grand.Web.Common/Startup/SecurityAdvisoryStartup.cs`

**Zależności:**

Brak.

**Kryteria akceptacji:**

* [ ] Świeża instalacja produkcyjna wysyła nagłówki bezpieczeństwa i wymusza HTTPS
* [ ] `dotnet run` w Development działa bez certyfikatu i bez pętli przekierowań
* [ ] Wyłączenie mechanizmu poza Development produkuje `LogWarning` z nazwą klucza konfiguracyjnego
* [ ] Start aplikacji nie jest przerywany (tylko ostrzeżenie)
* [ ] Nota w release notes o możliwej pętli przekierowań za proxy bez `UseForwardedHeaders`

**Ryzyko:** Średnie
**Breaking Change:** Częściowo
**Effort:** S

---

### SEC-013 – Zabezpieczenie endpointów uploadu

**Cel:**
Publiczne endpointy uploadu nie mogą być tanim wektorem DoS ani przyjmować dowolnej zawartości.

**Zakres:**
Sprawdzanie `IFormFile.Length` przed materializacją. Twardy limit globalny niezależny od konfiguracji
atrybutu. Domyślna biała lista rozszerzeń przy pustej konfiguracji. Rate limiting na endpointach
nieuwierzytelnionych.

**Dotknięte projekty:**

* `Grand.Web`
* `Grand.Web.Common`
* `Grand.SharedKernel`

**Dotknięte pliki/klasy:**

* `src/Web/Grand.Web/Controllers/ContactController.cs` → `UploadFileContactAttribute`
* `src/Web/Grand.Web/Controllers/ProductController.cs` → `UploadFileProductAttribute`
* `src/Web/Grand.Web/Controllers/ShoppingCartController.cs` → `UploadFileCheckoutAttribute`
* `src/Core/Grand.SharedKernel/Extensions/AllowedFileExtensions.cs`
* `src/Web/Grand.Web.Common/Extensions/StorageExtensions.cs`

**Zależności:**

Brak.

**Kryteria akceptacji:**

* [ ] Plik przekraczający limit jest odrzucany bez alokacji `byte[]` całej zawartości
* [ ] Pusta `ValidationFileAllowedExtensions` oznacza domyślną białą listę, nie „wszystko wolno"
* [ ] Twardy limit globalny obowiązuje niezależnie od konfiguracji atrybutu
* [ ] Rate limiting odrzuca nadmiar żądań z jednego adresu z kodem 429
* [ ] Test: 20 równoległych uploadów 30 MB nie powoduje wzrostu pamięci procesu powyżej progu

**Ryzyko:** Niskie
**Breaking Change:** Częściowo
**Effort:** S

---

### SEC-014 – Usunięcie danych osobowych z telemetrii

**Cel:**
Trace'y nie mogą zawierać adresów e-mail ani innych danych identyfikujących klienta.

**Zakres:**
Zamiana tagu `Customer` z `Email` na `Id`, tagu `Store` z `Name` na `Id`. Przegląd pozostałych wywołań
`AddTag` i logowania strukturalnego pod kątem PII.

**Dotknięte projekty:**

* `Grand.Web.Common`

**Dotknięte pliki/klasy:**

* `src/Web/Grand.Web.Common/Middleware/ContextLoggingMiddleware.cs`

**Zależności:**

Brak.

**Kryteria akceptacji:**

* [ ] Żaden tag `Activity` nie zawiera adresu e-mail, imienia, nazwiska ani adresu
* [ ] Korelacja klienta w trace'ach nadal możliwa przez `customer.id`
* [ ] Przegląd `LogInformation`/`LogError` w `PluginController` i `AccountController` pod kątem e-maili
      w komunikatach (obecnie `PluginController` loguje `CurrentCustomerEmail`)
* [ ] Nota w release notes o zmianie formatu tagów

**Ryzyko:** Niskie
**Breaking Change:** Nie
**Effort:** S

---

### SEC-015 – Centralny mechanizm zakresu danych najemcy

**Cel:**
Zakres danych vendora i sklepu ma być egzekwowany przez infrastrukturę, nie przez ręczne sprawdzenia
w akcjach.

**Zakres:**
`IDataScopeProvider` zwracający filtr dla bieżącego kontekstu. `ScopedResourceAttribute` weryfikujący
przynależność encji wskazanej parametrem trasy przed wejściem do akcji. Przepisanie kontrolerów
`Grand.Web.Vendor` i `Grand.Web.Store` na nowy mechanizm, z usunięciem ręcznych porównań.

**Dotknięte projekty:**

* `Grand.Web.Common`
* `Grand.Web.Vendor`
* `Grand.Web.Store`
* `Grand.Web.AdminShared`

**Dotknięte pliki/klasy:**

* nowy `src/Web/Grand.Web.Common/Security/Scope/IDataScopeProvider.cs`
* nowy `src/Web/Grand.Web.Common/Security/Scope/ScopedResourceAttribute.cs`
* `src/Web/Grand.Web.Vendor/Controllers/**` (11 kontrolerów, 88 miejsc)
* `src/Web/Grand.Web.Store/Controllers/**` (36 kontrolerów)

**Zależności:**

* `TEST-023` (testy integracyjne izolacji najemcy) — **twarda zależność**
* `TEST-024` (test architektoniczny kompletności atrybutów)

**Kryteria akceptacji:**

* [ ] Żadna akcja w `Grand.Web.Vendor`/`Grand.Web.Store` nie zawiera ręcznego porównania `VendorId`/`StaffStoreId`
* [ ] Każda akcja ma atrybut zakresu albo jawne `Ignore` z komentarzem uzasadniającym
* [ ] Test integracyjny: vendor A nie odczyta ani nie zmodyfikuje żadnego zasobu vendora B (pokrycie wszystkich kontrolerów)
* [ ] Test architektoniczny zawodzi przy dodaniu akcji bez atrybutu zakresu
* [ ] Administrator globalny zachowuje pełny dostęp

**Ryzyko:** Wysokie
**Breaking Change:** Nie
**Effort:** L

---

### ARCH-011 – Włączenie walidacji kontenera DI w Development

**Cel:**
Błędy rejestracji i captive dependencies mają być wykrywane przy starcie na maszynie deweloperskiej
i w CI, nie w produkcji.

**Zakres:**
Warunkowe `ValidateScopes`/`ValidateOnBuild` zależne od środowiska we wszystkich czterech `Program.cs`.
Naprawa wszystkich błędów ujawnionych przez walidację.

**Dotknięte projekty:**

* `Grand.Web`, `Grand.Web.Admin`, `Grand.Web.Store`, `Grand.Web.Vendor`

**Dotknięte pliki/klasy:**

* `src/Web/Grand.Web/Program.cs`
* `src/Web/Grand.Web.Admin/Program.cs`
* `src/Web/Grand.Web.Store/Program.cs`
* `src/Web/Grand.Web.Vendor/Program.cs`

**Zależności:**

Brak.

**Kryteria akceptacji:**

* [ ] Wszystkie cztery hosty startują w Development z włączoną walidacją
* [ ] Produkcja zachowuje wyłączoną walidację (odporność na wadliwą wtyczkę)
* [ ] Zero captive dependencies na `IWorkContext`/`IStoreContext`
* [ ] Udokumentowany powód wyłączenia w produkcji (komentarz w `Program.cs`)

**Ryzyko:** Średnie
**Breaking Change:** Nie
**Effort:** M

---

### ARCH-012 – Usunięcie BuildServiceProvider z konfiguracji

**Cel:**
Wyeliminować tworzenie tymczasowych kontenerów i duplikację singletonów podczas konfiguracji.

**Zakres:**
`OpenApiStartup` — wiązanie konfiguracji bezpośrednio z `IConfiguration`, `IWebHostEnvironment`
z parametru. `PluginManager` i `ModuleLoader` — `LoggerFactory.Create(...)` zamiast kontenera.
`Grand.Web.Common/Startup/StartupApplication` — usunięcie ścieżki z `BuildServiceProvider`
dla `LocService`.

**Dotknięte projekty:**

* `Grand.Infrastructure`
* `Grand.Module.Api`
* `Grand.Web.Common`

**Dotknięte pliki/klasy:**

* `src/Modules/Grand.Module.Api/Infrastructure/OpenApiStartup.cs:36-38`
* `src/Core/Grand.Infrastructure/Plugins/PluginManager.cs:58`
* `src/Core/Grand.Infrastructure/Modules/ModuleLoader.cs:108`
* `src/Web/Grand.Web.Common/Startup/StartupApplication.cs:127`

**Zależności:**

* `ARCH-011` (walidacja ujawni skutki uboczne)

**Kryteria akceptacji:**

* [ ] Zero wywołań `BuildServiceProvider()` poza projektami testowymi
* [ ] Logowanie ładowania wtyczek i modułów działa bez zmian
* [ ] Dokumentacja OpenAPI generuje się bez zmian
* [ ] Instalacja od zera (bez zainstalowanej bazy) działa

**Ryzyko:** Średnie
**Breaking Change:** Nie
**Effort:** S

---

### ARCH-013 – Uogólnione kontrolery zaplecza w AdminShared

**Cel:**
Logika edycji encji zaplecza ma istnieć w jednym miejscu, sparametryzowana zakresem danych.

**Zakres:**
Przeniesienie logiki `ProductController` do generycznej klasy bazowej w `Grand.Web.AdminShared`,
sparametryzowanej `IDataScopeProvider`. Redukcja trzech kontrolerów do klas pochodnych deklarujących
autoryzację, trasę i zakres. Usunięcie `Grand.Web.Vendor/Services/ProductViewModelService.cs`.
**Produkt jest pilotem** — po jego zamknięciu i weryfikacji wzorca powtórzenie dla kolejnych encji.

**Dotknięte projekty:**

* `Grand.Web.AdminShared`
* `Grand.Web.Admin`
* `Grand.Web.Store`
* `Grand.Web.Vendor`

**Dotknięte pliki/klasy:**

* `src/Web/Grand.Web.Admin/Controllers/ProductController.cs` (2478 linii)
* `src/Web/Grand.Web.Store/Controllers/ProductController.cs` (2625 linii)
* `src/Web/Grand.Web.Vendor/Controllers/ProductController.cs` (2584 linii)
* `src/Web/Grand.Web.AdminShared/Services/ProductViewModelService.cs` (2571 linii)
* `src/Web/Grand.Web.Vendor/Services/ProductViewModelService.cs` (2381 linii — do usunięcia)

**Zależności:**

* `SEC-015`
* `TEST-023`
* `TEST-025` (testy integracyjne CRUD produktu we wszystkich trzech panelach)

**Kryteria akceptacji:**

* [ ] Jedna implementacja logiki CRUD produktu dla trzech paneli
* [ ] Każdy kontroler pochodny ma poniżej 200 linii
* [ ] `Grand.Web.Vendor/Services/ProductViewModelService.cs` usunięty
* [ ] Testy integracyjne CRUD produktu przechodzą dla wszystkich trzech paneli
* [ ] Wszystkie różnice funkcjonalne między panelami są jawne (parametr zakresu / nadpisana metoda), nie przypadkowe
* [ ] Nota migracyjna dla autorów wtyczek nadpisujących widoki produktu

**Ryzyko:** Wysokie
**Breaking Change:** Tak
**Effort:** XL

---

### ARCH-014 – Rozszerzenie konsolidacji na pozostałe encje

**Cel:**
Powtórzenie wzorca z ARCH-013 dla pozostałych zduplikowanych obszarów zaplecza.

**Zakres:**
Zamówienia, wysyłki, zwroty, recenzje, klienci, kategorie, marki, kolekcje. Cztery pozostałe zduplikowane
serwisy w `Grand.Web.Vendor` do usunięcia.

**Dotknięte projekty:**

* `Grand.Web.AdminShared`, `Grand.Web.Admin`, `Grand.Web.Store`, `Grand.Web.Vendor`

**Dotknięte pliki/klasy:**

* `src/Web/Grand.Web.Vendor/Services/OrderViewModelService.cs`
* `src/Web/Grand.Web.Vendor/Services/ShipmentViewModelService.cs`
* `src/Web/Grand.Web.Vendor/Services/MerchandiseReturnViewModelService.cs`
* `src/Web/Grand.Web.Vendor/Services/VendorReviewViewModelService.cs`
* odpowiadające kontrolery w trzech panelach

**Zależności:**

* `ARCH-013` (wzorzec musi być zweryfikowany na pilocie)

**Kryteria akceptacji:**

* [ ] `Grand.Web.Vendor/Services` nie zawiera serwisów duplikujących `AdminShared`
* [ ] Każdy skonsolidowany obszar ma testy integracyjne dla trzech paneli
* [ ] Sumaryczna liczba linii w `Grand.Web.{Admin,Store,Vendor}/Controllers` spada o min. 50%

**Ryzyko:** Wysokie
**Breaking Change:** Tak
**Effort:** XL

---

### REL-011 – Naprawa pętli sterującej zadań cyklicznych

**Cel:**
Zadanie cykliczne nie może kończyć się na stałe w sytuacji odwracalnej, a błąd nie może przechodzić
bez śladu w logach.

**Zakres:**
Zamiana `break` na oczekiwanie i ponowne sprawdzenie w trzech miejscach. Logowanie w zewnętrznym `catch`
z backoffem wykładniczym. Zachowanie `break` wyłącznie przy zatrzymaniu aplikacji.

**Dotknięte projekty:**

* `Grand.Web.Common`

**Dotknięte pliki/klasy:**

* `src/Web/Grand.Web.Common/Infrastructure/BackgroundServiceTask.cs` → `ExecuteAsync`

**Zależności:**

Brak.

**Kryteria akceptacji:**

* [ ] Wyłączenie i ponowne włączenie zadania w panelu wznawia je bez restartu aplikacji
* [ ] Zadanie wydzierżawione nieistniejącej maszynie wznawia się po zwolnieniu dzierżawy
* [ ] Zadanie niezaseedowane w bazie zaczyna działać po jego dodaniu, bez restartu
* [ ] Każdy wyjątek w pętli produkuje `LogError` z pełnym wyjątkiem
* [ ] Backoff wykładniczy z górnym ograniczeniem przy powtarzalnym błędzie
* [ ] Zamknięcie aplikacji nadal kończy pętlę natychmiast

**Ryzyko:** Niskie
**Breaking Change:** Nie
**Effort:** S

---

### OBS-011 – Health checks sprawdzające zależności

**Cel:**
Orkiestrator ma otrzymywać prawdziwą informację o gotowości instancji.

**Zakres:**
`/health/live` bez zmian (proces żyje). Nowy `/health/ready` sprawdzający MongoDB (`ping`),
Redis (gdy włączony) i stan instalacji bazy, z krótkimi timeoutami.

**Dotknięte projekty:**

* `Grand.Web.Common`

**Dotknięte pliki/klasy:**

* `src/Web/Grand.Web.Common/Infrastructure/ServiceCollectionExtensions.cs` → `AddGrandHealthChecks`
* `src/Web/Grand.Web.Common/Infrastructure/ApplicationBuilderExtensions.cs` → `UseGrandHealthChecks`

**Zależności:**

Brak.

**Kryteria akceptacji:**

* [ ] `/health/ready` zwraca 503, gdy MongoDB jest nieosiągalne
* [ ] `/health/ready` zwraca 503 przed zakończeniem instalacji bazy
* [ ] Check nie trwa dłużej niż 3 sekundy przy niedostępnej zależności
* [ ] `/health/live` zwraca 200 nawet przy niedostępnej bazie (poprawne zachowanie liveness)
* [ ] Redis sprawdzany tylko przy `RedisPubSubEnabled = true`

**Ryzyko:** Niskie
**Breaking Change:** Nie
**Effort:** S

---

### OBS-012 – Metryki i span'y domenowe

**Cel:**
Kluczowe procesy biznesowe mają być widoczne w telemetrii bez sięgania do bazy danych.

**Zakres:**
Centralny `GrandDiagnostics` z `ActivitySource` i `Meter`. Instrumentacja: składanie zamówienia,
przetwarzanie płatności, wyszukiwanie w katalogu, wykonanie zadań cyklicznych, trafienia/pudła cache.
Rejestracja źródeł w `Aspire.ServiceDefaults`.

**Dotknięte projekty:**

* `Grand.Infrastructure`
* `Grand.Business.Checkout`
* `Grand.Business.Catalog`
* `Aspire.ServiceDefaults`

**Dotknięte pliki/klasy:**

* nowy `src/Core/Grand.Infrastructure/Diagnostics/GrandDiagnostics.cs`
* `src/Business/Grand.Business.Checkout/Commands/Handlers/Orders/PlaceOrderCommandHandler.cs`
* `src/Core/Grand.Infrastructure/Caching/MemoryCacheBase.cs`
* `src/Web/Grand.Web.Common/Infrastructure/BackgroundServiceTask.cs`
* `src/Aspire/Aspire.ServiceDefaults/Extensions.cs`

**Zależności:**

* `SEC-014` (żeby nie zwielokrotnić problemu PII)

**Kryteria akceptacji:**

* [ ] Licznik złożonych zamówień z wymiarem `store.id` (nie `customer.id` — kardynalność)
* [ ] Licznik nieudanych płatności z wymiarem metody płatności
* [ ] Histogram czasu składania zamówienia
* [ ] Wskaźnik trafień cache
* [ ] Żaden wymiar metryki nie ma nieograniczonej kardynalności (brak `product.id`, `customer.id`, `order.code` jako wymiaru)
* [ ] Span'y zawierają `order.code` jako atrybut (atrybut span'a, nie wymiar metryki — to jest bezpieczne)
* [ ] Narzut instrumentacji poniżej 2% czasu żądania (pomiar)

**Ryzyko:** Średnie
**Breaking Change:** Nie
**Effort:** M

---

### PLG-011 – Zakres wersji zamiast dokładnego dopasowania

**Cel:**
Wtyczka zbudowana pod jedną wersję minor ma móc działać na kolejnych, jeśli autor to zadeklaruje.

**Zakres:**
`PluginInfoAttribute` dostaje `MinSupportedVersion`/`MaxSupportedVersion`. `PluginManager` porównuje
zakres zamiast równości. Brak deklaracji zakresu zachowuje obecne zachowanie (zgodność wsteczna).

**Dotknięte projekty:**

* `Grand.Infrastructure`

**Dotknięte pliki/klasy:**

* `src/Core/Grand.Infrastructure/Plugins/PluginInfoAttribute.cs`
* `src/Core/Grand.Infrastructure/Plugins/PluginVersionResolver.cs`
* `src/Core/Grand.Infrastructure/Plugins/PluginManager.cs:98`

**Zależności:**

Brak.

**Kryteria akceptacji:**

* [ ] Wtyczka bez deklaracji zakresu zachowuje się dokładnie jak dotąd
* [ ] Wtyczka z zakresem `2.4`–`2.6` ładuje się na 2.5
* [ ] Wtyczka poza zakresem jest oznaczona jako niekompatybilna, nie powoduje błędu startu
* [ ] Powód niekompatybilności widoczny w panelu wtyczek, nie tylko w logu
* [ ] Testy w `Grand.Modules.Tests` pokrywają granice zakresu

**Ryzyko:** Niskie
**Breaking Change:** Nie
**Effort:** S

---

### PLG-012 – Dokument publicznego API wtyczek

**Cel:**
Autor wtyczki ma wiedzieć, na czym może polegać; zespół ma wiedzieć, co może swobodnie refaktoryzować.

**Zakres:**
Dokument definiujący typy i namespace'y stanowiące publiczne API wtyczek, z polityką wersjonowania.
Test architektoniczny weryfikujący, że 16 wtyczek referencyjnych nie sięga poza zadeklarowane API.

**Dotknięte projekty:**

* `docs/`
* nowy `Grand.Architecture.Tests`
* `src/Plugins/**` (poprawki naruszeń)

**Dotknięte pliki/klasy:**

* nowy `docs/architecture/plugin-public-api.md`
* nowy test w `Grand.Architecture.Tests`

**Zależności:**

* `TEST-024` (infrastruktura testów architektonicznych)

**Kryteria akceptacji:**

* [ ] Dokument wymienia dozwolone namespace'y z uzasadnieniem
* [ ] Test zawodzi, gdy wtyczka referencyjna sięga poza API
* [ ] Wszystkie 16 wtyczek referencyjnych przechodzi test (lub naruszenie jest jawnie odnotowane jako dług)
* [ ] Polityka wersjonowania API opisana i powiązana z `PLG-011`

**Ryzyko:** Niskie
**Breaking Change:** Nie
**Effort:** M

---

### PLG-013 – Izolacja wtyczek w AssemblyLoadContext

**Cel:**
Konflikt zależności między wtyczkami nie może psuć aplikacji.

**Zakres:**
Ładowanie wtyczki do dedykowanego `AssemblyLoadContext` z `AssemblyDependencyResolver`. Jawna lista
assembly delegowanych do kontekstu hosta (typy współdzielone: `IPlugin`, encje domenowe, kontrakty).
Wzorzec do zaadaptowania z `ModuleLoader.ModuleLoadContext`.

**Dotknięte projekty:**

* `Grand.Infrastructure`

**Dotknięte pliki/klasy:**

* `src/Core/Grand.Infrastructure/Plugins/PluginManager.cs:247` (`AssemblyLoadContext.Default.LoadFromAssemblyPath`)
* nowy `src/Core/Grand.Infrastructure/Plugins/PluginLoadContext.cs`

**Zależności:**

* `PLG-012` (lista typów współdzielonych wynika z definicji API)

**Kryteria akceptacji:**

* [ ] Dwie wtyczki z różnymi wersjami tej samej biblioteki ładują się i działają jednocześnie (test)
* [ ] DI rozwiązuje usługi wtyczek poprawnie (typy współdzielone pochodzą z kontekstu hosta)
* [ ] Widoki Razor wtyczek nadal się kompilują i renderują
* [ ] Wszystkie 16 wtyczek referencyjnych działa bez zmian
* [ ] Czas startu aplikacji nie rośnie o więcej niż 20%

**Ryzyko:** Wysokie
**Breaking Change:** Częściowo
**Effort:** L

---

### TEST-020 – Infrastruktura testów integracyjnych

**Cel:**
Umożliwić pisanie testów wykonujących pełne żądanie HTTP przeciw prawdziwej bazie danych.

**Zakres:**
Nowy projekt `Grand.IntegrationTests`. `WebApplicationFactory` dla czterech hostów. MongoDB
z kontenera (Testcontainers albo kontener CI, który już istnieje). Izolacja danych między testami.
Konfiguracja replica set dla testów transakcji.

**Dotknięte projekty:**

* nowy `src/Tests/Grand.IntegrationTests`
* `Directory.Packages.props`
* `azure-pipelines.yml`, `.github/workflows/aspnetcore.yml`

**Zależności:**

* `TEST-026` (naprawa niestabilności) — zalecane przed

**Kryteria akceptacji:**

* [ ] Test wykonuje żądanie HTTP przez pełny pipeline i weryfikuje odpowiedź
* [ ] Każdy test dostaje izolowaną bazę (osobna nazwa bazy albo czyszczenie)
* [ ] Testy działają lokalnie i w CI bez ręcznej konfiguracji
* [ ] Czas wykonania całego zestawu poniżej 10 minut
* [ ] Dostępny wariant z replica set dla testów transakcyjnych

**Ryzyko:** Średnie
**Breaking Change:** Nie
**Effort:** L

---

### TEST-021 – Testy integracyjne magazynu i współbieżności

**Cel:**
Zabezpieczyć zachowanie stanów magazynowych przed zmianami DATA-013 i DATA-014.

**Zakres:**
Testy równoległej sprzedaży, rezerwacji, zwrotów i korekt magazynowych, z prawdziwą bazą.

**Dotknięte projekty:**

* `Grand.IntegrationTests`

**Zależności:**

* `TEST-020`

**Kryteria akceptacji:**

* [ ] Test wykrywa lost update w obecnej implementacji (czerwony przed DATA-013)
* [ ] Pokrycie: produkt prosty, produkt z kombinacjami atrybutów, produkt z wieloma magazynami
* [ ] Test rezerwacji i zwolnienia rezerwacji
* [ ] Test sprzedaży poniżej stanu przy `AllowOutOfStockOrders` w obu wariantach

**Ryzyko:** Niskie
**Breaking Change:** Nie
**Effort:** M

---

### TEST-022 – Testy integracyjne składania zamówienia

**Cel:**
Zabezpieczyć zachowanie checkoutu przed rozbiciem `PlaceOrderCommandHandler`.

**Zakres:**
Happy path plus ścieżki błędu: nieudana płatność, brak towaru, nieważny kupon, wygasły bon podarunkowy,
niepoprawny adres. Weryfikacja stanu bazy po każdej ścieżce oraz kolejności publikowanych zdarzeń.

**Dotknięte projekty:**

* `Grand.IntegrationTests`

**Zależności:**

* `TEST-020`

**Kryteria akceptacji:**

* [ ] Pełna ścieżka od koszyka do zamówienia z weryfikacją wszystkich zapisanych dokumentów
* [ ] Minimum 5 ścieżek błędu z weryfikacją stanu bazy
* [ ] Weryfikacja kolejności i zawartości publikowanych zdarzeń
* [ ] Test służy jako charakterystyka obecnego zachowania (golden test) przed DATA-016

**Ryzyko:** Niskie
**Breaking Change:** Nie
**Effort:** M

---

### TEST-023 – Testy integracyjne izolacji najemcy

**Cel:**
Zabezpieczyć izolację vendora i sklepu przed zmianami SEC-015 i ARCH-013/014.

**Zakres:**
Dla każdego kontrolera w `Grand.Web.Vendor` i `Grand.Web.Store` test próbujący dostępu do zasobu
innego najemcy — oczekiwany wynik 403/404, nigdy 200.

**Dotknięte projekty:**

* `Grand.IntegrationTests`

**Zależności:**

* `TEST-020`

**Kryteria akceptacji:**

* [ ] Pokrycie wszystkich 11 kontrolerów `Grand.Web.Vendor` i 36 `Grand.Web.Store`
* [ ] Testy dla GET, POST, DELETE osobno
* [ ] Test wykrywa celowo wprowadzone pominięcie sprawdzenia (weryfikacja przez mutację)
* [ ] Wynik uruchomienia na obecnym kodzie udokumentowany — jeśli ujawni realne luki, trafiają
      do Security Backlog jako osobne pozycje P0

**Ryzyko:** Niskie
**Breaking Change:** Nie
**Effort:** L

---

### TEST-024 – Testy architektoniczne

**Cel:**
Reguły architektoniczne mają być egzekwowane maszynowo, nie w code review.

**Zakres:**
Nowy projekt `Grand.Architecture.Tests`. Reguły: granice modułów biznesowych, macierz dozwolonych
zależności między domenami, obecność atrybutów autoryzacji i zakresu na akcjach paneli, zakaz
`BuildServiceProvider` poza testami, zakaz `.Result`/`.Wait()` poza dozwoloną listą, zakaz referencji
z `Grand.Domain` do warstw wyższych.

**Dotknięte projekty:**

* nowy `src/Tests/Grand.Architecture.Tests`
* `Directory.Packages.props`

**Zależności:**

Brak.

**Kryteria akceptacji:**

* [ ] Reguła: żaden projekt `Grand.Business.*` nie referencuje innego `Grand.Business.*` poza `.Core`
* [ ] Reguła: `Grand.Domain` nie referencuje `Grand.Data` ani `Grand.Infrastructure`
* [ ] Reguła: każda publiczna akcja w panelach ma atrybut autoryzacji
* [ ] Reguła: `BuildServiceProvider()` tylko w projektach testowych
* [ ] Reguła: `.Result`/`.Wait()` tylko na jawnej liście wyjątków z uzasadnieniem
* [ ] Testy uruchamiane w CI na każdym PR
* [ ] Każda reguła ma komunikat błędu wyjaśniający, co i dlaczego jest zabronione

**Ryzyko:** Niskie
**Breaking Change:** Nie
**Effort:** M

---

### TEST-025 – Testy integracyjne CRUD zaplecza

**Cel:**
Zabezpieczyć zachowanie paneli przed konsolidacją ARCH-013/014.

**Zakres:**
Pełny CRUD dla produktu, zamówienia, kategorii, klienta w każdym z trzech paneli, z weryfikacją,
że różnice między panelami są zamierzone.

**Dotknięte projekty:**

* `Grand.IntegrationTests`

**Zależności:**

* `TEST-020`

**Kryteria akceptacji:**

* [ ] CRUD produktu przetestowany w Admin, Store i Vendor
* [ ] Różnice funkcjonalne między panelami udokumentowane w asercjach testu
* [ ] Testy przechodzą przed i po ARCH-013 bez zmian asercji biznesowych

**Ryzyko:** Niskie
**Breaking Change:** Nie
**Effort:** L

---

### TEST-026 – Naprawa niestabilności testów przy równoległym uruchomieniu

**Cel:**
Zielone CI ma znaczyć „zielone".

**Zakres:**
Identyfikacja i izolacja współdzielonego stanu statycznego powodującego losowe błędy
w `Grand.Business.Customers.Tests`, `Grand.Business.Marketing.Tests`, `Grand.Business.Messages.Tests`.
Kandydaci: `DataSettingsManager.Instance`, `PluginManager.ReferencedPlugins`, `AutoMapperConfig`,
`MemoryCacheBase._resetCacheToken`, `PluginPaths.Instance`.

**Dotknięte projekty:**

* `Grand.Business.Customers.Tests`, `Grand.Business.Marketing.Tests`, `Grand.Business.Messages.Tests`
* możliwe zmiany w `Grand.Infrastructure`, `Grand.Data` (uczynienie stanu wstrzykiwalnym)

**Zależności:**

Brak.

**Kryteria akceptacji:**

* [ ] Pełne rozwiązanie przechodzi 20 kolejnych uruchomień równoległych bez błędu
* [ ] Zidentyfikowana i udokumentowana przyczyna każdego wyeliminowanego wyścigu
* [ ] Zmiany w kodzie produkcyjnym (jeśli konieczne) nie zmieniają zachowania runtime

**Ryzyko:** Średnie
**Breaking Change:** Nie
**Effort:** M

---

## 5. Quick Wins

Zmiany o małym nakładzie, niskim ryzyku i realnej wartości. Kosmetyka bez wartości została pominięta
świadomie — refaktoryzacje typu „zmień nazwę klasy" czy „wyodrębnij metodę" nie znajdują się na tej liście,
bo nie zmieniają niczego dla użytkownika ani dla zespołu.

| ID | Zadanie | Effort | Uzasadnienie wartości |
|---|---|---|---|
| **QW-01** | `SEC-014` — zamiana e-maila klienta na `Id` w tagach `Activity` | S | Jedna linia. Usuwa PII z systemów zewnętrznych. Najwyższy stosunek wartości do kosztu w całym dokumencie. |
| **QW-02** | `REL-011` — zamiana `break` na retry w `BackgroundServiceTask` | S | Trzy miejsca. Usuwa klasę cichych awarii wysyłki maili. |
| **QW-03** | `OBS-011` — health check sprawdzający MongoDB | S | Kilkanaście linii. Naprawia współpracę z orkiestratorem. |
| **QW-04** | `PLG-011` — zakres wersji wtyczek zamiast równości | S | Odblokowuje ekosystem wtyczek bez łamania zgodności wstecznej. |
| **QW-05** | `SEC-013` — sprawdzanie `IFormFile.Length` przed materializacją | S | Zamyka tani wektor DoS na trzech publicznych endpointach. |
| **QW-06** | `DATA-011` — atomowy sekwencer numeru zamówienia | S | Usuwa jednocześnie wyścig i blokowanie wątku puli w checkoucie. |
| **QW-07** | Logowanie w zewnętrznym `catch` `BackgroundServiceTask` | S | Część QW-02, ale wartościowa nawet osobno — obecnie błędy znikają bez śladu. |
| **QW-08** | `SizeLimit` dla `IMemoryCache` z konfiguracją | S | Zapobiega nieograniczonemu wzrostowi pamięci. Wymaga ostrożnego doboru wartości. |
| **QW-09** | Usunięcie martwego katalogu `src/Web/Grand.Web.Models` | S | Katalog zawiera wyłącznie `bin`/`obj`, nie ma `.csproj`, nie występuje w `GrandNode.sln`. Myli przy nawigacji. |
| **QW-10** | `ARCH-012` — usunięcie trzech `BuildServiceProvider()` z `OpenApiStartup` | S | Trzy kontenery w trzech kolejnych linijkach; każdy duplikuje wszystkie singletony. |
| **QW-11** | Zamiana blokujących `.Result` w schematach eksportu na wstępne pobranie danych | S | 9 wystąpień, wszystkie w `*SchemaProperty.cs`. Eksport dużego katalogu obecnie blokuje wątki puli. |
| **QW-12** | Test architektoniczny zakazujący `BuildServiceProvider()` poza testami | S | Utrwala QW-10. Część `TEST-024`, ale można wdrożyć samodzielnie. |

**Świadomie NIE w Quick Wins:**

- Zamiana `public virtual` na `public` w 1037 metodach serwisów — `virtual` jest tam celowo, bo umożliwia
  nadpisanie zachowania przez wtyczkę. To nie jest dług.
- Ujednolicenie stylu komentarzy XML — brak wartości.
- Migracja `Grand.Business.Core` na `file`-scoped namespaces itd. — repozytorium już jest spójne, a `.editorconfig`
  to egzekwuje.

---

## 6. Security Backlog

Posortowane P0 → P4. Każda pozycja wskazuje konkretne miejsce w kodzie.

### P0

Brak pozycji zaklasyfikowanych jako P0 w rozumieniu „zdalnie wykorzystywalna luka bez uwierzytelnienia
prowadząca do przejęcia systemu". Najostrzejsze pozycje są P1.

> **Uwaga:** `TEST-023` (testy izolacji najemcy) może ujawnić konkretne, wykorzystywalne wystąpienia IDOR.
> Jeśli tak się stanie, każde z nich trafia tutaj jako osobna pozycja P0. Do czasu wykonania tych testów:
> **Niezweryfikowane – wymaga dodatkowego audytu.**

### P1

| ID | Miejsce w kodzie | Ryzyko |
|---|---|---|
| **SEC-001** / `SEC-011` | `src/Web/Grand.Web.Common/Validators/NoScriptsAttribute.cs` (56 użyć) + 80 `Html.Raw` w `src/Web/Grand.Web/Views/**` | Stored XSS. Vendor lub store manager wstrzykuje skrypt w `FullDescription`; skrypt wykonuje się u klienta (kradzież sesji, przechwycenie formularza płatności) i u administratora w podglądzie produktu (eskalacja do pełnych uprawnień). Czarna lista nie łapie `onfocus=`, `<svg onload\n=>`, `<iframe srcdoc>`, encji HTML. |
| **SEC-002** / `SEC-012` | `src/Web/Grand.Web/App_Data/appsettings.json` sekcja `Security` | Instalacja domyślna bez `X-Frame-Options` (clickjacking), bez `X-Content-Type-Options` (MIME sniffing), bez HSTS, bez wymuszenia HTTPS, z ciasteczkiem sesji bez flagi `Secure`. Przechwycenie sesji w sieci publicznej. |
| **SEC-003** / `SEC-015` | `src/Web/Grand.Web.Vendor/Controllers/**` (88 ręcznych sprawdzeń `VendorId`), `src/Web/Grand.Web.Store/Controllers/**` | IDOR. Brak centralnego egzekwowania zakresu najemcy przy ~370 akcjach. Pominięcie w jednej akcji daje odczyt lub modyfikację danych innego vendora, w tym danych osobowych klientów. |

### P2

| ID | Miejsce w kodzie | Ryzyko |
|---|---|---|
| **SEC-004** / `SEC-013` | `ContactController.UploadFileContactAttribute`, `ProductController.UploadFileProductAttribute`, `ShoppingCartController.UploadFileCheckoutAttribute` | DoS pamięciowy bez uwierzytelnienia — `GetDownloadBits()` materializuje do ~30 MB przed sprawdzeniem limitu. Pusta `ValidationFileAllowedExtensions` = brak walidacji rozszerzenia. Brak rate limitingu. |
| **SEC-005** / `SEC-014` | `src/Web/Grand.Web.Common/Middleware/ContextLoggingMiddleware.cs` | E-mail klienta eksportowany do systemów telemetrii (OTLP, Azure Monitor). Naruszenie minimalizacji danych; niewykonalne prawo do usunięcia. |
| **SEC-016** | `src/Web/Grand.Web.Admin/Controllers/PluginController.cs` → `Upload` | `Assembly.Load(ToByteArray(unzippedEntryStream))` ładuje niezweryfikowaną bibliotekę z przesłanego archiwum do procesu **w trakcie walidacji uploadu** — czyli przed jakąkolwiek decyzją o instalacji. Wykonuje inicjalizatory modułu. Wymaga uprawnień administratora, więc nie jest to eskalacja z zewnątrz, ale jest to wykonanie kodu w momencie, w którym użytkownik oczekuje wyłącznie walidacji. Zalecenie: `MetadataLoadContext` zamiast `Assembly.Load` do odczytu atrybutu. |
| **SEC-017** | `src/Web/Grand.Web.Common/Startup/*` — brak `script-src` w domyślnym CSP | Domyślny CSP (`object-src 'none'; form-action 'self'; frame-ancestors 'none'`) nie zawiera `script-src`, czyli nie ogranicza skutków XSS. Wymaga uporządkowania skryptów inline przed wdrożeniem. Zależne od `SEC-011`. |
| **SEC-018** | `src/Web/Grand.Web.Admin/Controllers/PluginController.cs` → logowanie `CurrentCustomerEmail` | E-mail administratora w komunikatach `LogInformation`. Ta sama klasa problemu co SEC-005, mniejsza skala. |
| **ARCH-003** / `ARCH-011` | `Program.cs` × 4 — `ValidateScopes = false` | Captive dependency na `IWorkContext`/`IStoreContext` byłaby wyciekiem tożsamości między sesjami i nie zostałaby wykryta. Obecnie **niezweryfikowane, czy takie zależności istnieją** — `ARCH-011` jest zadaniem weryfikującym. |

### P3

| ID | Miejsce w kodzie | Ryzyko |
|---|---|---|
| **SEC-019** | `src/Business/Grand.Business.Common/Services/Security/PermissionService.cs` → `AuthorizeAction` | Semantyka „deny wins": jeśli klient należy do dwóch grup, a jedna ma rekord odmowy dla akcji, wynik jest odmowny mimo zezwolenia w drugiej grupie. Zachowanie może być zamierzone, ale nie jest udokumentowane ani pokryte testem. **Niezweryfikowane – wymaga potwierdzenia intencji.** |
| **SEC-020** | `src/Modules/Grand.Module.Api/Attributes/EnableQueryAttribute.cs` | `$filter` i `$orderby` mogą trafić na nieindeksowane pola, powodując pełne skanowanie kolekcji. Parser jest dobrze zabezpieczony (`ApiQueryOptions` — whitelist członków, limit 512 znaków, `ParsingConfig` bez `new` i bez rozwiązywania typów), więc nie jest to injection — jest to ryzyko DoS przez kosztowne zapytanie. Zalecenie: limit czasu wykonania zapytania (`maxTimeMS`). |
| **SEC-021** | `src/Web/Grand.Web.Admin/Controllers/ElFinderController.cs` | Menedżer plików oparty na zewnętrznej bibliotece `elFinder.Net` (przypięta wersja HTTP — patrz commit #763). Antiforgery naprawione w #765. Powierzchnia ataku wymaga osobnego przeglądu. **Niezweryfikowane – wymaga dodatkowego audytu.** |
| **SEC-022** | `src/Core/Grand.Infrastructure/Roslyn/RoslynCompiler.cs` | Kompilacja skryptów C# z katalogu w czasie startu, gdy `Extensions.UseRoslynScripts = true`. Domyślnie wyłączone, więc ryzyko niskie — ale włączenie oznacza wykonanie dowolnego kodu z systemu plików. Zalecenie: wyraźne ostrzeżenie w logu przy włączeniu. |

### P4

| ID | Miejsce w kodzie | Ryzyko |
|---|---|---|
| **SEC-023** | `src/Core/Grand.Infrastructure/Plugins/PluginManager.cs` | Brak weryfikacji podpisu wtyczki. Instalacja wtyczki z niezaufanego źródła to pełna kompromitacja — ale to jest świadoma decyzja modelu wtyczek, a nie defekt. Podpisywanie wtyczek to funkcja produktowa, nie poprawka. **Nice to have.** |
| **SEC-024** | Cały kod uwierzytelniania | Brak wsparcia dla WebAuthn/passkeys. 2FA (TOTP) jest zaimplementowane. **Nice to have.** |

---

## 7. Data & Persistence Backlog

### EF Core

**GrandNode nie używa Entity Framework Core.** W `Directory.Packages.props` nie ma żadnego pakietu
`Microsoft.EntityFrameworkCore.*`. Warstwa danych to MongoDB (`MongoDB.Driver` 3.10.0) z alternatywnym
sterownikiem LiteDB 5.0.21.

To jest **poprawna decyzja i nie należy jej zmieniać.** Model danych GrandNode jest głęboko zagnieżdżony
(`Product` zawiera `ProductAttributeCombinations`, `ProductWarehouseInventory`, `TierPrices`, `ProductPictures`
jako dokumenty podrzędne), co jest naturalne dla bazy dokumentowej i bolesne w relacyjnej. Migracja na
EF Core oznaczałaby przepisanie całego modelu domenowego, wszystkich zapytań i wszystkich migracji —
przy zerowym zysku funkcjonalnym.

Wszystkie pozycje poniżej dotyczą MongoDB i LiteDB.

### MongoDB / Repositories / IQueryable

| ID | Priorytet | Pozycja | Miejsce |
|---|---|---|---|
| **DATA-011** | P0 | Atomowy sekwencer numeru zamówienia | `OrderService.InsertOrder` |
| **DATA-012** | P0 | Unikalny indeks na `Order.OrderNumber` | `InstallationService.CreateIndexes` + migracja |
| **DATA-013** | P0 | Atomowe modyfikacje stanów magazynowych (`IncField` / `UpdateOneAsync`) | `InventoryManageService` |
| **DATA-018** | P2 | Usunięcie konstruktora `MongoRepository(IAuditInfoProvider)` tworzącego własny `MongoClient` | `src/Core/Grand.Data/Mongo/MongoRepository.cs:33-36` — konstruktor tworzy `new MongoClient(connectionString)` per instancja repozytorium. DI wybiera konstruktor z `IMongoDatabase`, więc obecnie nieużywany, ale jest to pułapka: zmiana rejestracji albo użycie w teście da eksplozję puli połączeń. Commit #757 rozwiązał ten problem dla ścieżki DI; konstruktor pozostał. |
| **DATA-019** | P2 | Pozostałe synchroniczne wykonania zapytań LINQ | 14 wystąpień `.Table.ToList()` / `.Table.FirstOrDefault()` / `.Table.Count()`. Większość w `Grand.Module.Installer` i `Grand.Module.Migration` (ścieżki jednorazowe, akceptowalne). Do naprawy: `MerchandiseReturnService.cs:158`, `ShipmentService.cs:132` (ścieżki żądania). |
| **DATA-020** | P3 | Brak projekcji w zapytaniach listowych | Zapytania listowe pobierają pełne dokumenty `Product` (z zagnieżdżonymi kolekcjami) tam, gdzie potrzeba kilku pól. **Potential** — wymaga pomiaru, które listy są kosztowne. Zależne od `OBS-012`. |

### DbContext

`IDatabaseContext` (`src/Core/Grand.Data/IDatabaseContext.cs`) nie jest odpowiednikiem `DbContext` —
nie śledzi zmian, nie ma jednostki pracy, służy głównie do tworzenia indeksów i operacji na bazie.
Repozytoria są rejestrowane jako `Scoped` i pobierają `IMongoDatabase` (również `Scoped`), a `IMongoClient`
jest singletonem — to jest poprawny układ dla sterownika MongoDB.

**Brak zmian rekomendowanych.** Wprowadzanie warstwy śledzenia zmian byłoby odtwarzaniem EF Core na
bazie dokumentowej — antywzorzec.

### Transactions

| ID | Priorytet | Pozycja |
|---|---|---|
| **DATA-015** | P1 | Opcjonalne wsparcie sesji/transakcji w `IRepository<T>` i `IDatabaseContext`, aktywne tylko na replica set |
| **DATA-017** | P1 | Objęcie fazy utrwalenia zamówienia transakcją |
| **DATA-021** | P3 | Wzorzec Outbox dla efektów ubocznych nietransakcyjnych (mail, webhook). Obecnie `QueuedEmail` pełni tę rolę dla maili i robi to dobrze; brakuje tego dla wywołań webhooków wtyczek. **Nice to have.** |

### Concurrency

| ID | Priorytet | Pozycja |
|---|---|---|
| **DATA-014** | P1 | Znacznik współbieżności w `BaseEntity` + optimistic concurrency w `IRepository<T>` |
| **DATA-022** | P2 | Rozproszona blokada dla operacji, które muszą być globalnie serializowane. Obecnie jedyny mechanizm to `TryClaimTaskRun` dla zadań cyklicznych (poprawny). Brak ogólnego mechanizmu — potrzebny np. przy przeliczaniu drzewa kategorii. **Potential.** |

### Indexes

| ID | Priorytet | Pozycja |
|---|---|---|
| **DATA-023** | P2 | Indeksy tworzone wyłącznie w `InstallationService.CreateIndexes` (177 wywołań `CreateIndex`). Nowy indeks dla istniejącej instalacji wymaga ręcznej migracji, a brak indeksu nie jest w żaden sposób sygnalizowany. Rekomendacja: deklaratywna definicja indeksów obok encji + weryfikacja przy starcie (log ostrzegawczy o brakujących), zamiast rozproszonej listy w instalatorze. |
| **DATA-024** | P2 | Brak indeksu na `Order.OrderNumber` (patrz `DATA-012`) |
| **DATA-025** | P3 | Przegląd pokrycia indeksami zapytań raportowych (`OrderReportService`, `ProductsReportService`). **Potential — wymaga pomiaru na realnych danych.** |

### Queries

| ID | Priorytet | Pozycja |
|---|---|---|
| **DATA-026** | P2 | Limit czasu wykonania zapytań API (`maxTimeMS`) dla `$filter`/`$orderby` na nieindeksowanych polach — patrz `SEC-020` |
| **DATA-027** | P3 | Audyt N+1 w ścieżkach renderowania storefrontu. `GetProductDetailsPageHandler` (1188 linii) pobiera dane z wielu serwisów w pętlach. **Potential — wymaga profilowania, nie zgadywania.** |

### Caching

Obecny mechanizm (`ICacheBase` z `MemoryCacheBase` / `RedisMessageCacheManager`, `SemaphoreSlim` per klucz,
stałe kluczy w `Grand.Infrastructure/Caching/Constants/`, inwalidacja przez `EntityCacheEvent`
i Redis pub/sub między instancjami) jest **dobrze zaprojektowany i nie wymaga przebudowy.**

| ID | Priorytet | Pozycja |
|---|---|---|
| **DATA-028** | P2 | Brak `SizeLimit` w `IMemoryCache` — cache może rosnąć bez ograniczenia. Wymaga przypisania rozmiarów wpisom, więc nie jest to jednolinijkowa zmiana. **Confirmed** (brak konfiguracji `SizeLimit` w `AddMemoryCache`). |
| **DATA-029** | P3 | `MemoryCacheBase.RemoveByPrefix` iteruje `CacheEntries` (`ConcurrentDictionary`) przy każdym wywołaniu. Przy dużym cache i częstej inwalidacji to koszt O(n). **Potential — wymaga pomiaru.** |
| **DATA-030** | P3 | Metryki trafień/pudeł cache — część `OBS-012`. Bez nich nie da się ocenić, czy cache w ogóle działa. |

### Data consistency

| ID | Priorytet | Pozycja |
|---|---|---|
| **DATA-031** | P2 | Brak narzędzia wykrywającego niespójności między kolekcjami (zamówienie bez pozycji, płatność bez zamówienia, rezerwacja bez zamówienia). Rekomendacja: zadanie diagnostyczne uruchamiane na żądanie z panelu, raportujące anomalie — nie naprawiające ich automatycznie. |
| **DATA-032** | P3 | Denormalizowane pola (`Product.StockQuantity` jako suma `ProductWarehouseInventory`, `Product.LowStock`) mogą się rozjechać ze źródłem. Rekomendacja: zadanie rekoncyliacyjne. **Potential.** |

---

## 8. Testing Backlog

Priorytet mają testy **zabezpieczające istniejące zachowanie** przed dużymi zmianami architektonicznymi.
Testy zwiększające pokrycie „dla pokrycia" są niżej.

### Unit Tests

Stan obecny: 1826 metod, 21 projektów, dobre pokrycie warstwy biznesowej. Główny problem to niestabilność
i luka w warstwie webowej.

| ID | Priorytet | Pozycja |
|---|---|---|
| **TEST-026** | P1 | Naprawa niestabilności przy równoległym uruchomieniu (`Customers`, `Marketing`, `Messages`) — współdzielony stan statyczny |
| **TEST-030** | P2 | Testy jednostkowe dla `Grand.Web.Common` — obecnie 15 testów na 161 plików. Priorytet: filtry autoryzacji, `ContextMiddleware`, `ViewLocationExpander`, tag helpery formularzy |
| **TEST-031** | P2 | Testy jednostkowe dla sanityzacji HTML (`SEC-011`) — zestaw min. 20 payloadów XSS |
| **TEST-032** | P3 | Testy jednostkowe dla `ApiQueryOptions` — parser jest dobrze napisany, ale krytyczny dla bezpieczeństwa; zasługuje na pełne pokrycie przypadków brzegowych |

### Integration Tests

**Kategoria o najwyższym priorytecie w całym dokumencie.** Obecnie: zero.

| ID | Priorytet | Pozycja |
|---|---|---|
| **TEST-020** | P1 | Infrastruktura: `WebApplicationFactory` + MongoDB z kontenera + izolacja danych + wariant replica set |
| **TEST-022** | P1 | Składanie zamówienia — happy path + 5 ścieżek błędu, jako golden test przed `DATA-016` |
| **TEST-023** | P1 | Izolacja najemcy — wszystkie kontrolery `Vendor` i `Store`, przed `SEC-015` i `ARCH-013` |
| **TEST-021** | P1 | Magazyn i współbieżność — przed `DATA-013` |
| **TEST-025** | P2 | CRUD zaplecza w trzech panelach — przed `ARCH-013`/`ARCH-014` |
| **TEST-033** | P2 | Kontener DI — zbudowanie pełnego kontenera z walidacją i rozwiązanie wszystkich usług (uzupełnia `ARCH-011`) |
| **TEST-034** | P2 | Ładowanie wtyczek — weryfikacja, że 16 wtyczek referencyjnych ładuje się, rejestruje usługi i renderuje widoki. Konieczne przed `PLG-013`. |
| **TEST-035** | P3 | Migracje — uruchomienie pełnej ścieżki migracji na bazie zainstalowanej w poprzedniej wersji |

### Architecture Tests

Obecnie: zero. Najtańsza kategoria o największym zwrocie długoterminowym.

| ID | Priorytet | Pozycja |
|---|---|---|
| **TEST-024** | P1 | Projekt bazowy + reguły: granice modułów, zakaz `BuildServiceProvider`, zakaz `.Result`/`.Wait()`, kierunek zależności `Grand.Domain` |
| **TEST-036** | P1 | Reguła: każda publiczna akcja w panelach ma atrybut autoryzacji |
| **TEST-024b** | P2 | Reguła: każda akcja w `Vendor`/`Store` ma atrybut zakresu danych (po `SEC-015`) |
| **TEST-037** | P2 | Reguła: macierz dozwolonych zależności między domenami biznesowymi (`ARCH-002`) |
| **TEST-038** | P2 | Reguła: wtyczki referencyjne nie sięgają poza publiczne API (`PLG-012`) |
| **TEST-039** | P3 | Reguła: encje w `Grand.Domain` nie mają zależności od `Grand.Data` ani `Grand.Business.*` |

### API Tests

| ID | Priorytet | Pozycja |
|---|---|---|
| **TEST-040** | P2 | Testy kontraktowe `Grand.Module.Api` — kształt odpowiedzi, kody statusu, obsługa `$filter`/`$select`/`$top`/`$skip`. Obecnie 30 testów na cały moduł. |
| **TEST-041** | P2 | Testy autoryzacji API — `AuthorizeApiAdminAttribute`, weryfikacja JWT, ograniczenie IP, `IgnoreFilter` |
| **TEST-042** | P3 | Weryfikacja zgodności wygenerowanego dokumentu OpenAPI z rzeczywistymi endpointami |

### Security Tests

| ID | Priorytet | Pozycja |
|---|---|---|
| **TEST-043** | P1 | Zestaw payloadów XSS przeciw wszystkim polom oznaczonym `[NoScripts]` (obecnie 56) — powinien być czerwony przed `SEC-011` |
| **TEST-044** | P1 | Testy IDOR — element `TEST-023`, wyodrębniony jako osobna kategoria raportowania |
| **TEST-045** | P2 | Testy antiforgery — weryfikacja, że każda akcja POST w panelach odrzuca żądanie bez tokenu |
| **TEST-046** | P2 | Testy uploadu — limity rozmiaru, walidacja rozszerzeń, rate limiting (`SEC-013`) |
| **TEST-047** | P3 | Testy siły hasła i procesu logowania — blokada konta, historia haseł, upgrade SHA1 → PBKDF2 |

### End-to-End Tests

Obecnie: zero. **Świadomie najniższy priorytet.** Testy E2E są najdroższe w utrzymaniu i najbardziej
niestabilne; wprowadzenie ich przed ustabilizowaniem testów integracyjnych pogłębiłoby problem
z zaufaniem do CI.

| ID | Priorytet | Pozycja |
|---|---|---|
| **TEST-048** | P3 | Ścieżka zakupowa: katalog → produkt → koszyk → checkout → potwierdzenie. Jeden scenariusz, dobrze utrzymywany, jest wart więcej niż dwadzieścia niestabilnych. |
| **TEST-049** | P3 | Ścieżka instalacji — od pustej bazy do działającego sklepu. Obecnie weryfikowana wyłącznie ręcznie, a jest to ścieżka, którą przechodzi każdy nowy użytkownik. |
| **TEST-050** | P4 | Ścieżki panelu administracyjnego w przeglądarce. **Nice to have** — testy integracyjne pokrywają logikę, E2E pokrywałoby głównie JavaScript. |

---

## 9. Observability & Operations Backlog

### Logging

Stan obecny: standardowe `ILogger<T>` z logowaniem strukturalnym, poprawnie stosowane
(`logger.LogInformation("Task {TaskName} execute", Name)` — nie interpolacja stringów). Poziomy logowania
konfigurowalne przez `appsettings.json`.

| ID | Priorytet | Pozycja |
|---|---|---|
| **OBS-020** | P2 | Logowanie w `catch (Exception)` w `BackgroundServiceTask` — obecnie błąd znika bez śladu (część `REL-011`) |
| **OBS-021** | P2 | Przegląd 6 wystąpień `catch (Exception)` pod kątem połykania błędów bez logu |
| **OBS-022** | P2 | Usunięcie PII z komunikatów logów (`PluginController` loguje `CurrentCustomerEmail`) — patrz `SEC-018` |
| **OBS-023** | P3 | Ustandaryzowanie identyfikatorów zdarzeń (`EventId`) dla zdarzeń operacyjnych, żeby dało się na nie alertować |

### Metrics

| ID | Priorytet | Pozycja |
|---|---|---|
| **OBS-012** | P2 | `Meter` z licznikami domenowymi: zamówienia, nieudane płatności, nieudane logowania, trafienia cache, czas zadań cyklicznych |
| **OBS-024** | P2 | Kontrola kardynalności — żaden wymiar metryki nie może przyjmować nieograniczonej liczby wartości (`product.id`, `customer.id`, `order.code` są zabronione jako wymiary) |
| **OBS-025** | P3 | Metryki wtyczek — czas wykonania providerów płatności i wysyłki, z wymiarem `provider.systemName`. Bardzo przydatne przy diagnozie „sklep wolno działa" wynikającej z wolnej bramki. |

### Tracing

| ID | Priorytet | Pozycja |
|---|---|---|
| **OBS-012** | P2 | `ActivitySource` w ścieżkach: składanie zamówienia, płatność, wyszukiwanie w katalogu |
| **SEC-014** | P2 | Usunięcie e-maila z tagów `Activity` |
| **OBS-026** | P3 | Instrumentacja MongoDB — sterownik 3.x wspiera `DiagnosticsActivityEventSubscriber`; zapytania do bazy jako span'y potomne dałyby natychmiastową odpowiedź na „które zapytanie jest wolne" |
| **OBS-027** | P3 | Próbkowanie — przy pełnym ruchu produkcyjnym 100% trace'ów jest nie do utrzymania kosztowo |

### OpenTelemetry

Stan obecny: `Aspire.ServiceDefaults` konfiguruje poprawnie logi, metryki i trace'y, z eksportem OTLP
i Azure Monitor. **Szkielet jest dobry** — problem polega wyłącznie na tym, że nikt na nim nie budował.

| ID | Priorytet | Pozycja |
|---|---|---|
| **OBS-028** | P2 | Rejestracja własnych `ActivitySource` i `Meter` w `ConfigureOpenTelemetry` (bez tego metryki z `OBS-012` nie będą eksportowane) |
| **OBS-029** | P3 | Atrybuty zasobu — `service.name` jest ustawiane tylko w gałęzi Azure Monitor; powinno być globalne, wraz z `service.version` (dostępne przez `GrandVersion.FullVersion`) i `service.instance.id` |

### Health checks

| ID | Priorytet | Pozycja |
|---|---|---|
| **OBS-011** | P2 | `/health/ready` sprawdzający MongoDB, Redis i stan instalacji |
| **OBS-030** | P3 | Health check wtyczek — czy wszystkie zainstalowane wtyczki załadowały się poprawnie. Obecnie niekompatybilna wtyczka jest tylko logowana przy starcie. |

### Diagnostics

| ID | Priorytet | Pozycja |
|---|---|---|
| **OBS-031** | P3 | Strona diagnostyczna w panelu: wersja, gałąź i commit (`GrandVersion` już to udostępnia), stan wtyczek, stan zadań cyklicznych, stan połączeń z bazą i Redis. Obecnie te informacje są rozrzucone. |
| **OBS-032** | P3 | Endpoint zrzutu konfiguracji efektywnej (z zamaskowanymi sekretami) — najczęstsza przyczyna zgłoszeń „u mnie nie działa" to różnica w konfiguracji |

### Correlation

| ID | Priorytet | Pozycja |
|---|---|---|
| **OBS-033** | P2 | Propagacja `TraceId` do odpowiedzi błędu — użytkownik zgłaszający problem podaje identyfikator, po którym da się znaleźć trace. `GrandExceptionHandler` jest właściwym miejscem. |
| **OBS-034** | P3 | Korelacja zadań cyklicznych — każde uruchomienie zadania powinno tworzyć własny `Activity` jako korzeń, żeby dało się prześledzić pojedyncze wykonanie |

### Production troubleshooting

| ID | Priorytet | Pozycja |
|---|---|---|
| **OBS-035** | P3 | Dokument runbook: jak zdiagnozować zawieszony checkout, jak sprawdzić stan zadań, jak wymusić czyszczenie cache, jak zweryfikować synchronizację cache między instancjami |
| **OBS-036** | P3 | Dynamiczna zmiana poziomu logowania bez restartu — `IOptionsMonitor` na sekcji `Logging` częściowo to umożliwia, ale nie jest to udokumentowane ani przetestowane |

---

## 10. Plugin Architecture Backlog

Jeden z najważniejszych obszarów produktu — ekosystem wtyczek jest realną wartością GrandNode
i jednocześnie największym ograniczeniem swobody refaktoryzacji.

### Plugin boundaries

| ID | Priorytet | Pozycja |
|---|---|---|
| **PLG-020** | P2 | `src/Plugins/Theme.Modern/Theme.Modern.csproj` referencuje `Grand.Web.csproj` — motyw zależy od całej aplikacji storefrontu, co czyni każdą zmianę w `Grand.Web` potencjalnie łamiącą motyw. Rekomendacja: wydzielić z `Grand.Web` to, czego motyw faktycznie potrzebuje (modele widoków, komponenty), albo udokumentować tę zależność jako świadomą. |
| **PLG-021** | P2 | Brak granicy zaufania — wtyczka ma dostęp do `IRepository<T>` każdej encji, do pełnej konfiguracji i do systemu plików. To jest **świadomy model** (wtyczka to kod zaufany), ale powinien być jawnie udokumentowany, żeby nikt nie zakładał izolacji, której nie ma. |
| **PLG-022** | P3 | `PluginManager.RegisterPluginInterface` rejestruje **każdy** typ implementujący `IPlugin` jako `Scoped` w kontenerze hosta, bez kontroli. Wtyczka może w ten sposób nadpisać rejestrację. **Potential** — wymaga weryfikacji, czy kolejność rejestracji na to pozwala. |

### Public plugin API

| ID | Priorytet | Pozycja |
|---|---|---|
| **PLG-012** | P2 | Dokument definiujący publiczne API wtyczek (typy, namespace'y, polityka wersjonowania) |
| **PLG-023** | P2 | Test architektoniczny weryfikujący, że wtyczki referencyjne nie sięgają poza API (`TEST-038`) |
| **PLG-024** | P3 | Rozważenie `[InternalsVisibleTo]` / atrybutów oznaczających API niepubliczne, żeby granica była widoczna w IDE, nie tylko w dokumencie |

### Plugin dependencies

| ID | Priorytet | Pozycja |
|---|---|---|
| **PLG-025** | P3 | Brak deklaracji zależności między wtyczkami — wtyczka nie może zadeklarować „wymagam `Shipping.ByWeight`". Kolejność instalacji i ładowania jest nieokreślona. **Nice to have**, ale rośnie w znaczeniu wraz z ekosystemem. |
| **PLG-026** | P3 | Rozwiązywanie zależności NuGet wtyczki — `Grand.Plugin.props` rozwiązuje problem kopiowania (`CopyLocalLockFileAssemblies` + `ExcludeAssets=runtime`, dobrze udokumentowane), ale konflikt wersji między dwiema wtyczkami pozostaje nierozwiązywalny bez `PLG-013`. |

### Plugin lifecycle

| ID | Priorytet | Pozycja |
|---|---|---|
| **PLG-027** | P2 | Instalacja i odinstalowanie wtyczki wymaga `applicationLifetime.StopApplication()` — w środowisku bez automatycznego restartu (IIS bez recyklingu, `dotnet run`) oznacza to ręczną interwencję. Minimum: wyraźne ostrzeżenie w panelu przed operacją. |
| **PLG-028** | P3 | Brak haka `Update` w `IPlugin` — jest `Install` i `Uninstall`, ale aktualizacja wtyczki między wersjami nie ma dedykowanego punktu wejścia. Autorzy obchodzą to migracjami. **Nice to have.** |
| **PLG-029** | P3 | `PluginManager.Load` rzuca wyjątek przerywający start całej aplikacji, gdy jedna wtyczka zawiedzie (`throw fail` w bloku `catch`). Jedna wadliwa wtyczka blokuje sklep. Rekomendacja: oznaczenie wtyczki jako niesprawnej i kontynuacja startu, z widocznym komunikatem w panelu. **To jest ważniejsze niż sugeruje priorytet P3 — do rozważenia jako P2.** |

### Version compatibility

| ID | Priorytet | Pozycja |
|---|---|---|
| **PLG-011** | P2 | Zakres wersji zamiast dokładnej równości `Major.Minor` |
| **PLG-030** | P3 | Komunikat o niekompatybilności widoczny w panelu z podaniem oczekiwanej i faktycznej wersji — obecnie tylko `LogInformation` przy starcie |

### Security

| ID | Priorytet | Pozycja |
|---|---|---|
| **SEC-016** | P2 | `Assembly.Load` przesłanego archiwum w trakcie walidacji uploadu — zamienić na `MetadataLoadContext` |
| **SEC-023** | P4 | Podpisywanie wtyczek. **Nice to have** — funkcja produktowa, nie poprawka. |
| **PLG-031** | P3 | Weryfikacja zawartości archiwum wtyczki przed rozpakowaniem — `ZipFile.ExtractToDirectory` w .NET jest odporne na zip-slip, ale manipulacja wpisami archiwum w trybie `Update` (`archive.CreateEntry($"{_path}/{y.Name}")`) używa nazw z archiwum. **Niezweryfikowane – wymaga dodatkowego audytu.** |

### Isolation

| ID | Priorytet | Pozycja |
|---|---|---|
| **PLG-013** | P3 | Izolacja w `AssemblyLoadContext` z `AssemblyDependencyResolver` |
| **PLG-032** | P4 | Wyładowywanie wtyczek bez restartu — pochodna `PLG-013`. **Nice to have.** |

### Extensibility

Stan obecny jest dobry: `IProvider` z `SystemName`, `Priority`, `LimitedToStores`, `LimitedToGroups`;
wyspecjalizowane interfejsy dla płatności, wysyłki, podatków, rabatów, widżetów, uwierzytelniania;
`IStartupApplication` do rejestracji usług; `IThemeView` i `ViewLocationExpander` dla motywów;
zdarzenia domenowe przez `Grand.Mediator`; `MessageTokensAddedEvent` dla szablonów wiadomości.

| ID | Priorytet | Pozycja |
|---|---|---|
| **PLG-033** | P3 | Brak punktu rozszerzenia dla webhooków wychodzących — wtyczki integracyjne muszą subskrybować zdarzenia domenowe i same zarządzać ponowieniami. Powiązane z `DATA-021` (outbox). **Nice to have.** |
| **PLG-034** | P3 | Brak sposobu na rozszerzenie schematu encji przez wtyczkę inaczej niż przez `UserFields`. Mechanizm `UserFields` działa i jest właściwym rozwiązaniem dla bazy dokumentowej — problemem jest brak dokumentacji, że to jest **zamierzona** droga. |

### Backward compatibility

| ID | Priorytet | Pozycja |
|---|---|---|
| **PLG-035** | P2 | Brak polityki deprecjacji — w całym repozytorium jest **jeden** atrybut `[Obsolete]`. Zmiany API są wprowadzane bez okresu przejściowego. Rekomendacja: `[Obsolete]` z informacją o wersji usunięcia, minimum jedno wydanie minor przed usunięciem. |
| **PLG-036** | P2 | Brak dokumentu migracji wtyczek między wersjami. `ARCH-013`/`ARCH-014` **wymagają** takiego dokumentu — bez niego konsolidacja paneli po cichu zepsuje wtyczki nadpisujące widoki zaplecza. |

---

## 11. Performance Backlog

Każda pozycja jest oznaczona jako **Confirmed** (potwierdzona w kodzie) albo **Potential** (ryzyko
teoretyczne, wymagające pomiaru). Optymalizacje bez uzasadnienia nie znalazły się na liście.

**Zasada nadrzędna:** wszystko oznaczone jako **Potential** wymaga najpierw `OBS-012` (metryki).
Optymalizowanie bez pomiaru to zgadywanie.

### Confirmed

| ID | Pozycja | Dowód | Rekomendacja | Effort |
|---|---|---|---|---|
| **PERF-001** | Synchroniczne zapytanie LINQ w ścieżce checkoutu | `OrderService.InsertOrder:213` — `.FirstOrDefault()` bez `Async` na kolekcji `Order`. Blokuje wątek puli przy każdym zamówieniu, a czas rośnie z liczbą zamówień w bazie. | `DATA-011` (sekwencer) usuwa to zapytanie całkowicie | S |
| **PERF-002** | Cztery round-tripy zamiast jednego przy aktualizacji stanu magazynowego | `InventoryManageService.UpdateStockProduct:708-711` — cztery `UpdateField` na tym samym dokumencie | `DATA-013` — jedno `UpdateOneAsync` z `UpdateBuilder` | S |
| **PERF-003** | Blokujące `.Result` w schematach eksportu | 9 wystąpień: `BrandSchemaProperty.cs:28`, `CategorySchemaProperty.cs:29`, `CollectionSchemaProperty.cs:28`, `ProductSchemaProperty.cs:121-123`, `OrderSchemaProperty.cs:57,72`, `AddressSchemaProperty.cs:29`. Wywoływane **per wiersz** eksportu — eksport 10 000 produktów to 30 000 blokujących wywołań. | Pobrać dane pomocnicze (obrazy, kraje) hurtowo przed budową schematu i przekazać jako słownik | S |
| **PERF-004** | `IMemoryCache` bez `SizeLimit` | Brak wywołania `AddMemoryCache` z konfiguracją i brak `SizeLimit` w całym repozytorium. Cache rośnie do wyczerpania pamięci procesu; jedyne ograniczenie to czas wygaśnięcia (`DefaultCacheTimeMinutes: 60`). | Ustawić `SizeLimit` i przypisać rozmiary wpisom. Wymaga rozwagi — źle dobrany limit degraduje wydajność bardziej niż jego brak. Poprzedzić `OBS-012`. | M |
| **PERF-005** | Materializacja pliku przed sprawdzeniem limitu rozmiaru | `ContactController.cs:151`, `ProductController.cs:418`, `ShoppingCartController.cs` — `GetDownloadBits()` przed sprawdzeniem `ValidationFileMaximumSize`. Alokacje na Large Object Heap. | `SEC-013` | S |
| **PERF-006** | Synchroniczne zapytania w ścieżkach żądania poza instalatorem | `MerchandiseReturnService.cs:158`, `ShipmentService.cs:132` — `.Table.FirstOrDefault()`. Pozostałe 12 wystąpień jest w `Grand.Module.Installer` i `Grand.Module.Migration`, czyli ścieżkach jednorazowych — tam jest to akceptowalne. | Zamienić na `FirstOrDefaultAsync` z `IRepository<T>` | S |
| **PERF-007** | Duplikacja singletonów przez `BuildServiceProvider()` | `OpenApiStartup.cs:36-38` (trzy kontenery), `PluginManager.cs:58`, `ModuleLoader.cs:108`, `StartupApplication.cs:127`. Każdy tworzy własne kopie wszystkich zarejestrowanych do tego momentu singletonów, nigdy nie zwalniane. | `ARCH-012` | S |

### Potential

| ID | Pozycja | Dlaczego to tylko ryzyko | Co zmierzyć przed działaniem |
|---|---|---|---|
| **PERF-010** | N+1 w renderowaniu strony produktu | `GetProductDetailsPageHandler` ma 1188 linii i sięga do wielu serwisów. Nie znaczy to jeszcze, że wykonuje zapytania w pętli — większość danych jest cache'owana. | Profilowanie liczby zapytań do MongoDB na jedno wyświetlenie strony produktu (`OBS-026`) |
| **PERF-011** | Kosztowne zapytania API na nieindeksowanych polach | `$filter` i `$orderby` dopuszczają dowolne pole modelu (`ApiQueryOptions` sprawdza tylko, czy pole *istnieje*, nie czy jest indeksowane). Pełne skanowanie kolekcji jest możliwe. Realność zależy od tego, kto ma dostęp do API. | Logowanie wolnych zapytań MongoDB; rekomendacja niezależna od pomiaru: limit `maxTimeMS` (`DATA-026`) |
| **PERF-012** | Koszt `RemoveByPrefix` w `MemoryCacheBase` | Iteracja po `ConcurrentDictionary` przy każdym wywołaniu, O(n) względem liczby kluczy. Przy dużym cache i częstej inwalidacji (edycja produktu czyści prefiks) może być zauważalne. | Rozmiar `CacheEntries` i częstość `RemoveByPrefix` w produkcji |
| **PERF-013** | Brak projekcji w zapytaniach listowych | Listy pobierają pełne dokumenty `Product` z zagnieżdżonymi kolekcjami (`ProductAttributeCombinations`, `TierPrices`, `ProductPictures`), gdy potrzeba kilku pól. Wielkość dokumentu produktu w realnym sklepie potrafi przekraczać 100 kB. | Rozmiar dokumentów i wolumen transferu z bazy dla list katalogowych i list w panelu |
| **PERF-014** | Publikowanie zdarzeń synchronicznie w ścieżce żądania | `Grand.Mediator.Publish` wywołuje handlery sekwencyjnie, w tym samym wątku i tej samej transakcji logicznej. Wolny handler (np. wtyczka wysyłająca webhook) spowalnia żądanie użytkownika. Obecnie brak dowodu, że którykolwiek handler jest wolny. | Czas wykonania handlerów notyfikacji (`OBS-025`) |
| **PERF-015** | Zadania cykliczne w procesie webowym | Wszystkie zadania działają w każdej instancji webowej, mieszając profil obciążenia. `EndAuctionsTask` i `QueuedMessagesSendScheduleTask` mogą być kosztowne. | Zużycie CPU i pamięci przez zadania względem obsługi żądań |
| **PERF-016** | Kompresja odpowiedzi w procesie aplikacji | `UseResponseCompression` z Brotli i Gzip. Świadoma decyzja, dobrze udokumentowana w kodzie (bundle 399 kB + arkusz 392 kB). Przy odwrotnym proxy kompresującym jest to praca wykonywana dwukrotnie. | Zużycie CPU na kompresję; udokumentować zalecenie wyłączenia przy proxy kompresującym |

### Świadomie NIE optymalizowane

- **`public virtual` w 1037 metodach** — narzut wywołania wirtualnego jest pomijalny, a możliwość
  nadpisania przez wtyczkę jest funkcją, nie długiem.
- **AutoMapper** — narzut mapowania jest realny, ale zamiana na mapowanie ręczne w setkach miejsc to
  ogromny koszt przy niepewnym zysku. Bez pomiaru: nie ruszać.
- **Refleksja przy starcie** (`TypeSearcher`, `Activator.CreateInstance` dla `IStartupApplication`,
  `IAutoMapperProfile`, `ITypeConverter`) — koszt jednorazowy przy starcie procesu, nieistotny dla
  wydajności runtime.

---

## 12. Target Architecture

### Zasady docelowe

Architektura docelowa to **ta sama architektura co dziś, z wyegzekwowanymi granicami**. Nie proponuję
przejścia na Clean Architecture z czterema warstwami, mikroserwisów ani rich domain model. Modular
Monolith z bazą dokumentową jest właściwym wyborem dla platformy e-commerce instalowanej przez pojedyncze
sklepy — zmiana paradygmatu kosztowałaby lata i nie rozwiązała żadnego z problemów z sekcji 2.

Pięć zasad docelowych:

1. **Granica jest granicą tylko wtedy, gdy jest egzekwowana maszynowo.** Referencja projektu albo test
   architektoniczny — nie konwencja w code review.
2. **Logika istnieje w jednym miejscu.** Trzy panele różnią się autoryzacją i zakresem danych, nie logiką.
3. **Bezpieczeństwo jest domyślne.** Nowa akcja jest domyślnie chroniona i domyślnie ograniczona zakresem;
   rezygnacja z ochrony jest jawną deklaracją.
4. **Operacje na danych krytycznych są atomowe.** Numer zamówienia, stan magazynowy, saldo punktów —
   nigdy przez read-modify-write.
5. **Kontrakt wtyczki jest jawny i wersjonowany.** To, co nie jest kontraktem, wolno refaktoryzować.

### Moduły i granice

| Warstwa | Projekty | Wolno zależeć od | Odpowiedzialność |
|---|---|---|---|
| **Kernel** | `Grand.SharedKernel` | — | Typy pomocnicze bez logiki domenowej |
| **Domain** | `Grand.Domain` | `SharedKernel` | Encje, ustawienia, enumy. Model anemiczny — celowo |
| **Persistence** | `Grand.Data` | `Domain`, `SharedKernel` | `IRepository<T>`, sterowniki Mongo/LiteDB, sesje |
| **Infrastructure** | `Grand.Infrastructure`, `Grand.Mediator`, `Grand.Mapping` | `Data`, `Domain` | Cache, konfiguracja, wtyczki, mediator, kontekst |
| **Contracts** | `Grand.Business.Core` | `Infrastructure`, `Domain`, `SharedKernel` | Interfejsy, komendy, zapytania, zdarzenia, DTO |
| **Application** | `Grand.Business.{Catalog,Checkout,…}` | `Business.Core` + niżej | Logika biznesowa. **Nigdy siebie nawzajem** |
| **Modules** | `Grand.Module.{Api,Installer,Migration,ScheduledTasks}` | `Business.Core` + niżej | Funkcje opcjonalne, ładowane przez `FeatureManagement` |
| **Web shared** | `Grand.Web.Common`, `Grand.Web.AdminShared` | `Business.*`, `Infrastructure` | Pipeline, filtry, kontrolery bazowe, serwisy widoku |
| **Hosts** | `Grand.Web`, `.Admin`, `.Store`, `.Vendor` | `Web.Common`, `AdminShared` | Trasy, autoryzacja, zakres, widoki |
| **Plugins** | `src/Plugins/**` | **wyłącznie publiczne API** | Rozszerzenia |

**Zmiany względem stanu obecnego:**

- `Grand.Web` **przestaje** referencować `Grand.Web.Admin`, `.Store` i `.Vendor`. Dziś storefront zawiera
  kod wszystkich paneli — to sprzęga wdrożenia i powiększa powierzchnię ataku. Wspólny kod idzie do
  `AdminShared`, hosty stają się równorzędne.
- `Theme.Modern` **przestaje** referencować `Grand.Web.csproj` — motyw korzysta wyłącznie z publicznego API.
- Macierz zależności między domenami biznesowymi jest **zadeklarowana i weryfikowana** testem.

### Application

Warstwa aplikacji pozostaje tym, czym jest: serwisami z interfejsami w `Grand.Business.Core` plus
handlerami komend i zapytań. **Nie wprowadzamy pełnego CQRS** — dziś część operacji idzie przez serwisy,
część przez mediator, i to jest pragmatyczne. Zasada docelowa:

- **Mediator** — operacje wieloetapowe, przekraczające granicę domen, wymagające rozszerzalności przez
  wtyczki (`PlaceOrderCommand`, `GetProductDetailsPage`).
- **Serwis** — operacje CRUD w obrębie jednej domeny (`ICategoryService.InsertCategory`).

Ujednolicanie tego na siłę w jedną stronę byłoby czystą stratą.

Zmiana: `PlaceOrderCommandHandler` rozbity na fazy (`DATA-016`), każda z ograniczoną liczbą zależności.

### Domain

**Model pozostaje anemiczny.** Encje to kontenery danych serializowane bezpośrednio do dokumentów
MongoDB. Wprowadzenie zachowania do encji wymagałoby oddzielenia modelu domenowego od modelu
persystencji, czyli warstwy mapowania dla ~200 encji, przy zerowym zysku dla użytkownika. Uzasadnienie
w sekcji 15.

Jedyna zmiana: `BaseEntity` zyskuje znacznik współbieżności (`DATA-014`).

### Infrastructure

Bez zmian strukturalnych. Zmiany punktowe: usunięcie `BuildServiceProvider` (`ARCH-012`), izolacja
wtyczek (`PLG-013`), diagnostyka (`OBS-012`), limit rozmiaru cache (`PERF-004`).

### Plugin architecture

Docelowo:

- Publiczne API jawnie zadeklarowane i weryfikowane testem (`PLG-012`)
- Wersjonowanie zakresem, nie równością (`PLG-011`)
- Ładowanie do izolowanego `AssemblyLoadContext` (`PLG-013`)
- Wadliwa wtyczka nie blokuje startu aplikacji (`PLG-029`)
- Polityka deprecjacji z `[Obsolete]` i okresem przejściowym (`PLG-035`)

### Persistence

- `IRepository<T>` pozostaje głównym kontraktem — jest dobry i dopasowany do MongoDB
- Dochodzi opcjonalna sesja/transakcja (`DATA-015`)
- Dochodzi optimistic concurrency (`DATA-014`)
- Operacje inkrementalne przez `$inc`, nie read-modify-write (`DATA-013`)
- Indeksy deklarowane obok encji, weryfikowane przy starcie (`DATA-023`)

### APIs

`Grand.Module.Api` pozostaje modułem opcjonalnym, ładowanym przez `FeatureManagement`.
Parser zapytań (`ApiQueryOptions`) jest dobrze zabezpieczony i pozostaje. Dochodzą: limit czasu zapytania
(`DATA-026`), testy kontraktowe (`TEST-040`), testy autoryzacji (`TEST-041`).

### Background processing

- Pętla samonaprawialna, bez `break` w sytuacjach odwracalnych (`REL-011`)
- Atomowe przejęcie uruchomienia (`TryClaimTaskRun`) — **zachować, jest poprawne**
- Docelowo: harmonogramy cron (`REL-012`) i opcjonalny oddzielny host workera (`REL-013`)
- Outbox dla wywołań zewnętrznych (`DATA-021`)

### Events

`Grand.Mediator` pozostaje. Notyfikacje publikowane synchronicznie w ścieżce żądania — **zachować jako
domyślne**, bo daje przewidywalność i prostotę debugowania. Docelowo: możliwość zadeklarowania handlera
jako asynchronicznego (przez outbox) dla operacji, które nie muszą blokować odpowiedzi.

**Nie dodawać pipeline behaviors** — uzasadnienie w sekcji 15.

### Caching

Bez zmian strukturalnych. `ICacheBase` + `MemoryCacheBase`/`RedisMessageCacheManager` + stałe kluczy
+ inwalidacja przez zdarzenia i Redis pub/sub to dobry projekt. Dochodzi limit rozmiaru (`PERF-004`)
i metryki (`DATA-030`).

### Diagram ASCII

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│                                   HOSTS                                       │
│                                                                               │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐              │
│  │ Grand.Web  │  │ Grand.Web  │  │ Grand.Web  │  │ Grand.Web  │              │
│  │(storefront)│  │  .Admin    │  │  .Store    │  │  .Vendor   │              │
│  └─────┬──────┘  └─────┬──────┘  └─────┬──────┘  └─────┬──────┘              │
│        │  ZMIANA: brak wzajemnych referencji między hostami                   │
│        │               │               │               │                      │
│        │               └───────┬───────┴───────────────┘                      │
│        │                       │                                              │
│        │              ┌────────▼──────────┐                                   │
│        │              │ Grand.Web         │  kontrolery bazowe + serwisy      │
│        │              │   .AdminShared    │  widoku + widoki domyślne         │
│        │              └────────┬──────────┘  (ARCH-013/014)                   │
│        └──────────┬────────────┘                                              │
│                   │                                                           │
│         ┌─────────▼──────────┐                                                │
│         │  Grand.Web.Common  │  pipeline, filtry, IDataScopeProvider,         │
│         │                    │  ScopedResourceAttribute (SEC-015), motywy      │
│         └─────────┬──────────┘                                                │
└───────────────────┼───────────────────────────────────────────────────────────┘
                    │
┌───────────────────┼───────────────────────────────────────────────────────────┐
│                   │            MODULES (opcjonalne, FeatureManagement)         │
│  ┌────────────┐ ┌─┴──────────┐ ┌──────────────┐ ┌─────────────────────┐       │
│  │ Module.Api │ │  .Installer│ │  .Migration  │ │  .ScheduledTasks    │       │
│  └─────┬──────┘ └─────┬──────┘ └──────┬───────┘ └──────────┬──────────┘       │
└────────┼──────────────┼───────────────┼────────────────────┼──────────────────┘
         │              │               │                    │
┌────────┼──────────────┼───────────────┼────────────────────┼──────────────────┐
│        │              │  APPLICATION  │                    │                   │
│  ┌─────▼──────┐ ┌─────▼──────┐ ┌──────▼─────┐ ┌────────────▼───┐ ┌──────────┐ │
│  │  Catalog   │ │  Checkout  │ │  Customers │ │   Marketing    │ │ Messages │ │
│  └─────┬──────┘ └─────┬──────┘ └──────┬─────┘ └────────┬───────┘ └────┬─────┘ │
│  ┌─────▼──────┐ ┌─────▼──────┐ ┌──────▼──────────┐     │              │       │
│  │    Cms     │ │  Storage   │ │ Authentication  │     │              │       │
│  └─────┬──────┘ └─────┬──────┘ └──────┬──────────┘     │              │       │
│  ┌─────▼──────┐       │               │                │              │       │
│  │   Common   │       │               │                │              │       │
│  └─────┬──────┘       │               │                │              │       │
│        │  ZASADA: moduły NIGDY nie referencują siebie nawzajem                 │
│        └──────────────┴───────┬───────┴────────────────┴──────────────┘       │
└───────────────────────────────┼───────────────────────────────────────────────┘
                                │
                    ┌───────────▼─────────────┐
                    │   Grand.Business.Core   │   Interfaces / Commands /
                    │   (CONTRACTS)           │   Queries / Events / Dto
                    └───────────┬─────────────┘   macierz zależności
                                │                 weryfikowana testem (TEST-037)
┌───────────────────────────────┼───────────────────────────────────────────────┐
│                          INFRASTRUCTURE                                        │
│  ┌──────────────────────┐  ┌──┴────────────┐  ┌──────────────┐                │
│  │ Grand.Infrastructure │  │ Grand.Mediator│  │ Grand.Mapping│                │
│  │  cache / plugins /   │  └───────────────┘  └──────────────┘                │
│  │  config / context /  │                                                      │
│  │  diagnostics(OBS-012)│                                                      │
│  └──────────┬───────────┘                                                      │
│  ┌──────────▼───────────┐   IRepository<T> + sesje (DATA-015)                  │
│  │     Grand.Data       │   + optimistic concurrency (DATA-014)                │
│  │  Mongo  │  LiteDB    │   + indeksy deklaratywne (DATA-023)                  │
│  └──────────┬───────────┘                                                      │
│  ┌──────────▼───────────┐  ┌────────────────────┐                             │
│  │    Grand.Domain      │  │ Grand.SharedKernel │                             │
│  │  (model anemiczny)   │  └────────────────────┘                             │
│  └──────────────────────┘                                                      │
└────────────────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────────────────┐
│                              PLUGINS (16 w repo)                                │
│   Payments.* │ Shipping.* │ Tax.* │ Widgets.* │ Authentication.* │ Theme.*     │
│                                                                                 │
│   Izolowany AssemblyLoadContext (PLG-013)                                       │
│   Wolno używać WYŁĄCZNIE publicznego API (PLG-012, weryfikowane TEST-038)       │
│   Wersjonowanie zakresem (PLG-011)                                              │
│   ZMIANA: Theme.Modern przestaje referencować Grand.Web                         │
└────────────────────────────────────────────────────────────────────────────────┘

           ┌─────────────┐        ┌─────────────┐       ┌──────────────┐
           │  MongoDB    │        │   Redis     │       │ OTLP / Azure │
           │  (+ replica │◄──────►│  pub/sub    │       │   Monitor    │
           │   set dla   │        │  cache sync │       │  (OBS-012)   │
           │   DATA-015) │        │  + DP keys  │       └──────────────┘
           └─────────────┘        └─────────────┘
```

---

## 13. Roadmap

Kolejność wynika z zależności, nie z preferencji. Fazy 0 i 5 muszą poprzedzać fazę 2 — inaczej największe
zmiany strukturalne odbywają się bez siatki bezpieczeństwa.

### Phase 0 – Critical

P0/P1 dotyczące integralności danych i najostrzejszych luk bezpieczeństwa. Wykonalne natychmiast,
bez zależności.

| ID | Zadanie | Effort |
|---|---|---|
| `DATA-011` | Atomowy sekwencer numeru zamówienia | S |
| `DATA-012` | Unikalny indeks na `Order.OrderNumber` | S |
| `DATA-013` | Atomowe modyfikacje stanów magazynowych | M |
| `SEC-012` | Bezpieczne domyślne ustawienia bezpieczeństwa | S |
| `SEC-013` | Zabezpieczenie endpointów uploadu | S |
| `SEC-014` | Usunięcie PII z telemetrii | S |
| `REL-011` | Naprawa pętli sterującej zadań cyklicznych | S |

**Kryterium wyjścia:** brak możliwości duplikatu numeru zamówienia i lost update na magazynie
(potwierdzone testem); instalacja domyślna wysyła nagłówki bezpieczeństwa; zadania cykliczne wznawiają
się bez restartu.

### Phase 1 – Foundations

Podstawowe granice i siatka bezpieczeństwa. **Faza blokująca dla wszystkiego, co dalej.**

| ID | Zadanie | Effort |
|---|---|---|
| `TEST-026` | Naprawa niestabilności testów | M |
| `TEST-024` | Projekt testów architektonicznych + reguły bazowe | M |
| `TEST-036` | Reguła: atrybuty autoryzacji na akcjach | S |
| `TEST-020` | Infrastruktura testów integracyjnych | L |
| `ARCH-011` | Walidacja kontenera DI w Development | M |
| `ARCH-012` | Usunięcie `BuildServiceProvider` | S |
| `TEST-037` | Macierz zależności między domenami | S |

**Kryterium wyjścia:** CI jest wiarygodne (20 kolejnych zielonych uruchomień równoległych); testy
architektoniczne zawodzą przy naruszeniu granic; da się napisać test integracyjny w mniej niż godzinę.

### Phase 2 – Core Architecture

Największe zmiany strukturalne. Wykonalne dopiero po fazie 1.

| ID | Zadanie | Effort |
|---|---|---|
| `TEST-023` | Testy integracyjne izolacji najemcy | L |
| `TEST-025` | Testy integracyjne CRUD zaplecza | L |
| `TEST-022` | Testy integracyjne składania zamówienia | M |
| `SEC-015` | Centralny mechanizm zakresu danych | L |
| `TEST-024b` | Reguła: atrybuty zakresu na akcjach | S |
| `DATA-016` | Rozbicie `PlaceOrderCommandHandler` | L |
| `ARCH-013` | Uogólnione kontrolery zaplecza — pilot na produkcie | XL |
| `ARCH-014` | Rozszerzenie konsolidacji na pozostałe encje | XL |

**Kryterium wyjścia:** jedna implementacja logiki CRUD dla trzech paneli; zakres najemcy egzekwowany
przez infrastrukturę; żadna nowa klasa nie ma więcej niż 8 zależności.

### Phase 3 – Security

Hardening po ustabilizowaniu struktury.

| ID | Zadanie | Effort |
|---|---|---|
| `SEC-011` | Sanityzacja HTML na białej liście | M |
| `TEST-043` | Zestaw payloadów XSS | S |
| `SEC-017` | `script-src` w domyślnym CSP | M |
| `SEC-016` | `MetadataLoadContext` zamiast `Assembly.Load` przy uploadzie wtyczki | S |
| `TEST-045` | Testy antiforgery | S |
| `TEST-041` | Testy autoryzacji API | M |
| `SEC-019` | Udokumentowanie i pokrycie testem semantyki „deny wins" | S |

**Kryterium wyjścia:** wszystkie payloady XSS neutralizowane; CSP ogranicza `script-src`; testy
bezpieczeństwa w CI.

### Phase 4 – Reliability

Odporność, spójność, przetwarzanie w tle.

| ID | Zadanie | Effort |
|---|---|---|
| `DATA-015` | Opcjonalne wsparcie transakcji | L |
| `DATA-017` | Transakcyjne utrwalenie zamówienia | M |
| `DATA-014` | Znacznik współbieżności w `BaseEntity` | L |
| `REL-012` | Harmonogramy cron dla zadań | M |
| `DATA-031` | Narzędzie wykrywania niespójności między kolekcjami | M |
| `PLG-029` | Wadliwa wtyczka nie blokuje startu aplikacji | S |
| `DATA-023` | Deklaratywne indeksy z weryfikacją przy starcie | M |

**Kryterium wyjścia:** wyjątek w środku składania zamówienia nie zostawia częściowego stanu (na replica
set); zadania mają harmonogramy cron; niespójności są wykrywalne.

### Phase 5 – Testing

Rozszerzenie pokrycia poza to, co było konieczne dla faz 1–2.

| ID | Zadanie | Effort |
|---|---|---|
| `TEST-021` | Testy integracyjne magazynu (jeśli nie wykonane wcześniej z `DATA-013`) | M |
| `TEST-033` | Test walidacji kontenera DI | S |
| `TEST-034` | Testy ładowania wtyczek | M |
| `TEST-030` | Testy jednostkowe `Grand.Web.Common` | M |
| `TEST-040` | Testy kontraktowe API | M |
| `TEST-035` | Testy migracji | M |

**Kryterium wyjścia:** każdy obszar zmieniany w fazach 6–8 ma testy zabezpieczające.

### Phase 6 – Observability

Diagnostyka produkcyjna. Warunek mierzalności fazy 7.

| ID | Zadanie | Effort |
|---|---|---|
| `OBS-011` | Health checks sprawdzające zależności | S |
| `OBS-012` | Metryki i span'y domenowe | M |
| `OBS-028` | Rejestracja źródeł w OpenTelemetry | S |
| `OBS-033` | `TraceId` w odpowiedzi błędu | S |
| `OBS-026` | Instrumentacja zapytań MongoDB | S |
| `OBS-021` | Przegląd `catch (Exception)` pod kątem połykania błędów | S |
| `OBS-035` | Runbook operacyjny | M |

**Kryterium wyjścia:** incydent „checkout nie działa" jest diagnozowalny z telemetrii bez dostępu do bazy.

### Phase 7 – Performance

Optymalizacje oparte na dowodach. Wszystko z kategorii **Potential** wymaga najpierw pomiaru z fazy 6.

| ID | Zadanie | Effort |
|---|---|---|
| `PERF-003` | Blokujące `.Result` w schematach eksportu | S |
| `PERF-006` | Pozostałe synchroniczne zapytania w ścieżkach żądania | S |
| `PERF-004` | `SizeLimit` dla `IMemoryCache` | M |
| `DATA-026` | Limit czasu zapytań API | S |
| `PERF-010`–`PERF-016` | Pozycje **Potential** — wyłącznie po pomiarze | — |

**Kryterium wyjścia:** każda wykonana optymalizacja ma udokumentowany pomiar przed i po.

### Phase 8 – Long Term

Pozostałe usprawnienia, w tym te oznaczone jako **Nice to have**.

| ID | Zadanie | Effort |
|---|---|---|
| `PLG-011` | Zakres wersji wtyczek | S |
| `PLG-012` | Dokument publicznego API wtyczek | M |
| `PLG-013` | Izolacja wtyczek w `AssemblyLoadContext` | L |
| `PLG-035` | Polityka deprecjacji | S |
| `PLG-036` | Dokument migracji wtyczek | M |
| `REL-013` | Oddzielny host workera | L |
| `DATA-021` | Outbox dla wywołań zewnętrznych | L |
| `TEST-048` | Testy E2E ścieżki zakupowej | M |
| `TEST-049` | Testy E2E instalacji | M |
| `OBS-031` | Strona diagnostyczna w panelu | M |

> **Uwaga do `PLG-011`:** mimo umieszczenia w fazie 8 ze względu na zależności, samo zadanie jest
> Quick Winem (S, brak zależności) i może być wykonane w dowolnym momencie. Umieszczenie tutaj wynika
> z tego, że pełną wartość daje dopiero razem z `PLG-012`.

---

## 14. Dependency Graph

### Graf główny

```text
TEST-026 (naprawa niestabilności CI)
   │
   └── TEST-024 (testy architektoniczne)
          │
          ├── TEST-036 (reguła: autoryzacja na akcjach)
          ├── TEST-037 (macierz zależności domen)          ──► ARCH-002 (zamknięte)
          └── TEST-038 (granice API wtyczek)
                 │
                 └── PLG-012 (dokument publicznego API)
                        │
                        └── PLG-013 (izolacja AssemblyLoadContext)
                               │
                               └── PLG-032 (wyładowywanie wtyczek)

TEST-020 (infrastruktura testów integracyjnych)
   │
   ├── TEST-021 (magazyn + współbieżność)
   │      │
   │      └── DATA-013 (atomowe stany magazynowe)
   │             │
   │             └── DATA-014 (znacznik współbieżności w BaseEntity)
   │
   ├── TEST-022 (składanie zamówienia — golden test)
   │      │
   │      └── DATA-016 (rozbicie PlaceOrderCommandHandler)
   │             │
   │             └── DATA-017 (transakcyjne utrwalenie)
   │                    │
   │                    └── wymaga DATA-015
   │
   ├── TEST-023 (izolacja najemcy)
   │      │
   │      └── SEC-015 (centralny mechanizm zakresu)
   │             │
   │             ├── TEST-024b (reguła: zakres na akcjach)
   │             │
   │             └── ARCH-013 (uogólnione kontrolery — pilot)
   │                    │
   │                    └── ARCH-014 (pozostałe encje)
   │
   ├── TEST-025 (CRUD zaplecza)  ──► ARCH-013
   │
   ├── TEST-033 (walidacja kontenera DI)
   ├── TEST-034 (ładowanie wtyczek)  ──► PLG-013
   └── DATA-015 (wsparcie transakcji, wymaga replica set w testach)

ARCH-011 (walidacja DI w Development)
   │
   └── ARCH-012 (usunięcie BuildServiceProvider)

SEC-014 (PII poza telemetrią)
   │
   └── OBS-012 (metryki i span'y domenowe)
          │
          ├── OBS-028 (rejestracja źródeł OTEL)
          ├── OBS-026 (instrumentacja MongoDB)
          │      │
          │      └── PERF-010, PERF-013 (pozycje Potential)
          │
          └── PERF-004 (SizeLimit cache — wymaga danych o zużyciu)

SEC-011 (sanityzacja HTML)
   │
   ├── TEST-043 (payloady XSS)
   └── SEC-017 (script-src w CSP)

DATA-011 (sekwencer numeru zamówienia)
   │
   └── DATA-012 (unikalny indeks)
```

### Zadania bez zależności (wykonalne natychmiast)

```text
DATA-011   SEC-012   SEC-013   SEC-014   REL-011
ARCH-011   TEST-024  TEST-026  PLG-011   OBS-011
PERF-003   PERF-006  SEC-016   PLG-029
```

### Ścieżka krytyczna

```text
TEST-026 ──► TEST-020 ──► TEST-023 ──► SEC-015 ──► ARCH-013 ──► ARCH-014
   M            L            L           L           XL           XL
```

To jest najdłuższa łańcuchowa zależność w dokumencie i determinuje minimalny czas realizacji celu
„jedna implementacja logiki zaplecza". Skrócenie jej nie jest możliwe bez rezygnacji z siatki
bezpieczeństwa — czego nie zalecam.

---

## 15. What NOT to change

Sekcja obowiązkowa. Poniższe elementy są **wystarczająco dobre** albo są **pragmatycznymi kompromisami**,
których refaktoryzacja kosztowałaby więcej, niż dała. Zapis jest wiążący: propozycja zmiany któregokolwiek
z tych punktów wymaga nowych dowodów, nie samego argumentu „będzie czyściej".

### 15.1 Anemiczny model domenowy — ZOSTAWIĆ

`Grand.Domain` to encje bez zachowania, serializowane bezpośrednio do dokumentów MongoDB.

**Dlaczego zostawić:** przejście na rich domain model wymagałoby oddzielenia modelu domenowego od modelu
persystencji, czyli warstwy mapowania dla ~200 encji z głęboko zagnieżdżonymi kolekcjami. Zysk dla
użytkownika: zerowy. Koszt: miesiące pracy i ryzyko regresji w każdym obszarze. Logika biznesowa
w serwisach jest testowalna i czytelna — a to jest cel, dla którego rich model bywa polecany.

Anemiczny model + serwisy to standardowy i poprawny układ dla bazy dokumentowej.

### 15.2 `IRepository<T>` jako generyczne repozytorium — ZOSTAWIĆ

Generyczne repozytorium bywa nazywane antywzorcem, bo „ukrywa możliwości ORM-a". Tutaj nie ukrywa:
`Table` zwraca `IQueryable<T>`, a `ToListAsync`/`PagedAsync`/`FirstOrDefaultAsync` wykonują zapytanie
na sterowniku. `UpdateField`, `IncField`, `UpdateOneAsync`, `AddToCollectionField`,
`UpdateCollectionFieldItem` to bezpośrednie odwzorowanie operacji MongoDB, nie ich zubożenie.

**Dlaczego zostawić:** interfejs jest jednocześnie warstwą abstrakcji nad dwoma sterownikami (Mongo
i LiteDB), co jest realną funkcją produktu — LiteDB pozwala uruchomić GrandNode bez serwera bazy.
Rezygnacja z abstrakcji oznaczałaby rezygnację z LiteDB albo duplikację całej warstwy dostępu do danych.

**Zmiany dopuszczalne:** rozszerzenie o sesje (`DATA-015`) i optimistic concurrency (`DATA-014`).
Nie zastępowanie.

### 15.3 Brak pipeline behaviors w `Grand.Mediator` — ZOSTAWIĆ

`Grand.Mediator` celowo nie ma odpowiednika `IPipelineBehavior` z MediatR.

**Dlaczego zostawić:** pipeline behaviors są mechanizmem, przez który logika staje się niewidoczna
w miejscu wywołania — walidacja, transakcja i logowanie dzieją się „gdzieś". W systemie z wtyczkami
rejestrującymi własne handlery to jest realne ryzyko: wtyczka może dodać behavior wpływający na wszystkie
komendy hosta. Obecny model — walidacja przez `ValidationFilter` w MVC, transakcja jawnie w kodzie fazy
utrwalenia — jest bardziej przewidywalny.

Jeśli kiedyś zajdzie potrzeba cross-cutting concern w mediatorze, rozwiązaniem jest dekorator na
konkretnym handlerze, nie globalny pipeline.

### 15.4 Mieszanie serwisów i mediatora — ZOSTAWIĆ

Część operacji idzie przez serwisy (`ICategoryService.InsertCategory`), część przez komendy
(`PlaceOrderCommand`). Nie jest to niekonsekwencja, tylko dopasowanie narzędzia do zadania: mediator
tam, gdzie potrzebna jest rozszerzalność przez wtyczki i przekroczenie granicy domen; serwis tam,
gdzie to zwykły CRUD.

**Dlaczego zostawić:** ujednolicenie w stronę „wszystko przez mediator" dałoby setki jednolinijkowych
handlerów bez wartości. Ujednolicenie w stronę „wszystko przez serwisy" zabrałoby wtyczkom punkty
rozszerzenia.

Warto natomiast **udokumentować kryterium wyboru** — to jest tanie i eliminuje dyskusję w każdym PR.

### 15.5 Rozdzielenie na cztery hosty webowe — ZOSTAWIĆ

`Grand.Web`, `Grand.Web.Admin`, `Grand.Web.Store`, `Grand.Web.Vendor` jako osobne aplikacje.

**Dlaczego zostawić:** różne modele uwierzytelniania, różne profile ryzyka, możliwość wystawienia
storefrontu publicznie i paneli tylko w sieci wewnętrznej, możliwość niezależnego skalowania.
To jest dobra decyzja architektoniczna.

Problemem jest **duplikacja logiki** między nimi (`ARCH-001`), nie sam podział. Konsolidacja ma dotyczyć
kodu, nie hostów.

**Jedyna zalecana zmiana:** usunięcie referencji `Grand.Web` → `Grand.Web.{Admin,Store,Vendor}`,
która sprzęga wdrożenia wbrew intencji podziału.

### 15.6 Anemiczne DTO i AutoMapper — ZOSTAWIĆ

238 testów w `Grand.Mapping.Tests` sugeruje, że mapowanie jest obszarem o dużej powierzchni. Kusi, żeby
zastąpić AutoMapper mapowaniem ręcznym „dla wydajności i jawności".

**Dlaczego zostawić:** zamiana oznacza przepisanie setek mapowań przy niepewnym zysku wydajnościowym
i pewnym ryzyku regresji. Bez pomiaru (faza 6) taka zmiana jest zgadywaniem. Jeśli pomiar wykaże, że
mapowanie jest wąskim gardłem — wtedy warto, punktowo, w konkretnych ścieżkach.

### 15.7 Kluczowanie cache i inwalidacja przez zdarzenia — ZOSTAWIĆ

`ICacheBase` + stałe kluczy w `Grand.Infrastructure/Caching/Constants/` + inwalidacja przez
`EntityCacheEvent` + synchronizacja między instancjami przez Redis pub/sub.

**Dlaczego zostawić:** to jest kompletny, przemyślany projekt. `SemaphoreSlim` per klucz zapobiega
cache stampede. `RedisMessageCacheManager` rozwiązuje problem, który w wielu systemach jest rozwiązany
źle albo wcale. Stałe kluczy zamiast stringów w miejscu użycia to dobra praktyka.

**Zmiany dopuszczalne:** limit rozmiaru (`PERF-004`), metryki (`DATA-030`). Nie przebudowa.

### 15.8 `public virtual` w serwisach — ZOSTAWIĆ

1037 metod `public virtual` w `Grand.Business.*`.

**Dlaczego zostawić:** to jest punkt rozszerzenia dla wtyczek — wtyczka może zarejestrować klasę
pochodną nadpisującą zachowanie. Usunięcie `virtual` byłoby breaking change dla ekosystemu wtyczek,
a narzut wywołania wirtualnego jest pomijalny.

### 15.9 Wyłączona walidacja kontenera DI w produkcji — ZOSTAWIĆ (warunkowo)

`ValidateScopes = false` i `ValidateOnBuild = false` w produkcji.

**Dlaczego zostawić w produkcji:** GrandNode ładuje wtyczki dynamicznie. Wtyczka z niepoprawną
rejestracją przy `ValidateOnBuild = true` uniemożliwiłaby start całego sklepu. Odporność na wadliwą
wtyczkę jest tu warta więcej niż wczesne wykrycie błędu.

**Co zmienić (`ARCH-011`):** włączyć w Development i w CI, gdzie odporność nie jest potrzebna, a wczesne
wykrycie jest cenne. Oraz **udokumentować powód** w komentarzu — obecnie wygląda to na zaniedbanie,
a jest decyzją.

### 15.10 LiteDB jako alternatywny sterownik — ZOSTAWIĆ

`Grand.Data/LiteDb` duplikuje implementację `IRepository<T>` dla LiteDB.

**Dlaczego zostawić:** możliwość uruchomienia GrandNode bez serwera MongoDB obniża barierę wejścia dla
osób oceniających produkt i dla środowisk deweloperskich. Duplikacja implementacji repozytorium to koszt,
ale ograniczony i dobrze odizolowany.

**Zastrzeżenie:** przy `DATA-013`, `DATA-014` i `DATA-015` degradacja funkcjonalności LiteDB musi być
**jawna i udokumentowana**, a nie cicha. Cicha degradacja gwarancji atomowości byłaby gorsza niż brak
wsparcia LiteDB.

### 15.11 `Grand.Business.Core` jako wspólny projekt kontraktów — ZOSTAWIĆ (na razie)

Szczegóły w `ARCH-002`. Rozbicie na dziewięć projektów kontraktowych dałoby czystszy graf zależności
kosztem dziewięciu nowych projektów i złamania wszystkich `using` we wtyczkach.

**Dlaczego zostawić:** przy 368 plikach koszt obecnego układu (pełny rebuild przy zmianie kontraktu)
jest znośny. Realny problem — brak widoczności zależności między domenami — rozwiązuje test
architektoniczny (`TEST-037`) za ułamek kosztu.

**Kryterium rewizji:** przekroczenie ~800 plików w `Grand.Business.Core` albo potrzeba niezależnego
wersjonowania kontraktu jednej domeny.

### 15.12 Struktura projektów i konwencje nazewnicze — ZOSTAWIĆ

Podział `Core` / `Business` / `Web` / `Modules` / `Plugins` / `Tests`, wzorzec `IStartupApplication`,
`Grand.Common.props` i `Grand.Plugin.props`, centralne zarządzanie wersjami pakietów, konwencje
nazewnicze z `.ai/standards/naming.md`, słownictwo domenowe (Brand nie Manufacturer, Page nie Topic).

**Dlaczego zostawić:** wszystko to jest spójne, udokumentowane i egzekwowane. Zmiana konwencji
w istniejącym, spójnym repozytorium to czysty koszt.

### 15.13 Synchroniczne publikowanie notyfikacji — ZOSTAWIĆ jako domyślne

`Grand.Mediator.Publish` wywołuje handlery sekwencyjnie, w wątku żądania.

**Dlaczego zostawić:** przewidywalność. Handler zdarzenia, który zawiedzie, przerywa operację —
i najczęściej właśnie tego chcemy (nie chcemy zamówienia, dla którego nie udało się zdjąć stanu
magazynowego). Asynchroniczne publikowanie wprowadza problem gwarancji dostarczenia, którego dziś nie ma.

**Zmiana dopuszczalna:** możliwość zadeklarowania konkretnego handlera jako asynchronicznego (przez
outbox, `DATA-021`) dla operacji, które nie muszą blokować odpowiedzi — np. webhook do systemu
zewnętrznego. Nie zmiana domyślnej semantyki.

### 15.14 Czego NIE robić w ogóle

| Propozycja | Dlaczego nie |
|---|---|
| **Mikroserwisy** | GrandNode jest instalowany przez pojedyncze sklepy na pojedynczych serwerach. Rozbicie na usługi dodałoby złożoność operacyjną bez adresata. Modular Monolith jest właściwym wyborem. |
| **Migracja na EF Core / bazę relacyjną** | Model danych jest głęboko zagnieżdżony i naturalny dla bazy dokumentowej. Przepisanie modelu, zapytań i migracji przy zerowym zysku funkcjonalnym. |
| **Event Sourcing** | Rozwiązuje problem audytu i odtwarzalności stanu, którego GrandNode nie ma (`IHistoryService` pokrywa potrzeby audytowe). Ogromny koszt, brak adresata. |
| **Clean Architecture z czterema warstwami** | Obecny podział jest funkcjonalnie równoważny. Dodanie warstw dla zgodności z diagramem z książki to koszt bez korzyści. |
| **Zastąpienie `Grand.Mediator` przez MediatR** | Odwrót od świadomej, dobrze wykonanej decyzji (commit #753). `Grand.Mediator` to ~10 plików, robi dokładnie to, co potrzebne, bez zewnętrznej zależności o niepewnej licencji. |
| **Ujednolicenie wszystkiego przez CQRS** | Setki jednolinijkowych handlerów dla operacji CRUD. Patrz 15.4. |
| **Usunięcie duplikacji kodu jako celu samego w sobie** | Duplikacja w trzech panelach jest problemem, bo powoduje realne błędy i koszt (`ARCH-001`). Duplikacja między LiteDB a Mongo (15.10) jest kosztem świadomie zaakceptowanym. Nie każda duplikacja jest długiem. |
| **Wprowadzenie abstrakcji „na przyszłość"** | Repozytorium ma dziś zdrowy poziom abstrakcji. Dodawanie interfejsów dla klas z jedną implementacją bez punktu rozszerzenia to koszt bez korzyści. |

---

## 16. Final prioritized backlog

Jedna tabela, posortowana P0 → P4. W obrębie priorytetu kolejność odpowiada zależnościom
(zadania blokujące wyżej).

**Legenda kolumn:** Effort = S/M/L/XL · Risk = ryzyko wykonania zadania · Breaking = wpływ na API publiczne
lub dane

| ID | Priorytet | Kategoria | Zadanie | Effort | Risk | Breaking | Zależności |
| -- | --------- | --------- | ------- | ------ | ---- | -------- | ---------- |
| `DATA-011` | P0 | Data | Atomowy sekwencer numeru zamówienia | S | Średnie | Nie | — |
| `DATA-012` | P0 | Data | Unikalny indeks na `Order.OrderNumber` | S | Średnie | Nie | `DATA-011` |
| `DATA-013` | P0 | Data | Atomowe modyfikacje stanów magazynowych | M | Wysokie | Nie | — |
| `SEC-012` | P0 | Security | Bezpieczne domyślne ustawienia bezpieczeństwa | S | Średnie | Częściowo | — |
| `SEC-013` | P0 | Security | Zabezpieczenie endpointów uploadu | S | Niskie | Częściowo | — |
| `SEC-014` | P0 | Security | Usunięcie PII z telemetrii | S | Niskie | Nie | — |
| `REL-011` | P0 | Reliability | Naprawa pętli sterującej zadań cyklicznych | S | Niskie | Nie | — |
| `TEST-026` | P1 | Testing | Naprawa niestabilności testów równoległych | M | Średnie | Nie | — |
| `TEST-024` | P1 | Testing | Projekt testów architektonicznych + reguły bazowe | M | Niskie | Nie | — |
| `TEST-036` | P1 | Testing | Reguła: atrybuty autoryzacji na akcjach | S | Niskie | Nie | `TEST-024` |
| `TEST-020` | P1 | Testing | Infrastruktura testów integracyjnych | L | Średnie | Nie | `TEST-026` |
| `TEST-023` | P1 | Testing | Testy integracyjne izolacji najemcy | L | Niskie | Nie | `TEST-020` |
| `TEST-022` | P1 | Testing | Testy integracyjne składania zamówienia | M | Niskie | Nie | `TEST-020` |
| `TEST-021` | P1 | Testing | Testy integracyjne magazynu i współbieżności | M | Niskie | Nie | `TEST-020` |
| `SEC-011` | P1 | Security | Sanityzacja HTML na białej liście | M | Średnie | Częściowo | — |
| `TEST-043` | P1 | Testing | Zestaw payloadów XSS | S | Niskie | Nie | `SEC-011` |
| `SEC-015` | P1 | Security | Centralny mechanizm zakresu danych najemcy | L | Wysokie | Nie | `TEST-023`, `TEST-024` |
| `TEST-044` | P1 | Testing | Testy IDOR (raportowanie z `TEST-023`) | S | Niskie | Nie | `TEST-023` |
| `DATA-016` | P1 | Data | Rozbicie `PlaceOrderCommandHandler` na fazy | L | Wysokie | Nie | `TEST-022` |
| `ARCH-013` | P1 | Architecture | Uogólnione kontrolery zaplecza — pilot na produkcie | XL | Wysokie | Tak | `SEC-015`, `TEST-023`, `TEST-025` |
| `ARCH-014` | P1 | Architecture | Konsolidacja pozostałych encji zaplecza | XL | Wysokie | Tak | `ARCH-013` |
| `DATA-015` | P1 | Data | Opcjonalne wsparcie transakcji MongoDB | L | Wysokie | Częściowo | `TEST-020` |
| `DATA-017` | P1 | Data | Transakcyjne utrwalenie zamówienia | M | Wysokie | Nie | `DATA-015`, `DATA-016` |
| `DATA-014` | P1 | Data | Znacznik współbieżności w `BaseEntity` | L | Wysokie | Tak | `DATA-013`, `TEST-021` |
| `ARCH-011` | P2 | Architecture | Walidacja kontenera DI w Development | M | Średnie | Nie | — |
| `ARCH-012` | P2 | Architecture | Usunięcie `BuildServiceProvider` z konfiguracji | S | Średnie | Nie | `ARCH-011` |
| `OBS-011` | P2 | Observability | Health checks sprawdzające zależności | S | Niskie | Nie | — |
| `OBS-012` | P2 | Observability | Metryki i span'y domenowe | M | Średnie | Nie | `SEC-014` |
| `OBS-028` | P2 | Observability | Rejestracja własnych źródeł w OpenTelemetry | S | Niskie | Nie | `OBS-012` |
| `OBS-033` | P2 | Observability | `TraceId` w odpowiedzi błędu | S | Niskie | Nie | — |
| `OBS-024` | P2 | Observability | Kontrola kardynalności wymiarów metryk | S | Niskie | Nie | `OBS-012` |
| `TEST-025` | P2 | Testing | Testy integracyjne CRUD zaplecza | L | Niskie | Nie | `TEST-020` |
| `TEST-024b` | P2 | Testing | Reguła: atrybuty zakresu na akcjach | S | Niskie | Nie | `SEC-015` |
| `TEST-037` | P2 | Testing | Macierz zależności między domenami | S | Niskie | Nie | `TEST-024` |
| `TEST-033` | P2 | Testing | Test walidacji kontenera DI | S | Niskie | Nie | `TEST-020`, `ARCH-011` |
| `TEST-034` | P2 | Testing | Testy ładowania wtyczek | M | Niskie | Nie | `TEST-020` |
| `TEST-030` | P2 | Testing | Testy jednostkowe `Grand.Web.Common` | M | Niskie | Nie | — |
| `TEST-040` | P2 | Testing | Testy kontraktowe API | M | Niskie | Nie | `TEST-020` |
| `TEST-041` | P2 | Testing | Testy autoryzacji API | M | Niskie | Nie | `TEST-020` |
| `TEST-045` | P2 | Testing | Testy antiforgery | S | Niskie | Nie | `TEST-020` |
| `TEST-046` | P2 | Testing | Testy uploadu (limity, rozszerzenia, rate limiting) | S | Niskie | Nie | `SEC-013` |
| `SEC-016` | P2 | Security | `MetadataLoadContext` zamiast `Assembly.Load` przy uploadzie wtyczki | S | Niskie | Nie | — |
| `SEC-017` | P2 | Security | `script-src` w domyślnym CSP | M | Średnie | Częściowo | `SEC-011` |
| `SEC-018` | P2 | Security | Usunięcie e-maili z komunikatów logów | S | Niskie | Nie | — |
| `PERF-003` | P2 | Performance | Blokujące `.Result` w schematach eksportu (**Confirmed**) | S | Niskie | Nie | — |
| `PERF-006` | P2 | Performance | Synchroniczne zapytania w ścieżkach żądania (**Confirmed**) | S | Niskie | Nie | — |
| `PERF-004` | P2 | Performance | `SizeLimit` dla `IMemoryCache` (**Confirmed**) | M | Średnie | Nie | `OBS-012` |
| `PERF-007` | P2 | Performance | Duplikacja singletonów (**Confirmed**) | S | Niskie | Nie | = `ARCH-012` |
| `DATA-018` | P2 | Data | Usunięcie konstruktora `MongoRepository` tworzącego własny `MongoClient` | S | Niskie | Częściowo | — |
| `DATA-019` | P2 | Data | Pozostałe synchroniczne zapytania LINQ | S | Niskie | Nie | = `PERF-006` |
| `DATA-023` | P2 | Data | Deklaratywne indeksy z weryfikacją przy starcie | M | Średnie | Nie | — |
| `DATA-026` | P2 | Data | Limit czasu wykonania zapytań API (`maxTimeMS`) | S | Niskie | Nie | — |
| `DATA-031` | P2 | Data | Narzędzie wykrywania niespójności między kolekcjami | M | Niskie | Nie | — |
| `DATA-028` | P2 | Data | Limit rozmiaru cache (**Confirmed**) | M | Średnie | Nie | = `PERF-004` |
| `DATA-022` | P2 | Data | Ogólny mechanizm rozproszonej blokady (**Potential**) | M | Średnie | Nie | `OBS-012` |
| `REL-012` | P2 | Reliability | Harmonogramy cron dla zadań cyklicznych | M | Średnie | Nie | — |
| `PLG-011` | P2 | Plugin | Zakres wersji wtyczek zamiast dokładnej równości | S | Niskie | Nie | — |
| `PLG-012` | P2 | Plugin | Dokument publicznego API wtyczek | M | Niskie | Nie | `TEST-024` |
| `PLG-023` | P2 | Plugin | Test architektoniczny granic API wtyczek | S | Niskie | Nie | `PLG-012` |
| `PLG-020` | P2 | Plugin | Usunięcie referencji `Theme.Modern` → `Grand.Web` | M | Średnie | Częściowo | `PLG-012` |
| `PLG-021` | P2 | Plugin | Udokumentowanie modelu zaufania wtyczek | S | Niskie | Nie | — |
| `PLG-027` | P2 | Plugin | Ostrzeżenie przed restartem przy instalacji wtyczki | S | Niskie | Nie | — |
| `PLG-029` | P2 | Plugin | Wadliwa wtyczka nie blokuje startu aplikacji | S | Średnie | Nie | — |
| `PLG-035` | P2 | Plugin | Polityka deprecjacji API (`[Obsolete]` + okres przejściowy) | S | Niskie | Nie | `PLG-012` |
| `PLG-036` | P2 | Plugin | Dokument migracji wtyczek między wersjami | M | Niskie | Nie | `ARCH-013` |
| `OBS-020` | P2 | Observability | Logowanie w `catch` `BackgroundServiceTask` | S | Niskie | Nie | = `REL-011` |
| `OBS-021` | P2 | Observability | Przegląd `catch (Exception)` pod kątem połykania błędów | S | Niskie | Nie | — |
| `OBS-025` | P2 | Observability | Metryki czasu wykonania providerów wtyczek | M | Niskie | Nie | `OBS-012` |
| `TEST-031` | P2 | Testing | Testy jednostkowe sanityzacji HTML | S | Niskie | Nie | `SEC-011` |
| `QW-09` | P2 | Maintainability | Usunięcie martwego katalogu `src/Web/Grand.Web.Models` | S | Niskie | Nie | — |
| `ARCH-002` | P3 | Architecture | Macierz zależności domen zamiast rozbicia kontraktów | S | Niskie | Nie | = `TEST-037` |
| `SEC-019` | P3 | Security | Udokumentowanie i pokrycie testem semantyki „deny wins" | S | Niskie | Nie | — |
| `SEC-020` | P3 | Security | Ograniczenie kosztu zapytań API (**Potential**) | S | Niskie | Nie | = `DATA-026` |
| `SEC-021` | P3 | Security | Audyt menedżera plików elFinder — **Niezweryfikowane** | M | Średnie | Nie | — |
| `SEC-022` | P3 | Security | Ostrzeżenie przy włączeniu `UseRoslynScripts` | S | Niskie | Nie | — |
| `PLG-013` | P3 | Plugin | Izolacja wtyczek w `AssemblyLoadContext` | L | Wysokie | Częściowo | `PLG-012`, `TEST-034` |
| `PLG-022` | P3 | Plugin | Weryfikacja rejestracji typów wtyczek (**Potential**) | S | Średnie | Nie | — |
| `PLG-025` | P3 | Plugin | Deklaracja zależności między wtyczkami | M | Średnie | Nie | `PLG-012` |
| `PLG-028` | P3 | Plugin | Hak `Update` w `IPlugin` | S | Niskie | Nie | `PLG-012` |
| `PLG-030` | P3 | Plugin | Komunikat o niekompatybilności wtyczki w panelu | S | Niskie | Nie | `PLG-011` |
| `PLG-031` | P3 | Plugin | Audyt rozpakowywania archiwum wtyczki — **Niezweryfikowane** | S | Średnie | Nie | — |
| `PLG-033` | P3 | Plugin | Punkt rozszerzenia dla webhooków wychodzących | M | Średnie | Nie | `DATA-021` |
| `PLG-034` | P3 | Plugin | Udokumentowanie `UserFields` jako drogi rozszerzania encji | S | Niskie | Nie | — |
| `DATA-021` | P3 | Data | Outbox dla efektów ubocznych nietransakcyjnych | L | Wysokie | Nie | `DATA-015` |
| `DATA-020` | P3 | Data | Projekcje w zapytaniach listowych (**Potential**) | M | Średnie | Nie | `OBS-026` |
| `DATA-025` | P3 | Data | Przegląd pokrycia indeksami zapytań raportowych (**Potential**) | M | Średnie | Nie | `OBS-026` |
| `DATA-027` | P3 | Data | Audyt N+1 w ścieżkach storefrontu (**Potential**) | M | Średnie | Nie | `OBS-026` |
| `DATA-029` | P3 | Data | Koszt `RemoveByPrefix` (**Potential**) | S | Niskie | Nie | `OBS-012` |
| `DATA-030` | P3 | Data | Metryki trafień/pudeł cache | S | Niskie | Nie | `OBS-012` |
| `DATA-032` | P3 | Data | Zadanie rekoncyliacyjne pól denormalizowanych (**Potential**) | M | Średnie | Nie | `DATA-031` |
| `PERF-010` | P3 | Performance | N+1 na stronie produktu (**Potential**) | M | Średnie | Nie | `OBS-026` |
| `PERF-011` | P3 | Performance | Kosztowne zapytania API (**Potential**) | S | Niskie | Nie | `OBS-026` |
| `PERF-012` | P3 | Performance | Koszt `RemoveByPrefix` (**Potential**) | S | Niskie | Nie | `OBS-012` |
| `PERF-013` | P3 | Performance | Brak projekcji w listach (**Potential**) | M | Średnie | Nie | `OBS-026` |
| `PERF-014` | P3 | Performance | Synchroniczne publikowanie zdarzeń (**Potential**) | M | Średnie | Nie | `OBS-025` |
| `PERF-015` | P3 | Performance | Zadania cykliczne w procesie webowym (**Potential**) | L | Średnie | Nie | `OBS-012` |
| `PERF-016` | P3 | Performance | Kompresja w procesie vs. na proxy (**Potential**) | S | Niskie | Nie | `OBS-012` |
| `REL-013` | P3 | Reliability | Oddzielny host workera | L | Średnie | Nie | `REL-012` |
| `OBS-026` | P3 | Observability | Instrumentacja zapytań MongoDB | S | Niskie | Nie | `OBS-012` |
| `OBS-027` | P3 | Observability | Próbkowanie trace'ów | S | Niskie | Nie | `OBS-012` |
| `OBS-029` | P3 | Observability | Globalne atrybuty zasobu OpenTelemetry | S | Niskie | Nie | `OBS-028` |
| `OBS-030` | P3 | Observability | Health check stanu wtyczek | S | Niskie | Nie | `OBS-011` |
| `OBS-031` | P3 | Observability | Strona diagnostyczna w panelu | M | Niskie | Nie | `OBS-011` |
| `OBS-032` | P3 | Observability | Endpoint zrzutu efektywnej konfiguracji (z maskowaniem) | S | Średnie | Nie | — |
| `OBS-034` | P3 | Observability | Korelacja uruchomień zadań cyklicznych | S | Niskie | Nie | `OBS-012` |
| `OBS-035` | P3 | Observability | Runbook operacyjny | M | Niskie | Nie | `OBS-011`, `OBS-012` |
| `OBS-036` | P3 | Observability | Dynamiczna zmiana poziomu logowania | S | Niskie | Nie | — |
| `OBS-023` | P3 | Observability | Standaryzacja `EventId` zdarzeń operacyjnych | M | Niskie | Nie | — |
| `TEST-032` | P3 | Testing | Pełne pokrycie `ApiQueryOptions` przypadkami brzegowymi | S | Niskie | Nie | — |
| `TEST-035` | P3 | Testing | Testy migracji | M | Średnie | Nie | `TEST-020` |
| `TEST-039` | P3 | Testing | Reguła: encje `Grand.Domain` bez zależności do warstw wyższych | S | Niskie | Nie | `TEST-024` |
| `TEST-042` | P3 | Testing | Weryfikacja zgodności OpenAPI z endpointami | S | Niskie | Nie | `TEST-040` |
| `TEST-047` | P3 | Testing | Testy procesu logowania i polityki haseł | M | Niskie | Nie | `TEST-020` |
| `TEST-048` | P3 | Testing | Test E2E ścieżki zakupowej | M | Średnie | Nie | `TEST-020` |
| `TEST-049` | P3 | Testing | Test E2E instalacji | M | Średnie | Nie | `TEST-020` |
| `SEC-023` | P4 | Security | Podpisywanie wtyczek (**Nice to have**) | L | Średnie | Częściowo | `PLG-012` |
| `SEC-024` | P4 | Security | WebAuthn / passkeys (**Nice to have**) | L | Średnie | Nie | — |
| `PLG-032` | P4 | Plugin | Wyładowywanie wtyczek bez restartu (**Nice to have**) | XL | Wysokie | Częściowo | `PLG-013` |
| `PLG-024` | P4 | Plugin | Atrybuty oznaczające API niepubliczne (**Nice to have**) | M | Niskie | Nie | `PLG-012` |
| `PLG-026` | P4 | Plugin | Rozwiązywanie konfliktów NuGet między wtyczkami (**Nice to have**) | L | Wysokie | Nie | `PLG-013` |
| `TEST-050` | P4 | Testing | Testy E2E panelu w przeglądarce (**Nice to have**) | L | Wysokie | Nie | `TEST-048` |

### Podsumowanie liczbowe

| Priorytet | Liczba pozycji | Suma effortu (S=1, M=3, L=5, XL=8) |
|---|---:|---:|
| P0 | 7 | 9 |
| P1 | 17 | 71 |
| P2 | 42 | 82 |
| P3 | 43 | 96 |
| P4 | 6 | 30 |
| **Razem** | **115** | **288** |

Pozycje oznaczone `=` w kolumnie zależności są duplikatami tego samego zadania widzianego z perspektywy
innego backlogu (np. `PERF-007` to `ARCH-012`) — nie należy ich liczyć podwójnie przy planowaniu.

### Pozycje wymagające dodatkowego audytu

Poniższe zostały zidentyfikowane, ale **nie zweryfikowane** w tym audycie. Przed ich planowaniem
konieczne jest osobne rozpoznanie:

- `SEC-021` — powierzchnia ataku menedżera plików elFinder. **Niezweryfikowane – wymaga dodatkowego audytu.**
- `PLG-031` — manipulacja wpisami archiwum wtyczki w trybie `ZipArchiveMode.Update`. **Niezweryfikowane – wymaga dodatkowego audytu.**
- `PLG-022` — czy `RegisterPluginInterface` pozwala wtyczce nadpisać rejestrację hosta. **Niezweryfikowane – wymaga dodatkowego audytu.**
- `SEC-019` — czy semantyka „deny wins" w `PermissionService.AuthorizeAction` jest zamierzona. **Niezweryfikowane – wymaga potwierdzenia intencji przez zespół.**
- `ARCH-003` — czy w kontenerze istnieją faktyczne captive dependencies. Weryfikacja jest treścią zadania `ARCH-011`.
- Wszystkie pozycje oznaczone **Potential** w sekcji 11 — wymagają pomiaru z fazy 6.

---

## Metadane dokumentu

**Metoda audytu:** statyczna analiza repozytorium — graf referencji projektów, przegląd warstwy danych,
warstwy biznesowej, warstwy webowej, systemu wtyczek, konfiguracji bezpieczeństwa, CI i zestawu testów.
Wszystkie liczby (liczby plików, linii, wystąpień wzorców, testów) zostały zmierzone na commicie `81ff8e2d8`.

**Czego audyt NIE obejmował:**

- Uruchomienia aplikacji i testów dynamicznych
- Testów penetracyjnych
- Profilowania wydajności pod obciążeniem
- Przeglądu kodu frontendu (Vue 3, Vite) poza jego wpływem na bezpieczeństwo (`v-html`)
- Przeglądu zawartości widoków Razor poza wyszukaniem `Html.Raw`
- Przeglądu 16 wtyczek pod kątem ich wewnętrznej jakości

**Jak używać tego dokumentu:**

1. Zadanie wykonuje się pojedynczo, po ID z sekcji 4 albo z tabeli w sekcji 16.
2. Przed rozpoczęciem sprawdzić zależności w sekcji 14 — zadanie z niespełnionymi zależnościami
   wykonane wcześniej zwykle trzeba powtórzyć.
3. Kryteria akceptacji z sekcji 4 są warunkiem zamknięcia zadania, nie sugestią.
4. Sekcja 15 jest wiążąca — propozycja zmiany wymienionych tam elementów wymaga nowych dowodów.
5. Pozycje **Potential** i **Niezweryfikowane** nie są gotowe do implementacji — wymagają najpierw
   pomiaru albo rozpoznania.

**Aktualizacja:** dokument powinien być rewidowany po zamknięciu każdej fazy roadmapy. Oceny z sekcji 1
są punktem odniesienia — ich ponowna ocena po fazie 2 i po fazie 6 pokaże, czy roadmapa działa.
