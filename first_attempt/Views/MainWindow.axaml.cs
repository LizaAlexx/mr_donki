using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using first_attempt.Services;
using first_attempt.Models; // обязательно для доступа к CMEEvent и его структурам
using System.Threading.Tasks;
using System.Collections.Generic;

namespace first_attempt.Views
{
    public partial class MainWindow : Window
    {
        private readonly ApiService? _apiService= new ApiService();
        private readonly CouchDbService? _couchDbService= new CouchDbService();
        private readonly PostgresService? _postgresService = new PostgresService();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await _apiService.DownloadAllToBothAsync(); // теперь загружает в обе БД
            await CompareDatabasesAsync();              // затем сразу сравнение
        }

        private async Task CompareDatabasesAsync()
        {
            /*var couchIds = await _couchDbService.GetAllActivityIdsAsync();
            var pgIds = await _postgresService.GetAllActivityIdsAsync();

            var onlyInCouch = couchIds.Except(pgIds).ToList();
            var onlyInPostgres = pgIds.Except(couchIds).ToList();
            var inBoth = couchIds.Intersect(pgIds).ToList();

            Console.WriteLine($"Всего в CouchDB: {couchIds.Count}");
            Console.WriteLine($"Всего в PostgreSQL: {pgIds.Count}");
            Console.WriteLine($"Общие записи: {inBoth.Count}");
            Console.WriteLine($"Только в CouchDB: {onlyInCouch.Count}");
            Console.WriteLine($"Только в PostgreSQL: {onlyInPostgres.Count}");

            UpdateComparisonResultListBox(couchIds, pgIds, onlyInCouch, onlyInPostgres, inBoth);*/
        }
        private async void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            await CompareDatabasesAsync();
        }

        private void UpdateComparisonResultListBox(
            List<string> couchIds, 
            List<string> pgIds, 
            List<string> onlyInCouch, 
            List<string> onlyInPostgres, 
            List<string> inBoth)
        {
            var displayList = new List<string>
            {
                $"Всего в CouchDB: {couchIds.Count}",
                $"Всего в PostgreSQL: {pgIds.Count}",
                $"Общие записи: {inBoth.Count}",
                $"Только в CouchDB: {onlyInCouch.Count}",
                $"Только в PostgreSQL: {onlyInPostgres.Count}",
                "---- Только в CouchDB ----"
            };

            displayList.AddRange(onlyInCouch);

            displayList.Add("---- Только в PostgreSQL ----");
            displayList.AddRange(onlyInPostgres);

            displayList.Add("---- Общие записи ----");
            displayList.AddRange(inBoth);

            // Правильное назначение Items через свойство ItemsSource
            ComparisonResultListBox.ItemsSource = displayList;
        }

        private async void OnSync_Click(object sender, RoutedEventArgs e)
        {
            CouchDbService _couchDbService = new CouchDbService();
            PostgresService _postgresService = new PostgresService();

            // Получаем все события из CouchDB
            var stringCouchData = await _couchDbService.GetAllActivityIdsAsync();
            var couchData = await _couchDbService.GetAllCMEEventsAsync();
            Console.WriteLine($"Найдено в CouchDB: {couchData.Count} записей.");

            // Получаем все существующие ID из PostgreSQL
            var postgresIds = await _postgresService.GetAllActivityIdsAsync();
            Console.WriteLine($"Найдено в PostgreSQL: {postgresIds.Count} записей.");

            int newRecords = 0;

            foreach (var couchEvent in couchData)
            {
                if (!postgresIds.Contains(couchEvent.activityID))
                {
                    await _postgresService.SaveCMEEventAsync(couchEvent);
                    newRecords++;
                    Console.WriteLine($"✔ Добавлена новая запись: {couchEvent.activityID}");
                }
                else
                {
                    Console.WriteLine($"⏩ Пропущена существующая запись: {couchEvent.activityID}");
                }
            }

            Console.WriteLine($"✅ Синхронизация завершена. Добавлено новых записей: {newRecords}");
        }

        private async void OnShowStats_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Очистить список перед выводом новой статистики
                ComparisonResultListBox.ItemsSource = null;

                PostgresService postgresService = new PostgresService();

                // Получаем общее число событий
                var allEvents = await postgresService.GetAllCMEEventsAsync();
                int totalEvents = allEvents.Count;
                int eventsWithAnalysis = allEvents.Count(e => e.cmeAnalyses != null && e.cmeAnalyses.Count > 0);
                int eventsWithoutAnalysis = totalEvents - eventsWithAnalysis;

                double avgSpeed = allEvents
                    .Where(e => e.cmeAnalyses != null)
                    .SelectMany(e => e.cmeAnalyses)
                    .Where(a => a.speed.HasValue)
                    .Select(a => a.speed.Value)
                    .DefaultIfEmpty(0)
                    .Average();

                // Получаем количество событий по годам
                var eventsByYear = await postgresService.GetEventCountsByYearAsync();

                var stats = new List<string>
        {
            $"Общее число CME событий: {totalEvents}",
            $"Из них с анализами: {eventsWithAnalysis}",
            $"Из них без анализов: {eventsWithoutAnalysis}",
            $"Средняя скорость CME (по анализам): {avgSpeed:F2} км/с",
            "",
            "Статистика по годам:"
        };

                foreach (var year in eventsByYear.OrderBy(e => e.Key))
                {
                    stats.Add($"Год {year.Key}: {year.Value} событий");
                }

                ComparisonResultListBox.ItemsSource = stats;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выводе статистики: {ex.Message}");
            }
        }




    }
}
