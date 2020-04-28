using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using MilSpace.Configurations;
using MilSpace.Core.Tools;
using MilSpace.DataAccess.DataTransfer;
using MilSpace.DataAccess.Facade;
using MilSpace.Tools.SurfaceProfile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MilSpace.Tools
{
    public class BestOPParametersManager
    {
        private Dictionary<int, short> visibilityPercents = new Dictionary<int, short>();

        // TODO DS: Check this class and optimize it
        public static IFeatureClass CreateOPFeatureClass(WizardResult calcResult, IFeatureClass observatioPointsFeatureClass, IActiveView activeView, IRaster raster)
        {
            var observPointTemporaryFeatureClass = GdbAccess.Instance.GenerateTemporaryObservationPointFeatureClass(observatioPointsFeatureClass.Fields, "VO_Calculations_OPVisibility");

            double maxDistance = 0;
            double minDistance = 0;

            calcResult.ObservationStation.Project(EsriTools.Wgs84Spatialreference);

            var observationStationPolygon = calcResult.ObservationStation as IPolygon;
            var observationStationPointCollection = observationStationPolygon as IPointCollection;

            var observerPointGeometry = new PointClass
            {
                X = calcResult.ObservationPoint.X.Value,
                Y = calcResult.ObservationPoint.Y.Value,
                SpatialReference = EsriTools.Wgs84Spatialreference
            };

            observerPointGeometry.AddZCoordinate(raster);
            observerPointGeometry.ZAware = true;

            calcResult.ObservationStation.Project(activeView.FocusMap.SpatialReference);
            observerPointGeometry.Project(activeView.FocusMap.SpatialReference);

            for (int i = 0; i < observationStationPointCollection.PointCount; i++)
            {

                var line = new Line()
                {
                    FromPoint = observerPointGeometry,
                    ToPoint = observationStationPointCollection.Point[i],
                    SpatialReference = observerPointGeometry.SpatialReference
                };

                if (i == 0)
                {
                    maxDistance = line.Length;
                    minDistance = line.Length;
                }
                else
                {
                    if (maxDistance < line.Length)
                    {
                        maxDistance = line.Length;
                    }

                    if (minDistance > line.Length)
                    {
                        minDistance = line.Length;
                    }
                }
            }

            bool isCircle;
            double maxAzimuth;
            double minAzimuth;

            var points = EsriTools.GetPointsFromGeometries(new IGeometry[] { calcResult.ObservationStation },
                                                            observerPointGeometry.SpatialReference,
                                                            out isCircle).ToArray();

            bool isPointInside = EsriTools.IsPointOnExtent(EsriTools.GetEnvelopeOfGeometriesList(new IGeometry[] { calcResult.ObservationStation }),
                                                            observerPointGeometry);

            EsriTools.CreateDefaultPolylinesForFun(observerPointGeometry, points, new IGeometry[] { calcResult.ObservationStation },
                                                                                   isCircle, isPointInside, maxDistance, out minAzimuth, out maxAzimuth, out double maxLength).ToList();


            for (var currentHeight = calcResult.FromHeight; currentHeight <= calcResult.ToHeight; currentHeight += calcResult.Step)
            {
                var height = observerPointGeometry.Z + currentHeight;
                // Find angle to the farthest point of observation station
                var maxTiltAngle = EsriTools.FindAngleByDistanceAndHeight(height, maxDistance);

                // Find angle to the nearest point of observation station
                var minTiltAngle = EsriTools.FindAngleByDistanceAndHeight(height, minDistance);

                // Create observation point copy with changing height, distance and angles values
                var currentObservationPoint = new ObservationPoint();
                currentObservationPoint = calcResult.ObservationPoint.ShallowCopy();
                currentObservationPoint.RelativeHeight = currentHeight;
                currentObservationPoint.AngelMinH = minTiltAngle;
                currentObservationPoint.AngelMaxH = maxTiltAngle;
                currentObservationPoint.AzimuthStart = minAzimuth;
                currentObservationPoint.AzimuthEnd = maxAzimuth;
                currentObservationPoint.InnerRadius = minDistance;
                currentObservationPoint.OuterRadius = maxDistance;

                GdbAccess.Instance.AddObservPoint(observerPointGeometry, currentObservationPoint, observPointTemporaryFeatureClass);
            }

            return observPointTemporaryFeatureClass;
        }

        public static IFeatureClass CreateOOFeatureClass(IGeometry geometry, IActiveView activeView)
        {
            var featureClass = GdbAccess.Instance.GenerateTempStorage("ObservationStationGeometry", null,
                                                            esriGeometryType.esriGeometryPolygon, activeView, false, true);

            GdbAccess.Instance.AddGeometryToFeatureClass(geometry, featureClass);

            return featureClass;
        }
        //TODO DS: Don`t forget to remove temp storages
        //public static bool FindBestOPParameters(IFeatureClass observPointFeatureClass,
        //                                        IFeatureClass observStationFeatureClass, int[] observStationsIds,
        //                                        int[] observPointsIds,
        //                                        string taskId, string rasterSource, int expectedVisibilityPercent)
        //{
        //    //TODO DS: Deal with it
        //    IEnumerable<string> messages = null;

        //    var observationStationPolygon = observStationFeatureClass.GetFeature(observStationsIds.First()).ShapeCopy as IPolygon;

        //    var visibilityPercents = new Dictionary<int, short>();
        //    //TODO DS: Find visibility for each point and check visibility percent, set all suitable variants (OP rows) to the VO table
        //    //IQueryFilter queryFilter = new QueryFilter();
        //    //queryFilter.WhereClause = $"{observPointFeatureClass.OIDFieldName} >= 0";

        //    //var idFieldIndex = observPointFeatureClass.FindField(observPointFeatureClass.OIDFieldName);

        //    //IFeatureCursor featureCursor = observPointFeatureClass.Search(queryFilter, true);
        //    //IFeature feature = featureCursor.NextFeature();
        //    // TODO DS: maybe use GetAllIdsFromFeatureClass
        //    try
        //    {
        //        foreach(var pointId in observPointsIds)
        //        { 
        //            //id = (int)feature.Value[idFieldIndex];
        //            ////curPoints.Key is VisibilityCalculationresultsEnum.ObservationPoints or VisibilityCalculationresultsEnum.ObservationPointSingle

        //            //var pointId = (int)feature.Value[idFieldIndex];
        //            var observPointFeatureClassName = VisibilityTask.GetResultName(VisibilityCalculationResultsEnum.ObservationPoints, taskId, pointId);
        //            string visibilityArePolyFCName = string.Empty;
        //            //var exportedFeatureClass = GdbAccess.Instance.ExportObservationFeatureClass(
        //            //    obserpPointsfeatureClass as IDataset,
        //            //    observPointFeatureClassName,
        //            //    curPoints.Value);


        //            //if (string.IsNullOrWhiteSpace(exportedFeatureClass))
        //            //{
        //            //    string errorMessage = $"The feature calss {observPointFeatureClassName} was not exported";
        //            //    result.Exception = new MilSpaceVisibilityCalcFailedException(errorMessage);
        //            //    result.ErrorMessage = errorMessage;
        //            //    logger.ErrorEx("> ProcessObservationPoint ERROR ExportObservationFeatureClass. errorMessage:{0}", errorMessage);
        //            //    results.Add("Помилка: " + errorMessage + " ПС: " + pointId.ToString());
        //            //    return messages;
        //            //}

        //            //results.Add(iStepNum.ToString() + ". " + "Створено копію ПС для розрахунку: " + exportedFeatureClass);
        //            //iStepNum++;

        //            //Generate Visibility Raster
        //            // TODO DS: Remove this initialization
        //            string featureClass = observPointFeatureClassName;
        //            string outImageName = VisibilityTask.GetResultName(
        //                VisibilityCalculationResultsEnum.VisibilityAreaRasterSingle,
        //                taskId,
        //                pointId);
        //            // TODO DS: Maybe you don`t need this calling in controller
        //            if (!CalculationLibrary.GenerateVisibilityData(
        //                rasterSource,
        //                featureClass,
        //                VisibilityAnalysisTypesEnum.Frequency,
        //                outImageName,
        //                messages))
        //            {
        //                //TODO DS: Set error messages
        //                //string errorMessage = $"The result {outImageName} was not generated";
        //                //result.Exception = new MilSpaceVisibilityCalcFailedException(errorMessage);
        //                //result.ErrorMessage = errorMessage;
        //                //logger.ErrorEx("> ProcessObservationPoint ERROR ConvertRasterToPolygon. errorMessage:{0}", errorMessage);
        //                //results.Add("Помилка: " + errorMessage + " ПС: " + pointId.ToString());
        //                //return messages;
        //            }
        //            else
        //            {
        //                //results.Add(iStepNum.ToString() + ". " + "Розраховано видимість: " + outImageName + " ПС: " + pointId.ToString());
        //                //iStepNum++;

        //                //string visibilityArePolyFCName = null;
        //                ////ConvertToPolygon full visibility area
        //                //if (calcResults.HasFlag(VisibilityCalculationResultsEnum.VisibilityAreaPolygons)
        //                //    && !calcResults.HasFlag(VisibilityCalculationResultsEnum.ObservationObjects))
        //                //{
        //                //    visibilityArePolyFCName =
        //                //        VisibilityTask.GetResultName(pointId > -1 ?
        //                //        VisibilityCalculationResultsEnum.VisibilityAreaPolygonSingle :
        //                //        VisibilityCalculationResultsEnum.VisibilityAreaPolygons, outputSourceName, pointId);

        //                //    if (!CalculationLibrary.ConvertRasterToPolygon(outImageName, visibilityArePolyFCName, out messages))
        //                //    {
        //                //        if (!messages.Any(m => m.StartsWith("ERROR 010151"))) // Observatioj areas dont intersect Visibility aresa
        //                //        {
        //                //            string errorMessage = $"The result {visibilityArePolyFCName} was not generated";
        //                //            result.Exception = new MilSpaceVisibilityCalcFailedException(errorMessage);
        //                //            result.ErrorMessage = errorMessage;
        //                //            logger.ErrorEx("> ProcessObservationPoint ERROR ConvertRasterToPolygon. errorMessage:{0}", errorMessage);
        //                //            results.Add("Помилка: " + errorMessage + " ПС: " + pointId.ToString());
        //                //            return messages;
        //                //        }

        //                //        results.Add(iStepNum.ToString() + ". " + "Видимисть відсутня. Полігони не були конвертовані: " + visibilityArePolyFCName + " ПС: " + pointId.ToString());
        //                //        visibilityArePolyFCName = string.Empty;
        //                //    }
        //                //    else
        //                //    {
        //                //        results.Add(iStepNum.ToString() + ". " + "Конвертовано у полігони: " + visibilityArePolyFCName + " ПС: " + pointId.ToString());
        //                //    }
        //                //    iStepNum++;
        //                //}

        //                //Clip 

        //                var inClipName = outImageName;

        //                var resultLype = VisibilityCalculationResultsEnum.VisibilityObservStationClipSingle;
        //                var outClipName = VisibilityTask.GetResultName(resultLype, taskId, pointId);

        //                if (!CalculationLibrary.ClipVisibilityZonesByAreas(
        //                    inClipName,
        //                    outClipName,
        //                    observStationFeatureClass.AliasName,
        //                    messages))
        //                {
        //                    //TODO DS: Handle exceptions
        //                    //string errorMessage = $"The result {outClipName} was not generated";
        //                    //result.Exception = new MilSpaceVisibilityCalcFailedException(errorMessage);
        //                    //result.ErrorMessage = errorMessage;
        //                    //logger.ErrorEx("> ProcessObservationPoint ERROR ClipVisibilityZonesByAreas. errorMessage:{0}", errorMessage);
        //                    //results.Add("Помилка: " + errorMessage + " ПС: " + pointId.ToString());
        //                    //return messages;
        //                    return false;
        //                }
        //                else
        //                {
        //                    //results.Add(iStepNum.ToString() + ". " + "Зона видимості зведена до дійсного розміру: " + outClipName + " ПС: " + pointId.ToString());
        //                    //iStepNum++;

        //                    //Change to VisibilityAreaPolygonForObjects
        //                    var curCulcRResult = VisibilityCalculationResultsEnum.VisibilityAreaPolygonSingle;
        //                    visibilityArePolyFCName = VisibilityTask.GetResultName(curCulcRResult, taskId, pointId);

        //                    var rasterDataset = GdbAccess.Instance.GetDatasetFromCalcWorkspace(
        //                        outClipName, VisibilityCalculationResultsEnum.VisibilityAreaRaster);

        //                    bool isEmpty = EsriTools.IsRasterEmpty((IRasterDataset2)rasterDataset);

        //                    if (isEmpty)
        //                    {
        //                        //if (calcResults.HasFlag(curCulcRResult))
        //                        //{
        //                        //    calcResults &= ~curCulcRResult;
        //                        //}
        //                        //results.Add(iStepNum.ToString() + ". " + "Видимість відсутня. Полігони не було сформовано: " + visibilityArePolyFCName + " ПС: " + pointId.ToString());
        //                    }
        //                    else
        //                    {
        //                        if (!CalculationLibrary.ConvertRasterToPolygon(outClipName, visibilityArePolyFCName, out messages))
        //                        {
        //                            if (!messages.Any(m => m.StartsWith("ERROR 010151"))) // Observatioj areas dont intersect Visibility area
        //                            {
        //                                //string errorMessage = $"The result {visibilityArePolyFCName} was not generated";
        //                                //result.Exception = new MilSpaceVisibilityCalcFailedException(errorMessage);
        //                                //result.ErrorMessage = errorMessage;
        //                                //logger.ErrorEx("> ProcessObservationPoint ERROR ConvertRasterToPolygon. errorMessage:{0}", errorMessage);
        //                                //results.Add("Помилка: " + errorMessage + " ПС: " + pointId.ToString());
        //                                //return messages;
        //                            }
        //                            // results.Add(iStepNum.ToString() + ". " + "Видимість відсутня. Полігони не було сформовано: " + visibilityArePolyFCName + " ПС: " + pointId.ToString());
        //                        }
        //                        else
        //                        {
        //                            // results.Add(iStepNum.ToString() + ". " + "Конвертовано у полігони: " + visibilityArePolyFCName + " ПС: " + pointId.ToString());
        //                        }
        //                    }
        //                    //iStepNum++;
        //                }
        //                //else if (calcResults.HasFlag(VisibilityCalculationResultsEnum.VisibilityAreasTrimmedByPoly)
        //                //    && !string.IsNullOrEmpty(visibilityArePolyFCName))
        //                //{
        //                //    //Clip visibility images to valuable extent
        //                //    var inClipName = outImageName;
        //                //    var outClipName = VisibilityTask.GetResultName(pointId > -1 ?
        //                //      VisibilityCalculationResultsEnum.VisibilityAreaTrimmedByPolySingle :
        //                //      VisibilityCalculationResultsEnum.VisibilityAreasTrimmedByPoly, outputSourceName, pointId);

        //                //    if (!CalculationLibrary.ClipVisibilityZonesByAreas(
        //                //        inClipName,
        //                //        outClipName,
        //                //        visibilityArePolyFCName,
        //                //        messages,
        //                //        "NONE"))
        //                //    {
        //                //        string errorMessage = $"The result {outClipName} was not generated";
        //                //        result.Exception = new MilSpaceVisibilityCalcFailedException(errorMessage);
        //                //        result.ErrorMessage = errorMessage;
        //                //        logger.ErrorEx("> ProcessObservationPoint ERROR ClipVisibilityZonesByAreas. errorMessage:{0}", errorMessage);
        //                //        results.Add("Помилка: " + errorMessage + " ПС: " + pointId.ToString());
        //                //        return messages;
        //                //    }
        //                //    else
        //                //    {
        //                //        results.Add(iStepNum.ToString() + ". " + "Зона видимості зведена до дійсного розміру: " + outClipName + " ПС: " + pointId.ToString());
        //                //        iStepNum++;
        //                //        removeFullImageFromresult = true;
        //                //    }
        //                //}


        //                //var pointsCount = pointsFilteringIds.Where(id => id > -1).Count();
        //                //coverageTableManager.CalculateCoverageTableDataForPoint(
        //                //    (pointId == -1),
        //                //    visibilityArePolyFCName,
        //                //    pointsCount,
        //                //    curPoints.Value[0]);

        //                //results.Add(iStepNum.ToString() + ". " + "Сформовані записи таблиці покриття. для ПС: " + pointId.ToString());
        //                //iStepNum++;
        //            }

        //            var visibilityPolygonsForPointFeatureClass = GdbAccess.Instance.GetFeatureClass(MilSpaceConfiguration.ConnectionProperty.TemporaryGDBConnection, visibilityArePolyFCName);
        //            var visibilityArea = EsriTools.GetObjVisibilityArea(visibilityPolygonsForPointFeatureClass, observationStationPolygon);
        //            var observationStationPolygonArea = observationStationPolygon as IArea;

        //            var visibilityPercent = Math.Round(((visibilityArea * 100) / observationStationPolygonArea.Area), 0);
        //            visibilityPercents.Add(pointId, Convert.ToInt16(visibilityPercents));

        //           // feature = featureCursor.NextFeature();
        //        }
        //    }
        //    //TODO DS: Fill catch
        //    catch { }
        //    finally
        //    {
        //        //Marshal.ReleaseComObject(featureCursor);
        //    }

        //    //TODO DS: Check available visibility persents and generate table
        //    return CreateVOTable(observPointFeatureClass, visibilityPercents, expectedVisibilityPercent, taskId);
        //}

        public void FindBestOPParameters(string visibilityArePolyFCName,
                                               IFeatureClass observStationFeatureClass, int[] observStationsIds,
                                               int pointId)
        {
            var observationStationPolygon = observStationFeatureClass.GetFeature(observStationsIds.First()).ShapeCopy as IPolygon;

            var visibilityPolygonsForPointFeatureClass = GdbAccess.Instance.GetFeatureClass(MilSpaceConfiguration.ConnectionProperty.TemporaryGDBConnection, visibilityArePolyFCName);

            if(visibilityPolygonsForPointFeatureClass == null)
            {
                visibilityPercents.Add(pointId, 0);
                return;
            }

            //var visibilityPolygon = EsriTools.GetTotalPolygonFromFeatureClass(visibilityPolygonsForPointFeatureClass);
            //var intersection = EsriTools.GetIntersection(visibilityPolygon, observationStationPolygon);

            var visibilityArea = EsriTools.GetObjVisibilityArea(visibilityPolygonsForPointFeatureClass, observationStationPolygon);
            var observationStationPolygonArea = observationStationPolygon as IArea;

            var visibilityPercent = Math.Round(((visibilityArea * 100) / observationStationPolygonArea.Area), 0);
            visibilityPercents.Add(pointId, Convert.ToInt16(visibilityPercents));
        }


        public static List<int> GetAllIdsFromFeatureClass(IFeatureClass featureClass)
        {
            var ids = new List<int>();

            IQueryFilter queryFilter = new QueryFilter();
            queryFilter.WhereClause = $"{featureClass.OIDFieldName} >= 0";

            var idFieldIndex = featureClass.FindField(featureClass.OIDFieldName);

            IFeatureCursor featureCursor = featureClass.Search(queryFilter, true);
            IFeature feature = featureCursor.NextFeature();

            try
            {
                while (feature != null)
                {
                    ids.Add((int)feature.Value[idFieldIndex]);
                    feature = featureCursor.NextFeature();
                }

                return ids;
            }
            catch
            {
                return null;
            }
            finally
            {
                Marshal.ReleaseComObject(featureCursor);
            }
        }

        public bool CreateVOTable(IFeatureClass observPointFeatureClass, int expectedVisibilityPercent, string taskId)
        {
            var appropriateParamsIndexes = new List<int>();

            bool isParametersFound = false;
            KeyValuePair<int, short> bestParams = new KeyValuePair<int, short>(-1, 0);

            var fieldsClone = observPointFeatureClass.Fields as IClone;
            var fields = fieldsClone.Clone() as IFields;
            var shapeFieldIndex = fields.FindField(observPointFeatureClass.ShapeFieldName);

            var fieldsEdit = (IFieldsEdit)fields;
            fieldsEdit.DeleteField(fields.Field[shapeFieldIndex]);

            var bestParamsTableName = VisibilityTask.GetResultName(VisibilityCalculationResultsEnum.BestParametersTable, taskId);
            var bestParamsTable = GdbAccess.Instance.GenerateVOTable(fields, bestParamsTableName);


            foreach (var paramsVisibilityPercent in visibilityPercents)
            {
                if (paramsVisibilityPercent.Value >= expectedVisibilityPercent)
                {
                    appropriateParamsIndexes.Add(paramsVisibilityPercent.Key);
                    isParametersFound = true;
                }
                else
                {
                    if (bestParams.Value < paramsVisibilityPercent.Value)
                    {
                        bestParams = paramsVisibilityPercent;
                    }
                }
            }

            var bestParamsFeatures = new List<IFeature>();

            if (isParametersFound)
            {
                foreach (var index in appropriateParamsIndexes)
                {
                    bestParamsFeatures.Add(observPointFeatureClass.GetFeature(index));
                }
            }
            else
            {
                if (bestParams.Key > -1)
                {
                    bestParamsFeatures.Add(observPointFeatureClass.GetFeature(bestParams.Key));
                }
            }

            GdbAccess.Instance.FillBestParametersTable(bestParamsFeatures, bestParamsTable, bestParamsTableName);

            return isParametersFound;
        }
    }
}
