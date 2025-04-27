using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyCouch;
using MyCouch.Requests;
using MyCouch.Responses;
using first_attempt.Models;

namespace first_attempt.Services
{
    /// <summary>
    /// Сервис работы с CouchDB (через пакет MyCouch 1.3.0).
    /// </summary>
    public sealed class CouchDbService : IDisposable
    {
        private const string DbName = "cme_events";

        private readonly MyCouchServerClient _server;   // операции уровня сервера
        private readonly MyCouchClient _client;   // операции уровня БД

        public CouchDbService()
        {
            var conn = "http://admin:10122002@localhost:5984";   // строка подключения

            _server = new MyCouchServerClient(conn);             // «серверный» клиент
            _server.Databases.PutAsync(DbName).GetAwaiter().GetResult(); // создаём БД, если нет

            _client = new MyCouchClient(conn, DbName);           // «базовый» клиент
        }

        /// <summary>Создать или обновить документ <see cref="CMEEvent"/>.</summary>
        public async Task SaveCMEEventAsync(CMEEvent doc)
        {
            doc.Id = doc.activityID;                             // _id => activityID

            // получаем текущую ревизию (если документ существует)
            var head = await _client.Entities.GetAsync<CMEEvent>(doc.Id);  // Rev есть у ответа :contentReference[oaicite:0]{index=0}
            if (head.IsSuccess && !head.IsEmpty)
                doc.Rev = head.Rev;

            // PUT – создаёт новый или обновляет старый документ
            var resp = await _client.Entities.PutAsync(doc);
            Console.WriteLine(resp.IsSuccess
                ? $"Сохранено: {doc.Id}"
                : $"Ошибка {doc.Id}: {resp.Reason}");
        }

        /// <summary>Вернуть все документы базы без какого-либо лимита.</summary>
        public async Task<List<CMEEvent>> GetAllCMEEventsAsync()
        {
            var req = new QueryViewRequest("_all_docs")          // системный view :contentReference[oaicite:1]{index=1}
                .Configure(q => q.IncludeDocs(true));            // добавляем полные документы

            // Берём value как string, а includedDoc как CMEEvent :contentReference[oaicite:2]{index=2}
            var resp = await _client.Views.QueryAsync<string, CMEEvent>(req);

            if (!resp.IsSuccess)
                throw new Exception($"_all_docs error: {resp.Reason}");

            return resp.Rows
                       .Select(r => r.IncludedDoc)               // сам документ
                       .Where(d => d != null)
                       .ToList();
        }

        /// <summary>Вернуть список всех <c>activityID</c> в базе.</summary>
        public async Task<List<string>> GetAllActivityIdsAsync()
        {
            // самый простой способ – взять ранее полученные документы
            var docs = await GetAllCMEEventsAsync();

            return docs.Where(d => !string.IsNullOrEmpty(d.activityID))
                       .Select(d => d.activityID)
                       .ToList();
        }

        public void Dispose()
        {
            _client?.Dispose();
            _server?.Dispose();
        }
    }
}
