# Справочник реализованного функционала — форк `Ical.Net`

Справочник собственных изменений форка `ntrnx/ical.net` относительно upstream-библиотеки `rianjs/ical.net`. Описывает только отличия форка — что добавлено, удалено и изменено и почему; функциональность самой библиотеки iCalendar (RFC 5545) здесь не пересказывается. Без хронологии. Формат и правила обновления — в `CLAUDE.md` потребляющего проекта.

## Обзор

Форк upstream `rianjs/ical.net` версии `4.2.0` (точка ответвления — коммит `1ad17e5`, февраль 2022). Собственные изменения: перевод на одну целевую платформу `net10.0`, исправленное экранирование/разэкранирование TEXT-значений, nullable-длительность `Period`, ослабленная проверка в `CalDateTime.ToTimeZone`, внутренний построитель `TimeZoneInfo` из `VTIMEZONE` и отдельный тестовый проект на часовые пояса. Рабочие ветки — `net-10.0` (текущая) и `net-8.0`.

## Текущее состояние

### Сборка и платформа

- `Ical.Net.csproj` собирается под единственную `TargetFramework` `net10.0` (в upstream — `netstandard2.0;net50`); `LangVersion` не фиксирован, подавлен `CS1591`.
- `NodaTime` поднят до `3.3.0`; добавлены зависимости `Microsoft.Extensions.Logging`, `Microsoft.Extensions.Logging.Abstractions` и `Microsoft.Extensions.Logging.Console` — нужны `TimeZoneCreator`.
- Сборка по-прежнему подписывается `IcalNetStrongnameKey.snk`; `InternalsVisibleTo` дополнен тестовой сборкой `Ical.Net.TestsTimeZone`.
- `PackageId`, версия пакета и метаданные NuGet оставлены upstream-овыми (`Ical.Net` `4.2.0`) — по имени пакета форк от оригинала не отличить.

### Экранирование TEXT-значений

`Ical.Net.Serialization.DataTypes.StringSerializer` переписан:

