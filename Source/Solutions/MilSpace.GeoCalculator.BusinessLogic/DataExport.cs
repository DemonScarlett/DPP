﻿using MilSpace.GeoCalculator.BusinessLogic.Interfaces;
using MilSpace.GeoCalculator.BusinessLogic.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace MilSpace.GeoCalculator.BusinessLogic
{
    public class DataExport : IDataExport
    {
        public async Task ExportProjectionsToCsvAsync(List<PointModel> pointModels, string path)
        {
            using (var streamWriter = File.Open(path, FileMode.Create))
            {
                var csvString = SerializeToCSV(pointModels);
                var bytes = new UTF8Encoding(true).GetBytes(csvString);
                streamWriter.Write(bytes, 0, bytes.Length);
                await streamWriter.FlushAsync();
            }
        }

        public async Task ExportProjectionsToCsvAsync(PointModel pointModel, string path)
        {
            using (var streamWriter = File.Open(path, FileMode.Create))
            {
                var csvString = SerializeToCSV(new List<PointModel> { pointModel });
                var bytes = new UTF8Encoding(true).GetBytes(csvString);
                streamWriter.Write(bytes, 0, bytes.Length);
                await streamWriter.FlushAsync();
            }
        }

        public async Task ExportProjectionsToXmlAsync(List<PointModel> pointModels, string path)
        {
            var pointModelsList = new PointModelsList() { PointList = pointModels };
            var xmlSerializer = new XmlSerializer(typeof(PointModelsList));
            using (var streamWriter = File.Open(path, FileMode.Create))
            {
                using (XmlWriter writer = XmlWriter.Create(streamWriter, new XmlWriterSettings() { Async = true }))
                {
                    xmlSerializer.Serialize(writer, pointModelsList);
                    await writer.FlushAsync();
                }
            }
        }

        public async Task ExportProjectionsToXmlAsync(PointModel pointModel, string path)
        {
            var xmlSerializer = new XmlSerializer(typeof(PointModel));
            using (var streamWriter = File.Open(path, FileMode.Create))
            {
                using (XmlWriter writer = XmlWriter.Create(streamWriter, new XmlWriterSettings() { Async = true }))
                {
                    xmlSerializer.Serialize(writer, pointModel);
                    await writer.FlushAsync();
                }
            }
        }

        public string GetStringRepresentationOfProjections(List<PointModel> pointModels)
        {
            if (pointModels == null || !pointModels.Any()) return string.Empty;

            if (pointModels.Count == 1) return pointModels.First().ToString();

            var stringBuilder = new StringBuilder();

            foreach (var point in pointModels)
            {
                stringBuilder.AppendLine(point.ToString(true));
            }

            return stringBuilder.ToString();
        }

        private string SerializeToCSV(List<PointModel> items)
        {
            var output = "";
            var delimiter = ';';
            var properties = typeof(PointModel).GetProperties().Where(n =>
             n.PropertyType == typeof(int?)
             || n.PropertyType == typeof(double));

            using (var sw = new StringWriter())
            {
                var header = properties
                .Select(n => n.Name)
                .Aggregate((a, b) => a + delimiter + b);
                sw.WriteLine(header);
                foreach (var item in items)
                {
                    var row = properties
                    .Select(n => n.GetValue(item, null))
                    .Select(n => n == null ? "null" : n.ToString()).Aggregate((a, b) => a + delimiter + b);
                    sw.WriteLine(row);
                }
                output = sw.ToString();
            }
            return output;
        }
    }
}
