# Модуль размещения объявлений (HttpListener, C#, MSTest)

## Запуск
    dotnet run --project AdsServer
Сайт: http://localhost:9999/connection/ads  (хост, порт и путь — в AdsServer/settings.json)

## Тесты
    dotnet test

## Маршруты
| Метод | URL | Успех | Ошибки |
|-------|-----|-------|--------|
| GET  | /ads?category=... | 200 | 400 |
| GET  | /ads/details?id=1 | 200 | 400, 404 |
| POST | /ads/create (title, category) | 303 | 400, 409 |
| PUT  | /ads/update?id=1 (title, category, status) | 204 | 400, 404, 409 |

## Проверка PUT (консоль браузера, F12)
    fetch('/connection/ads/update?id=1', {method:'PUT',
      headers:{'Content-Type':'application/x-www-form-urlencoded'},
      body:new URLSearchParams({title:'Ноутбук Dell',category:'electronics',status:'archived'})
    }).then(r=>console.log(r.status))
