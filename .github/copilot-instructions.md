<!-- BEGIN @przeprogramowani/10x-cli -->

## 10xDevs AI Toolkit - Moduł 5, Lekcja 5 (Agenci asynchroniczni i zdalni)

Deleguj ograniczoną, zaplanowaną zmianę i wróć, aby ją przejrzeć. Kontrola nie znika; przenosi się do kontraktu zadania, konfiguracji sandboxa i przeglądu na końcu.

```
/10x-plan -> /goal + /10x-goal-implement (interactive or your AI coding assistant in non-interactive mode) -> review the commits and the manual checklist
```

### Router zadań — od czego zacząć

| Umiejętność | Użyj jej, gdy |
| --- | --- |
| `/10x-goal-implement <change-id>` | Zatwierdzony `context/changes/<id>/plan.md` powinien uruchomić się bez nadzoru. Połącz go z `/goal <condition>`; zapisuje commit po każdej fazie zakończonej pomyślnie i kończy się oczekującymi ręcznymi kontrolami. |
| `/10x-implement` | Chcesz samodzielnie kierować każdą fazą. |

### Twarde zasady

- **Deleguj tylko zaplanowaną pracę.** Brak zatwierdzonego `plan.md` oznacza brak uruchomienia bez nadzoru.
- **Ogranicz każdy przebieg.** Warunek `/goal` określa stan końcowy i limit tur.
- **Pomyślny przebieg nie oznacza ukończonej zmiany.** Przejrzyj commity i wykonaj ręczną checklistę przed scaleniem.
- **Zapewnij sandboxowi tylko te sekrety i dostęp do sieci, których zadanie potrzebuje.**
- **Monitoruj równoległe i zdalne przebiegi z jednego miejsca.** Uruchamiaj każdą sesję bez nadzoru w osobnej przestrzeni roboczej herdr, aby widzieć, które agenty pracują, są zablokowane lub ukończyły zadanie, oraz móc interweniować z poziomu przeglądarki (konfiguracja w M2L6 Deep Dive).

<!-- END @przeprogramowani/10x-cli -->
