---
project: "Argus"
version: 1
status: draft
created: 2026-09-24
context_type: greenfield
product_type: web-app
target_scale:
  users: large
  qps: unknown
  data_volume: unknown
timeline_budget:
  mvp_weeks: 1
  hard_deadline: null
  after_hours_only: false
---

## Vision & Problem Statement

Początkujący lub rozważający pierwszą inwestycję nie mają pewności, w co inwestować i jak aktywa mogą się zmieniać. Gdy próbują zrozumieć sytuację rynkową i zwiększyć pewność, przeglądają wiele stron, wykresów i porównań; mimo tego poświęcają czas i nadal odczuwają brak wiedzy oraz pewności.

Problem obejmuje dane uwięzione w wielu miejscach, brakującą możliwość oraz paraliż decyzyjny. Insightem jest niska świadomość społeczna na temat inwestycji, brak wiedzy i strach przed utratą pieniędzy. Problem może nie być wart rozwiązywania, jeśli ludzie nie chcą inwestować.

## User & Persona

### Primary persona

Osoba początkująca lub rozważająca pierwszą inwestycję. Sięga po produkt, gdy porównuje wiele stron, wykresów i aktywów, aby lepiej zrozumieć rynek i zwiększyć pewność przed podjęciem decyzji.

## Success Criteria

### Primary

- Użytkownik wybiera ograniczoną liczbę aktywów, a Argus tworzy ranking, pokazuje oceny i jasno wyjaśnia czynniki wpływające na wyniki.

### Secondary

- Argus może łączyć inwestycje z trendami w zakładce „Trends”, pokazując powiązane aktywa i komentarz.
- AI Copilot odpowiada na pytania o powody oznaczenia, historyczne wyniki i podobne sytuacje rynkowe.

### Guardrails

- Argus jasno komunikuje edukacyjny charakter usługi i to, że nie udziela rekomendacji inwestycyjnych.
- Każda okazja, ocena lub sygnał ma zrozumiałe wyjaśnienie czynników, które na niego wpłynęły.

## User Stories

### US-01: Analyze selected assets and receive an explained ranking

- **Given** the user has created an account and is logged into Argus, the user is on the Market Analysis page, and market data is available for the selected assets
- **When** the user selects one or more assets and clicks the Analyze Market button
- **Then** Argus analyzes the selected assets, calculates an Opportunity Score for each asset, ranks the analyzed assets, displays current and historical market data, presents at least two contributing factors when sufficient data is available, generates an AI-powered explanation grounded in the shown data and factors, and displays an educational-not-advice disclaimer

#### Acceptance Criteria

- AC-01: Given the user is logged in, when they select at least one asset and start analysis, then Argus returns analysis results successfully.
- AC-02: The analysis result contains an Opportunity Score for every selected asset.
- AC-03: The analyzed assets are visibly ordered by Opportunity Score without presenting one asset as a guaranteed or singular recommendation.
- AC-04: The result contains at least two factors explaining the assigned score.
- AC-05: The result contains an AI-generated summary for the ranked assets, grounded in the shown data and factors.
- AC-06: The result page displays current price and 24-hour, 7-day, and 30-day performance metrics.
- AC-07: The result page displays a historical chart of the selected asset.

## Functional Requirements

- FR-001: User can open the application and see a short description of Argus's purpose. Priority: must-have
  > Socrates: Counter-argument considered: "The description may be too vague and fail to explain how Argus differs from a chart or search engine." Resolution: kept as written.
- FR-002: User can select assets to analyze. Priority: must-have
  > Socrates: Counter-argument considered: "Selection does not solve decision paralysis because the user may not know what to choose." Resolution: kept as written.
- FR-003: User can start analysis for selected assets. Priority: must-have
  > Socrates: Counter-argument considered: "Analysis should start automatically after asset selection; a separate action adds friction." Resolution: kept as written.
- FR-004: Argus can retrieve current and historical price data for selected assets. Priority: must-have
  > Socrates: Counter-argument considered: "Unreliable, delayed, or incomplete data could undermine trust." Resolution: kept as written.
- FR-005: Argus can calculate an Opportunity Score for each analyzed asset. Priority: must-have
  > Socrates: Counter-argument considered: "A single number creates false precision and may look like investment advice." Resolution: kept as written.
- FR-006: Argus can present assets ranked from most to least interesting. Priority: must-have
  > Socrates: Counter-argument considered: "A ranking can hide uncertainty in the signals or data quality." Resolution: kept as written.
- FR-008: User can view details for a selected asset. Priority: must-have
  > Socrates: Counter-argument considered: "Details may overwhelm a beginner." Resolution: kept as written.
