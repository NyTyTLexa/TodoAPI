# TodoAPI - Микросервис управления задачами
Простой и функциональный API для работы с задачами на .NET 6 + PostgreSQL.
## Быстрый старт
### Что нужно установить
- Docker и Docker Compose
- .NET 6 SDK (если планируете разрабатывать)
### Запуск
```bash
git clone https://github.com/NyTyTLexa/TodoAPI.git
cd TodoAPI
docker-compose up -d
```
API будет доступен по адресу: http://localhost:80
Swagger UI: http://localhost:5000/swagger
## Что реализовано дополнительно
### Redis кэширование
Добавил кэширование для GET запросов. Кэш живет 5 минут и автоматически сбрасывается при изменении данных.
Например, первый запрос к `/api/tasks/all` идет в БД, последующие - из Redis до тех пор, пока не создадим/обновим/удалим задачу.
```csharp
// В контроллере проверяем кэш
var cacheKey = "all_tasks";
var cachedTasks = await _cacheService.GetAsync<List<Tasks>>(cacheKey);
if (cachedTasks != null) 
    return Ok(cachedTasks);
// Если в кэше нет - идем в БД и сохраняем результат
var tasks = await _context.Tasks.ToListAsync();
await _cacheService.SetAsync(cacheKey, tasks, TimeSpan.FromMinutes(5));
```
### RabbitMQ для событий
При изменении статуса задачи отправляется сообщение в очередь `task.status.changed`.
```json
{
  "taskId": 1,
  "newStatus": "completed", 
  "oldStatus": "active",
  "changedAt": "2024-03-15T12:00:00Z"
}
```
Это полезно, если потом нужно будет добавить уведомления, аналитику или интеграцию с другими системами.
### gRPC сервис
Реализовал gRPC сервис для получения статистики по задачам. Работает быстрее обычного REST API.
```proto
service TodoAnalytics {
  rpc GetStats (StatsRequest) returns (StatsResponse);
}
message StatsResponse {
  int32 active_tasks = 1;
  int32 completed_tasks = 2;
}
```
### Unit тесты
Покрыл тестами основную логику:
- Контроллеры (CRUD операции)
- Сервис кэширования
- Валидацию данных
Запуск тестов: `dotnet test`
## Архитектурные решения
### Структура проекта
```
TodoAPI/
├── Controllers/          # REST контроллеры
├── Models/              # Модели данных  
├── Data/                # EF Core контекст
├── Services/            # Бизнес логика
├── Grpc/                # gRPC сервисы
└── Program.cs           # Настройка приложения
```
### База данных
Использую PostgreSQL с Entity Framework Core в режиме Code-First. Миграции применяются автоматически при запуске.
Модель задачи простая:
```csharp
public class Tasks
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } // "active" или "completed"
    public DateTime CreatedAt { get; set; }
}
```
### Кэширование
Сделал двухуровневое кэширование:
1. Стандартный `IDistributedCache` от ASP.NET Core
2. Собственный `CacheService` для удобной работы с объектами
Кэш инвалидируется при любом изменении данных (POST/PUT/DELETE), чтобы не отдавать устаревшие данные.
### Очереди сообщений
RabbitMQ настроен по стандартной схеме Publisher-Subscriber. Сообщения отправляются в exchange `todo.events`, откуда их могут читать разные сервисы.
### Контейнеризация
Docker Compose поднимает весь стек:
- API приложение
- PostgreSQL
- Redis
- RabbitMQ с веб-интерфейсом
Все настроено для разработки и тестирования. Для продакшена нужно будет добавить переменные окружения для паролей и настроек.
### Обработка ошибок
Стандартная обработка ошибок ASP.NET Core. Возвращаю правильные HTTP статусы:
- 200 OK для успешных GET
- 201 Created для POST
- 204 No Content для PUT/DELETE  
- 404 Not Found если задача не найдена
- 400 Bad Request при ошибках валидации
### Тестирование xunit
## Локальная разработка
Если хотите запустить без Docker:
```bash
# Поднять только зависимости
docker-compose up postgres redis rabbitmq -d
# Обновить строки подключения в appsettings.json
# Применить миграции
dotnet ef database update
# Запустить приложение
dotnet run
```
API будет на https://localhost:80,http://localhost:5000,http://localhost:5001