- `Unescape` — посимвольный разбор вместо цепочки `Replace` + regex `SingleBackslashMatch` (regex удалён). Поддерживаются `\n`/`\N` → перевод строки, `\\` → `\`, `\;`, `\,`, `\"` (последнее — совместимость с Mozilla Sunbird). Неизвестная escape-последовательность сохраняется как есть (обратный слэш + символ), одиночный завершающий `\` — тоже.
- `Escape` — обратный слэш экранируется **первым** (`\` → `\\`), затем переводы строк, `;` и `,`. В upstream обратный слэш при сериализации не экранировался, и строка с `\` не переживала цикл сериализация → разбор.
- Обе функции пропускают только `null`/пустую строку (`string.IsNullOrEmpty`); строка из одних пробелов теперь тоже обрабатывается (в upstream — `IsNullOrWhiteSpace`).

### `Period.Duration` стал nullable

`Ical.Net.DataTypes.Period.Duration` — `TimeSpan?` вместо `TimeSpan`. Upstream сравнивал `TimeSpan` с `null` (всегда `false`) в `EventEvaluator`, `TodoEvaluator` и `PeriodSerializer`; в форке эти проверки переписаны на `Duration.HasValue`, `Period.ExtrapolateTimes` — так же. `Alarm.GetOccurrences` подставляет `default(TimeSpan)` при отсутствии длительности. Это **изменение публичного API**: код потребителя, читающий `Period.Duration` как `TimeSpan`, требует `.Value`/`?? default`.

### `CalDateTime.ToTimeZone` без проверки `tzId`

`Ical.Net.DataTypes.CalDateTime.ToTimeZone(string tzId)` больше не бросает `ArgumentException` на пустой/`null` `tzId`: целевая зона в этом случае резолвится `DateUtil.GetZone` как локальная зона процесса. Исправление сделано для повторяющихся событий «на весь день», у которых `TzId` отсутствует, — upstream падал при вычислении их вхождений.

### Построение `TimeZoneInfo` из `VTIMEZONE`

`Ical.Net.Utility.TimeZoneCreator` — добавленный в форке класс, превращающий распарсенный `VTimeZone` в `System.TimeZoneInfo` (`TimeZoneInfo.CreateCustomTimeZone` с одним `AdjustmentRule` на плавающих правилах перехода). Поддерживается узкий профиль `VTIMEZONE`, рассчитанный на календари CommuniGate:

- одна секция (зона без перехода на летнее время) — без `RRULE`/`RDATE`/`EXDATE`, `TZOFFSETFROM == TZOFFSETTO`;
- две секции STANDARD/DAYLIGHT — у каждой ровно один `RRULE` без `UNTIL`, `FREQ=YEARLY;INTERVAL=1`, ровно один `BYMONTH` и один `BYDAY` (смещение не меньше `-1`), без прочих `BY*`-компонентов, без `RDATE`/`EXDATE`/`EXRULE`.

Всё, что вне профиля, логируется через `ILogger` и даёт `null`.

### Прочие правки API

- `Ical.Net.VTimeZoneInfo`: свойства `TZOffsetFrom`/`TZOffsetTo` переименованы в `TzOffsetFrom`/`TzOffsetTo` (ломающее изменение); добавлены `Equals(VTimeZoneInfo)`/`GetHashCode` с учётом вычислителя.
- `Ical.Net.CalendarObject`: `Name`, `Line`, `Column` больше не `virtual`; удалён пустой `protected set` у `Calendar`.
- `Ical.Net.CalendarComponents.VTimeZone`: параметр `earlistDateTimeToSupport` переименован в `earliestDateTimeToSupport` (важно для вызовов с именованными аргументами).
- `Ical.Net.Serialization.EncodingProvider`: `Decode7Bit`/`Encode7Bit` помечены `[Obsolete]` (UTF-7 устарел в .NET); кодировка `7BIT` по-прежнему поддерживается.

## Ключевые сущности и API

- `Ical.Net.Utility.TimeZoneCreator` — построитель `TimeZoneInfo` из `VTimeZone`; конструктор принимает `ILogger`, метод `CreateTimeZone(VTimeZone)` — `internal`.
- `Ical.Net.Serialization.DataTypes.StringSerializer` — `Escape`/`Unescape` (`protected virtual`) с исправленной обработкой обратного слэша.
- `Ical.Net.DataTypes.Period.Duration` — `TimeSpan?`.
- `Ical.Net.DataTypes.CalDateTime.ToTimeZone(string)` — допускает пустой `tzId`.
- `Ical.Net.VTimeZoneInfo.TzOffsetFrom`/`TzOffsetTo` — переименованные свойства смещений.
- Тестовый проект `test/Ical.Net.TestsTimeZone` (xUnit, `net10.0`): `IcalNetParserTests` (разбор одиночных, повторяющихся, «на весь день» событий и событий с `EXDATE`/изменёнными экземплярами в разных зонах — Москва, Екатеринбург, Западная Европа, Восток Северной Америки), `OccurrencesTest`, `TimeZoneConversionTest`, `TimeZoneCreatorTest`; образцы iCalendar — `Samples`/`SamplesOriginal`.

## Нюансы реализации

- **`TimeZoneCreator` наружу недоступен.** `CreateTimeZone` объявлен `internal`, а `InternalsVisibleTo` выдан только тестовым сборкам, поэтому пользоваться построителем могут лишь тесты форка; из прикладного кода он не вызывается и в самой библиотеке тоже нигде не используется. Чтобы применить его в продукте, метод нужно сделать публичным.
- **Профиль `TimeZoneCreator` продуктовый.** Ограничения взяты из требований вики продукта под формат CommuniGate; тексты логов упоминают «Resampler». Для универсального разбора произвольных `VTIMEZONE` класс не годится (несколько правил перехода, `RDATE`, исторические изменения не поддерживаются). Отрицательное смещение `BYDAY` трактуется как «последняя неделя месяца» (неделя 5), поэтому допускается только `-1`. Порядок STANDARD/DAYLIGHT определяется сравнением `BYMONTH`, разница смещений может быть отрицательной (южное полушарие).
- **Проверка частоты в `TimeZoneCreator` нестрогая.** Условие отказа записано как `Frequency != Yearly && Interval != 1`, то есть правило отвергается, только если не выполнены оба требования; `FREQ=MONTHLY;INTERVAL=1` или `FREQ=YEARLY;INTERVAL=2` пройдут валидацию.
- **Тестовый проект не входит в `src/Ical.Net.sln`.** В решение подмодуля включены только upstream-проекты (`Ical.Net`, `Ical.Net.CoreUnitTests` на NUnit, `PerfTests`); `test/Ical.Net.TestsTimeZone` надо собирать и запускать отдельно или подключать в решение потребителя.
- **Ломающие отличия от upstream.** При обновлении с upstream-пакета или обратном переходе учитывать `Period.Duration` (`TimeSpan?`), переименование `TzOffsetFrom`/`TzOffsetTo` и невиртуальные `CalendarObject.Name`/`Line`/`Column`.
- **Синхронизация с upstream не ведётся.** После точки ответвления upstream-изменения в форк не вливались; ветки `timezone-first-parser`, `tz-caldatetime-refactoring`, `tz-fix-occurrences` в origin — работа над часовыми поясами, не влитая в `net-10.0` (каждая содержит коммиты, которых нет в текущей ветке). Попытка упростить парсер iCalendar-файлов («Simplified iCalendar file parser») была отменена, парсер остался upstream-овым.

## Планируемые расширения

_Нет запланированных пунктов._
