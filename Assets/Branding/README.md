# Значок приложения

Исходные файлы автора сохранены без изменений:

- `ProjectIcon-office-ribbon.svg` — векторный оригинал для редактирования.
- `ProjectIcon-office-ribbon-1024.png` — источник размеров для пакета Windows.
- `ProjectIcon-office-ribbon-512.png` — изображение в заголовке окна.
- `ProjectIcon-office-ribbon.ico` — оригинальный многомерный ICO; его точная копия `Assets/AppIcon.ico` используется для EXE, значка окна и панели задач.

После замены исходников выполните из корня проекта:

```powershell
.\scripts\update-app-icons.ps1
```

WinApp CLI 0.7.1+ создаёт PNG для ссылок из `Package.appxmanifest`: меню «Пуск», плиток, экрана запуска и значка пакета. У прозрачного исходника одинаковый рисунок для светлой и тёмной тем; генерируются обе группы `altform-unplated` / `altform-lightunplated`. Все PNG из `Assets` автоматически включаются в проект. SVG и оригинальный ICO в этой папке хранятся как исходники, отдельно от используемой копии ICO.

Требования к размерам и именованию: [Microsoft — Construct your Windows app's icon](https://learn.microsoft.com/en-us/windows/apps/design/iconography/app-icon-construction).
