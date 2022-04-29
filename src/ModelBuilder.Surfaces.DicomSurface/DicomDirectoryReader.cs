using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Media;
using ModelBuilder.Surfaces.DicomSurface.Interfaces;
using ModelBuilder.Surfaces.DicomSurface.Internal;
using ModelBuilder.Surfaces.DicomSurface.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ModelBuilder.Surfaces.DicomSurface
{
    public class DicomDirectoryReader : IDicomDirectoryReader
    {
        private const string MaleSex = "M";
        private const string FemaleSex = "F";
        private const string OtherSex = "O";
        private const char PixelSpacingDelimiter = '\\';

        private readonly DicomSurfaceOptions _options;
        private readonly DicomDirectory _dicomDirectory;

        public DicomDirectoryReader(DicomDirectory dicomDirectory, DicomSurfaceOptions options)
        {
            _options = options;
            _dicomDirectory = dicomDirectory;
        }

        public static DicomDirectoryReader Open(string dicomdirFilename, DicomSurfaceOptions options)
        {
            var directory = DicomDirectory.Open(dicomdirFilename);
            return new DicomDirectoryReader(directory, options);
        }

        public List<Patient> GetAllPatients()
        {
            return _dicomDirectory.RootDirectoryRecordCollection
                .Select(patient => new Patient
                {
                    Id = patient.GetString(DicomTag.PatientID),
                    Name = GetValueOrDefault<string>(patient, DicomTag.PatientName),
                    Sex = GetPatientSex(GetValueOrDefault<string>(patient, DicomTag.PatientSex)),
                    Studies = patient.LowerLevelDirectoryRecordCollection
                        .Select(study => new Study
                        {
                            Id = study.GetString(DicomTag.StudyInstanceUID),
                            Description = GetValueOrDefault<string>(study, DicomTag.StudyDescription),
                            DateTime = GetDateTimeOrDefault(study, DicomTag.StudyDate, DicomTag.StudyTime),
                            Series = study.LowerLevelDirectoryRecordCollection
                                .Select(series => new Series
                                {
                                    Id = series.GetString(DicomTag.SeriesInstanceUID),
                                    Number = GetValueOrDefault<int>(series, DicomTag.SeriesNumber),
                                    Description = GetValueOrDefault<string>(series, DicomTag.SeriesDescription),
                                    ImagesCount = series.LowerLevelDirectoryRecordCollection.Count(),
                                    DateTime = GetDateTimeOrDefault(series, DicomTag.SeriesDate, DicomTag.SeriesTime),
                                }).ToList(),
                        }).ToList(),
                }).ToList();
        }

        public DicomSurface GetSurface(string seriesId)
        {
            return GetSurfaceAsync(seriesId).GetAwaiter().GetResult();
        }

        public async Task<DicomSurface> GetSurfaceAsync(string seriesId)
        {
            var imageFiles = await GetDicomImageFiles(seriesId);
            var layers = imageFiles.Select(x => new { Image = new DicomImage(x.Dataset).RenderImage().AsSharpImage(), x.Dataset })
                .Select(x => new DicomLayer(x.Image)
                {
                    Thickness = x.Dataset.GetSingleValue<float>(DicomTag.SliceThickness),
                    Offset = x.Dataset.GetSingleValue<float>(DicomTag.SliceLocation),
                    PixelSpacing = GetPixelSpacing(x.Dataset),
                }).ToList();
           
            return new DicomSurface(layers, _options);
        }

        private async Task<DicomFile[]> GetDicomImageFiles(string seriesId)
        {
            var series = _dicomDirectory.RootDirectoryRecordCollection
                .SelectMany(x => x.LowerLevelDirectoryRecordCollection)
                .SelectMany(x => x.LowerLevelDirectoryRecordCollection)
                .First(x => x.GetString(DicomTag.SeriesInstanceUID) == seriesId);

            var imageFileIds = series.LowerLevelDirectoryRecordCollection
                .Select(x => x.GetString(DicomTag.ReferencedFileID))
                .ToList();

            var basePath = _dicomDirectory.File.Directory.Name;
            var tasks = imageFileIds.Select(async x => await DicomFile.OpenAsync(Path.Combine(basePath, x)));
            return await Task.WhenAll(tasks);
        }

        private static T GetValueOrDefault<T>(DicomDirectoryRecord record, DicomTag tag)
        {
            if (record.TryGetSingleValue<T>(tag, out var value))
            {
                return value;
            }

            return default;
        }

        private static DateTime? GetDateTimeOrDefault(DicomDirectoryRecord record, DicomTag dateTag, DicomTag timeTag)
        {
            if (!record.TryGetSingleValue<DateTime>(dateTag, out var date))
            {
                return null;
            }

            if (!record.TryGetSingleValue<DateTime>(timeTag, out var time))
            {
                return null;
            }

            return new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second);
        }

        private static PatientSex GetPatientSex(string sex)
        {
            return sex switch
            {
                MaleSex => PatientSex.Male,
                FemaleSex => PatientSex.Female,
                OtherSex => PatientSex.Other,
                _ => PatientSex.NotSpecified,
            };
        }

        private static (float X, float Y) GetPixelSpacing(DicomDataset dataset)
        {
            var pixelSpacing = dataset.GetString(DicomTag.PixelSpacing);
            var rowSpacing = pixelSpacing.Substring(0, pixelSpacing.IndexOf(PixelSpacingDelimiter));
            var columnSpacing = pixelSpacing.Substring(pixelSpacing.IndexOf(PixelSpacingDelimiter) + 1);

            return (float.Parse(rowSpacing, CultureInfo.InvariantCulture), float.Parse(columnSpacing, CultureInfo.InvariantCulture));
        }
    }
}
