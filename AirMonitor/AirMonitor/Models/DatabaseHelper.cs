using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AirMonitor.Models;
using AirMonitor.Models.Tables;
using Newtonsoft.Json;
using SQLite;

namespace AirMonitor.Helpers
{
    public class DatabaseHelper : IDisposable
    {
        private SQLiteConnection db;

        public void Initialize()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AirMonitorDatabase.db");

            db = new SQLiteConnection(path, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);

            db.CreateTable<InstallationEntity>();
            db.CreateTable<MeasurementEntity>();
            db.CreateTable<MeasurementItemEntity>();
            db.CreateTable<MeasurementValue>();
            db.CreateTable<AirQualityIndex>();
            db.CreateTable<AirQualityStandard>();
        }

        public void SaveInstallations(IEnumerable<Installation> installations)
        {
            var entries = installations.Select(installation => new InstallationEntity(installation));

            db.DeleteAll<InstallationEntity>();
            db.InsertAll(entries);
        }

        public IEnumerable<Installation> GetInstallations()
        {
            IEnumerable<Installation> installations = db.Table<InstallationEntity>().Select(installation => new Installation(installation));
            return installations;
        }

        public void SaveMeasurements(IEnumerable<Measurement> measurements)
        {
            db.DeleteAll<MeasurementValue>();
            db.DeleteAll<AirQualityIndex>();
            db.DeleteAll<AirQualityStandard>();
            db.DeleteAll<MeasurementItemEntity>();
            db.DeleteAll<MeasurementEntity>();

            foreach (var measurement in measurements)
            {
                db.InsertAll(measurement.Current.Values);
                db.InsertAll(measurement.Current.Indexes);
                db.InsertAll(measurement.Current.Standards);

                var measurementItemEntity = new MeasurementItemEntity(measurement.Current);
                db.Insert(measurementItemEntity);

                var measurementEntity = new MeasurementEntity(measurementItemEntity.Id, measurement.Installation.Id);
                db.Insert(measurementEntity);
            }
        }

        public IEnumerable<Measurement> GetMeasurements()
        {
            return db.Table<MeasurementEntity>().Select(s =>
            {
                var measurementItem = GetMeasurementItem(s.CurrentMeasurementItemId);
                var installation = GetInstallation(s.InstallationId);
                return new Measurement(measurementItem, installation);
            });
        }

        private MeasurementItem GetMeasurementItem(int id)
        {
            var entity = db.Get<MeasurementItemEntity>(id);
            var valueIds = JsonConvert.DeserializeObject<int[]>(entity.MeasurementValueIds);
            var indexIds = JsonConvert.DeserializeObject<int[]>(entity.AirQualityIndexIds);
            var standardIds = JsonConvert.DeserializeObject<int[]>(entity.AirQualityStandardIds);
            var values = db.Table<MeasurementValue>().Where(s => valueIds.Contains(s.Id)).ToArray();
            var indexes = db.Table<AirQualityIndex>().Where(s => indexIds.Contains(s.Id)).ToArray();
            var standards = db.Table<AirQualityStandard>().Where(s => standardIds.Contains(s.Id)).ToArray();
            return new MeasurementItem(entity, values, indexes, standards);
        }

        private Installation GetInstallation(string id)
        {
            var entity = db.Get<InstallationEntity>(id);
            return new Installation(entity);
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    db.Dispose();
                    db = null;
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion IDisposable Support
    }
}