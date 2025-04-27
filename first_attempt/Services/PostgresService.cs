using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using Newtonsoft.Json;
using first_attempt.Models;
using System.Diagnostics;

namespace first_attempt.Services
{
    public class PostgresService
    {
        private readonly string _connectionString;

        public PostgresService()
        {
            _connectionString = "Host=localhost;Port=5432;Username=postgres;Password=10122002;Database=cme_db";
            EnsureTablesCreated();

        }

        private void EnsureTablesCreated()
        {
            try
            {
                 using var conn = new NpgsqlConnection(_connectionString);
                 conn.Open();

                var createTablesSql = @"
                CREATE TABLE IF NOT EXISTS cme_event (
                    activity_id TEXT PRIMARY KEY,
                    catalog TEXT,
                    start_time TEXT,
                    source_location TEXT,
                    active_region_num INT,
                    note TEXT,
                    submission_time TEXT,
                    version_id INT,
                    link TEXT
                );

                CREATE TABLE IF NOT EXISTS instrument (
                    id SERIAL PRIMARY KEY,
                    activity_id TEXT REFERENCES cme_event(activity_id),
                    display_name TEXT
                );

                CREATE TABLE IF NOT EXISTS cme_analysis (
                    id SERIAL PRIMARY KEY,
                    activity_id TEXT REFERENCES cme_event(activity_id),
                    is_most_accurate BOOLEAN,
                    time21_5 TEXT,
                    latitude DOUBLE PRECISION,
                    longitude DOUBLE PRECISION,
                    half_angle DOUBLE PRECISION,
                    speed DOUBLE PRECISION,
                    type TEXT,
                    feature_code TEXT,
                    image_type TEXT,
                    measurement_technique TEXT,
                    note TEXT,
                    level_of_data INT,
                    tilt DOUBLE PRECISION,
                    minor_half_width DOUBLE PRECISION,
                    speed_measured_at_height DOUBLE PRECISION,
                    submission_time TEXT,
                    link TEXT
                );

                CREATE TABLE IF NOT EXISTS enlil_entry (
                    id SERIAL PRIMARY KEY,
                    analysis_id INT REFERENCES cme_analysis(id),
                    model_completion_time TEXT,
                    au DOUBLE PRECISION,
                    estimated_shock_arrival_time TEXT,
                    estimated_duration TEXT,
                    rmin_re DOUBLE PRECISION,
                    kp_18 DOUBLE PRECISION,
                    kp_90 DOUBLE PRECISION,
                    kp_135 DOUBLE PRECISION,
                    kp_180 DOUBLE PRECISION,
                    is_earth_gb BOOLEAN,
                    link TEXT
                );

                CREATE TABLE IF NOT EXISTS impact_entry (
                    id SERIAL PRIMARY KEY,
                    enlil_id INT REFERENCES enlil_entry(id),
                    is_glancing_blow BOOLEAN,
                    location TEXT,
                    arrival_time TEXT
                );

                CREATE TABLE IF NOT EXISTS linked_event (
                    id SERIAL PRIMARY KEY,
                    activity_id TEXT REFERENCES cme_event(activity_id),
                    linked_activity_id TEXT
                );
            ";

                using var cmd = new NpgsqlCommand(createTablesSql, conn);
                cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw;
            }
            
        }

        public async Task SaveCMEEventAsync(CMEEvent cmeEvent)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var transaction = await conn.BeginTransactionAsync();

