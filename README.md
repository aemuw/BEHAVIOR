# BEHAVIOR

Psychological horror від першої особи (Godot 4.7 .NET, C#). Гра вивчає поведінку гравця
і поступово використовує її проти нього. Деталі концепції: `docs/ROADMAP.md`.

## Керування

| Клавіша | Дія |
|---|---|
| WASD | рух |
| Shift | біг |
| Миша | огляд |
| E | взаємодія (двері) |
| Esc | відпустити курсор (клік по вікну повертає його) |
| F3 | debug-панель (тільки для розробки) |
| F4 | зберегти лог сесії в JSON |

Лог сесії: `Project -> Open User Data Folder -> sessions/`. Також пишеться при закритті вікна гри.

## Структура

```
scenes/main/Main.tscn          квартира-прототип (генерується tools/generate_apartment.py)
scenes/main/TestRoom.tscn      стара тестова кімната-коробка
scenes/player, scenes/environment
scripts/player/                PlayerController
scripts/interaction/           InteractionSystem, Door, IInteractable
scripts/behavior/              Tracker, Model, Profile, Director, події
scripts/environment/           AdaptiveLight, RoomZone, MovableProp
scripts/systems/               VisibilityUtil
tools/generate_apartment.py    генератор планування квартири
```

## Архітектура поведінкової системи

```
Player / Door / RoomZone
        |  (Report... )
        v
BehaviorTracker  -- Movement / Look / Interactions / Spatial / Rooms
        |  спостереження: контекст -> реакція гравця (до 1.5 с)
        v
BehaviorModel    -- прогноз реакції на контекст, оцінка точності
        |
BehaviorProfile  -- Exploration, Curiosity, Avoidance, Repetition,
        |           Predictability, Hesitation, DarkAvoidance
        v
BehaviorDirector -- обирає подію (BehaviorEvent)
```

Дані про планування: стіни, двері й кімнати можна змінити в `tools/generate_apartment.py`
і перегенерувати `Main.tscn` (`python3 tools/generate_apartment.py`).
Увага: ручні зміни в Main.tscn при перегенерації буде перезаписано.
