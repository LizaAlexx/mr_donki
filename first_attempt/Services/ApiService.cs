using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using first_attempt.Models;
using Newtonsoft.Json;

namespace first_attempt.Services
{
    public class ApiService
    {
        private readonly string _apiKey;
        private static readonly string _baseUrl = "https://api.nasa.gov";

        public ApiService(string apiKey = "xy2VnttWto2zYKcZLebLUckKLpXjjhO3pOl4DJmJ")
        {
            _apiKey = apiKey;
        }

        // Формирует URL и делает HTTP-запрос
        public async Task<string> GetJsonRequest(string path, DateOnly startDate, DateOnly endDate)
        {
            string url = $"{_baseUrl}/DONKI/{path}?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}&api_key={_apiKey}";
            Debug.WriteLine(url);

            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                return await response.Content.ReadAsStringAsync();
            }
        }

        // Получает список событий CME за указанный период
        public async Task<List<CMEEvent>> GetCMEEvents(DateOnly startDate, DateOnly endDate)
        {
            string json = await GetJsonRequest("CME", startDate, endDate);

            try
            {
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    Error = (sender, args) =>
                    {
                        Console.WriteLine($"❌ Ошибка десериализации: {args.ErrorContext.Error.Message}");
                        args.ErrorContext.Handled = true;
                    }
                };

                var result = JsonConvert.DeserializeObject<List<CMEEvent>>(json, settings);
                return result ?? new List<CMEEvent>();
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Ошибка при разборе JSON: {ex.Message}");
                return new List<CMEEvent>();
            }
        }


        // Получает все CME-события с 2017 года по сегодняшний день
        public async Task<List<CMEEvent>> DownloadAllCMEEventsAsync()
        {
            var allEvents = new List<CMEEvent>();
            var start = new DateOnly(2017, 1, 1);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            while (start < today)
            {
                var end = start.AddDays(29);
                if (end > today)
                    end = today;

                Console.WriteLine($"🔄 Запрос: {start} — {end}");

                try
                {
                    var chunk = await GetCMEEvents(start, end);
                    if (chunk != null && chunk.Count > 0)
                    {
                        allEvents.AddRange(chunk);
                        Console.WriteLine($"✅ Получено {chunk.Count} записей.");
                    }
                    else
                    {
                        Console.WriteLine("⚠️ Нет данных за период.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка на диапазоне {start} — {end}: {ex.Message}");
                }

                start = end.AddDays(1);
            }

            Console.WriteLine($"📦 Всего загружено событий: {allEvents.Count}");
            return allEvents;
        }

        // Вспомогательный тестовый метод
        public async void test()
        {
            Debug.WriteLine("API Service test");
            var result = await GetJsonRequest("CME", DateOnly.Parse("2017-01-01"), DateOnly.Parse("2017-01-31"));
            Debug.WriteLine(result);
        }

        public async Task DownloadAllToPostgresOnlyAsync()
        {
            var pg = new PostgresService();
            var start = new DateOnly(2017, 1, 1);
            var endDate = new DateOnly(2025, 3, 31);

            while (start < endDate)
            {
                var end = start.AddDays(29);
                if (end > endDate)
                    end = endDate;

                Console.WriteLine($"[PostgreSQL] Запрос за период {start} — {end}");

                try
                {
                    var chunk = await GetCMEEvents(start, end);
                    foreach (var ev in chunk)
                    {
                        await pg.SaveCMEEventAsync(ev);
                        Console.WriteLine($"✔ Добавлено: {ev.activityID}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка на периоде {start} — {end}: {ex.Message}");
                }

                start = end.AddDays(1);
            }

            Console.WriteLine("✅ Все данные успешно сохранены в PostgreSQL.");
        }

        public async Task DownloadAllToBothAsync()
        {
            var pg = new PostgresService();
            var couch = new CouchDbService();
            var start = new DateOnly(2017, 1, 1);
            var endDate = DateOnly.FromDateTime(DateTime.UtcNow);

            while (start < endDate)
            {
                var end = start.AddDays(29);
                if (end > endDate)
                    end = endDate;

                Console.WriteLine($"[Both] Запрос за период {start} — {end}");

                try
                {
                    var chunk = await GetCMEEvents(start, end);
                    foreach (var ev in chunk)
                    {
                        await pg.SaveCMEEventAsync(ev);
                        await couch.SaveCMEEventAsync(ev);
                        Console.WriteLine($"✔ Сохранено в обе БД: {ev.activityID}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка на периоде {start} — {end}: {ex.Message}");
                }

                start = end.AddDays(1);
            }

            Console.WriteLine("✅ Данные успешно сохранены в CouchDB и PostgreSQL.");
        }


    }
}