            try
            {
                var insertCmeEventSql = @"
                    INSERT INTO cme_event (activity_id, catalog, start_time, source_location, active_region_num, note, submission_time, version_id, link)
                    VALUES (@activity_id, @catalog, @start_time, @source_location, @active_region_num, @note, @submission_time, @version_id, @link)
                    ON CONFLICT (activity_id) DO NOTHING;
                ";

                await using (var cmd = new NpgsqlCommand(insertCmeEventSql, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("activity_id", cmeEvent.activityID);
                    cmd.Parameters.AddWithValue("catalog", cmeEvent.catalog ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("start_time", cmeEvent.startTime ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("source_location", cmeEvent.sourceLocation ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("active_region_num", cmeEvent.activeRegionNum ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("note", cmeEvent.note ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("submission_time", cmeEvent.submissionTime ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("version_id", cmeEvent.versionId);
                    cmd.Parameters.AddWithValue("link", cmeEvent.link ?? (object)DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                }

                if (cmeEvent.instruments != null)
                {
                    foreach (var instrument in cmeEvent.instruments)
                    {
                        var insertInstrumentSql = @"
                            INSERT INTO instrument (activity_id, display_name)
                            VALUES (@activity_id, @display_name);
                        ";
                        await using var cmd = new NpgsqlCommand(insertInstrumentSql, conn, transaction);
                        cmd.Parameters.AddWithValue("activity_id", cmeEvent.activityID);
                        cmd.Parameters.AddWithValue("display_name", instrument.displayName ?? (object)DBNull.Value);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                if (cmeEvent.cmeAnalyses != null)
                {
                    foreach (var analysis in cmeEvent.cmeAnalyses)
                    {
                        var insertAnalysisSql = @"
                            INSERT INTO cme_analysis (activity_id, is_most_accurate, time21_5, latitude, longitude, half_angle, speed, type, feature_code, image_type, measurement_technique, note, level_of_data, tilt, minor_half_width, speed_measured_at_height, submission_time, link)
                            VALUES (@activity_id, @is_most_accurate, @time21_5, @latitude, @longitude, @half_angle, @speed, @type, @feature_code, @image_type, @measurement_technique, @note, @level_of_data, @tilt, @minor_half_width, @speed_measured_at_height, @submission_time, @link)
                            RETURNING id;
                        ";
                        await using var cmd = new NpgsqlCommand(insertAnalysisSql, conn, transaction);
                        cmd.Parameters.AddWithValue("activity_id", cmeEvent.activityID);
                        cmd.Parameters.AddWithValue("is_most_accurate", analysis.isMostAccurate);
                        cmd.Parameters.AddWithValue("time21_5", analysis.time21_5 ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("latitude", analysis.latitude ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("longitude", analysis.longitude ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("half_angle", analysis.halfAngle ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("speed", analysis.speed ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("type", analysis.type ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("feature_code", analysis.featureCode ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("image_type", analysis.imageType ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("measurement_technique", analysis.measurementTechnique ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("note", analysis.note ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("level_of_data", analysis.levelOfData);
                        cmd.Parameters.AddWithValue("tilt", analysis.tilt ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("minor_half_width", analysis.minorHalfWidth ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("speed_measured_at_height", analysis.speedMeasuredAtHeight ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("submission_time", analysis.submissionTime ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("link", analysis.link ?? (object)DBNull.Value);

                        var analysisId = (int)await cmd.ExecuteScalarAsync();

                        if (analysis.enlilList != null)
                        {
                            foreach (var enlil in analysis.enlilList)
                            {
                                var insertEnlilSql = @"
                                    INSERT INTO enlil_entry (analysis_id, model_completion_time, au, estimated_shock_arrival_time, estimated_duration, rmin_re, kp_18, kp_90, kp_135, kp_180, is_earth_gb, link)
                                    VALUES (@analysis_id, @model_completion_time, @au, @estimated_shock_arrival_time, @estimated_duration, @rmin_re, @kp_18, @kp_90, @kp_135, @kp_180, @is_earth_gb, @link)
                                    RETURNING id;
                                ";
                                await using var cmdEnlil = new NpgsqlCommand(insertEnlilSql, conn, transaction);
                                cmdEnlil.Parameters.AddWithValue("analysis_id", analysisId);
                                cmdEnlil.Parameters.AddWithValue("model_completion_time", enlil.modelCompletionTime ?? (object)DBNull.Value);
                                cmdEnlil.Parameters.AddWithValue("au", enlil.au);
                                cmdEnlil.Parameters.AddWithValue("estimated_shock_arrival_time", enlil.estimatedShockArrivalTime ?? (object)DBNull.Value);
                                cmdEnlil.Parameters.AddWithValue("estimated_duration", enlil.estimatedDuration ?? (object)DBNull.Value);
                                cmdEnlil.Parameters.AddWithValue("rmin_re", enlil.rmin_re ?? (object)DBNull.Value);
                                cmdEnlil.Parameters.AddWithValue("kp_18", enlil.kp_18 ?? (object)DBNull.Value);
                                cmdEnlil.Parameters.AddWithValue("kp_90", enlil.kp_90 ?? (object)DBNull.Value);
                                cmdEnlil.Parameters.AddWithValue("kp_135", enlil.kp_135 ?? (object)DBNull.Value);
                                cmdEnlil.Parameters.AddWithValue("kp_180", enlil.kp_180 ?? (object)DBNull.Value);
                                cmdEnlil.Parameters.AddWithValue("is_earth_gb", enlil.isEarthGB);
                                cmdEnlil.Parameters.AddWithValue("link", enlil.link ?? (object)DBNull.Value);

                                var enlilId = (int)await cmdEnlil.ExecuteScalarAsync();

                                if (enlil.impactList != null)
                                {
                                    foreach (var impact in enlil.impactList)
                                    {
                                        var insertImpactSql = @"
                                            INSERT INTO impact_entry (enlil_id, is_glancing_blow, location, arrival_time)
                                            VALUES (@enlil_id, @is_glancing_blow, @location, @arrival_time);
                                        ";
                                        await using var cmdImpact = new NpgsqlCommand(insertImpactSql, conn, transaction);
                                        cmdImpact.Parameters.AddWithValue("enlil_id", enlilId);
                                        cmdImpact.Parameters.AddWithValue("is_glancing_blow", impact.isGlancingBlow);
                                        cmdImpact.Parameters.AddWithValue("location", impact.location ?? (object)DBNull.Value);
                                        cmdImpact.Parameters.AddWithValue("arrival_time", impact.arrivalTime ?? (object)DBNull.Value);
                                        await cmdImpact.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }
                    }
                }

                if (cmeEvent.linkedEvents != null)
                {
                    foreach (var linked in cmeEvent.linkedEvents)
                    {
                        var insertLinkedSql = @"
                            INSERT INTO linked_event (activity_id, linked_activity_id)
                            VALUES (@activity_id, @linked_activity_id);
                        ";
                        await using var cmd = new NpgsqlCommand(insertLinkedSql, conn, transaction);
                        cmd.Parameters.AddWithValue("activity_id", cmeEvent.activityID);
                        cmd.Parameters.AddWithValue("linked_activity_id", linked.activityID ?? (object)DBNull.Value);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<List<string>> GetAllActivityIdsAsync()
        {
            var ids = new List<string>();

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = "SELECT activity_id FROM cme_event";
            await using var cmd = new NpgsqlCommand(query, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                ids.Add(reader.GetString(0));
            }

            return ids;
        }

        public async Task<Dictionary<int, int>> GetEventCountsByYearAsync()
        {
            var result = new Dictionary<int, int>();

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = "SELECT start_time FROM cme_event";
            await using var cmd = new NpgsqlCommand(query, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var startTimeStr = reader.GetString(0);

                if (DateTime.TryParse(startTimeStr, out var startTime))
                {
                    var year = startTime.Year;
                    if (result.ContainsKey(year))
                        result[year]++;
                    else
                        result[year] = 1;
                }
            }

            return result;
        }

        public async Task<List<CMEEvent>> GetAllCMEEventsAsync()
        {
            var events = new List<CMEEvent>();

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var query = @"SELECT activity_id, catalog, start_time, source_location, active_region_num, note, submission_time, version_id, link FROM cme_event;";
            await using var cmd = new NpgsqlCommand(query, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var ev = new CMEEvent
                {
                    activityID = reader.IsDBNull(0) ? null : reader.GetString(0),
                    catalog = reader.IsDBNull(1) ? null : reader.GetString(1),
                    startTime = reader.IsDBNull(2) ? null : reader.GetString(2),
                    sourceLocation = reader.IsDBNull(3) ? null : reader.GetString(3),
                    activeRegionNum = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    note = reader.IsDBNull(5) ? null : reader.GetString(5),
                    submissionTime = reader.IsDBNull(6) ? null : reader.GetString(6),
                    versionId = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                    link = reader.IsDBNull(8) ? null : reader.GetString(8)
                };
                events.Add(ev);
            }

            return events;
        }




    }
}
