# Overview
TraeToDo — UWP-приложение, включающее AI-чат через OpenRouter (DeepSeek) и список задач. Настройки и состояние приложения хранятся через `SettingsManager` и `ApplicationData.Current.LocalSettings`.

# Goals
- Поддерживать настраиваемый AI‑чат (API key и идентификатор модели OpenRouter).
- Обеспечивать удобную страницу настроек для управления поведением приложения.

# Progress
- Добавлена поддержка настраиваемого `Model id` для OpenRouter DeepSeek:
  - В `SettingsManager` добавлены методы `GetModelId`/`SaveModelId` и ключ `MODEL_ID_SETTING`.
  - На странице `SettingsPage` добавлено поле `Model id` (`ModelIdTextBox`) рядом с вводом API key.
  - `DeepSeekService` теперь берет идентификатор модели через `SettingsManager.Instance.GetModelId()` вместо захардкоженной константы.

# Pending
- Протестировать новое поле `Model id` в Settings:
  - Проверить дефолтное значение `deepseek/deepseek-r1:free` при отсутствии настройки.
  - Убедиться, что изменения `Model id` сохраняются и используются в вызовах к OpenRouter.