- FR-009: User can view current price and 24-hour, 7-day, and 30-day changes for an asset. Priority: must-have
  > Socrates: Counter-argument considered: "Too many periods can increase cognitive load for a beginner." Resolution: kept as written.
- FR-010: User can view a historical chart for a selected asset. Priority: must-have
  > Socrates: Counter-argument considered: "A chart recreates TradingView and may not add unique value." Resolution: kept as a minimal context for the explanation.
- FR-011: Argus can generate an understandable explanation of why an asset was flagged as interesting. Priority: must-have
  > Socrates: Counter-argument considered: "AI explanations may sound convincing without reflecting the data." Resolution: kept as written.
- FR-012: Argus can present at least two concrete factors contributing to an asset's score when sufficient data is available; otherwise it can state that the evidence is insufficient. Priority: must-have
  > Socrates: Counter-argument considered: "Some assets may not have two or three reliable factors." Resolution: changed to require factors only when sufficient data is available and to state when evidence is insufficient.
- FR-013: User can view an AI-generated summary of an asset's situation based only on the data and factors presented by Argus. Priority: must-have
  > Socrates: Counter-argument considered: "AI summaries may hallucinate." Resolution: kept with the constraint that the summary is grounded only in shown data and factors and acknowledges uncertainty or missing data.
- FR-014: User can see that Argus content is educational and is not investment advice. Priority: must-have
  > Socrates: Counter-argument considered: "A disclaimer may be ignored, but it remains necessary." Resolution: kept as written.
- FR-015: User can create an account. Priority: must-have
  > Socrates: Counter-argument considered: "Registration delays first value and increases abandonment." Resolution: kept as written.
- FR-016: User can log in to the application. Priority: must-have
  > Socrates: Counter-argument considered: "Login delays the main flow and a guest-first experience may be better." Resolution: kept as written.
- FR-017: User can save selected assets to a watchlist for monitoring. Priority: nice-to-have
  > Socrates: Counter-argument considered: "A watchlist is not part of the first value and may be premature." Resolution: moved from must-have to nice-to-have.
- FR-018: User can receive a daily email report with top opportunities. Priority: nice-to-have
  > Socrates: Counter-argument considered: "The report may create false urgency, is not part of first value, and may be stale when opened." Resolution: kept as nice-to-have.
- FR-019: Argus can use search-trend data when calculating the Opportunity Score. Priority: nice-to-have
  > Socrates: Counter-argument considered: "Search popularity does not prove asset value and may distort the score." Resolution: kept as nice-to-have.
- FR-020: Argus can analyze news sentiment and include it in the asset score. Priority: nice-to-have
  > Socrates: Counter-argument considered: "News sentiment may be unreliable or biased." Resolution: kept as nice-to-have.

## Non-Functional Requirements

- Argus clearly indicates that its content is educational and does not constitute investment advice.
- Every opportunity, score, or signal includes a clear explanation of the factors that contributed to it.

## Business Logic

Argus identyfikuje i rankinguje aktywa o najwyższym potencjale inwestycyjnym na podstawie bieżących danych rynkowych oraz historycznych wzorców.

Zalogowany użytkownik wybiera jedno lub więcej aktywów do analizy i uruchamia analizę rynku. Argus oblicza Opportunity Score dla każdego wybranego aktywa, tworzy ranking od najbardziej do najmniej interesującego oraz generuje wyjaśnienie zawierające główne czynniki wpływające na ocenę aktywa i podsumowanie AI. Reguła jest wykonywana po uruchomieniu analizy, a wynik jest prezentowany na ekranie wyników analizy. Argus nie wskazuje jednego aktywa jako „Top Opportunity”.

## Access Control

Użytkownik uzyskuje dostęp przez logowanie. Wszyscy zalogowani użytkownicy mają ten sam zakres dostępu; nie ma rozdzielenia na role.

## Non-Goals

- Argus does not provide personalized investment advice; it remains an educational analysis tool.
- Argus does not execute automated trades; the user makes their own decisions.
- Argus does not manage portfolios or allocate funds; it analyzes selected assets.
- Argus does not analyze the entire market; the MVP limits analysis to assets selected by the user.
- Argus does not provide real-time alerts; alerts are outside the MVP.
- Argus does not guarantee future results or signal accuracy; it presents analysis and uncertainty.

## Open Questions

1. **Should AI Copilot be included in the MVP?** — It appears in Secondary success criteria, but no corresponding functional requirement was retained after the Socratic review. Owner: user.
