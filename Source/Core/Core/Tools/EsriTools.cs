﻿using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.SystemUI;
using MilSpace.Core.DataAccess;
using MilSpace.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;


namespace MilSpace.Core.Tools
{
    public static class EsriTools
    {
        private static Logger logger = Logger.GetLoggerEx("MilSpace.Core.Tools.EsriTools");
        private static ISpatialReference wgs84 = null;
        private static IRgbColor whiteColor = new RgbColor()
        {
            Green = 192,
            Blue = 192,
            Red = 192
        };

        private static Dictionary<esriGeometryType, Func<IRgbColor, ISymbol>> symbolsToFlash = new Dictionary<esriGeometryType, Func<IRgbColor, ISymbol>>()
        {
            { esriGeometryType.esriGeometryPoint, (rgbColor) => {
                  //Set point props to the flash geometry
                ISimpleMarkerSymbol simpleMarkerSymbol = new SimpleMarkerSymbol()
                {
                    Style = esriSimpleMarkerStyle.esriSMSCross,
                    Size = 8,
                    Color = rgbColor,
                    Outline = true,
                    OutlineColor = whiteColor,
                    OutlineSize = 2
                };

                return (ISymbol)simpleMarkerSymbol; }
            },
              { esriGeometryType.esriGeometryPolygon, (rgbColor) => {
                  //Set point props to the flash geometry

                ISimpleFillSymbol simpleFillSymbol = new SimpleFillSymbolClass();
                simpleFillSymbol.Color = rgbColor;

                simpleFillSymbol.Style = esriSimpleFillStyle.esriSFSForwardDiagonal;



                return (ISymbol)simpleFillSymbol; }
            },
            { esriGeometryType.esriGeometryPolyline, (rgbColor) => {
                //Define an arrow marker  
                IArrowMarkerSymbol arrowMarkerSymbol = new ArrowMarkerSymbol();

                arrowMarkerSymbol.Color = rgbColor;
                arrowMarkerSymbol.Size = 8;
                arrowMarkerSymbol.Length = 8;
                arrowMarkerSymbol.Width = 6;
                //Add an offset to make sure the square end of the line is hidden  
                arrowMarkerSymbol.XOffset = 0.8;

                //Create cartographic line symbol  
                ICartographicLineSymbol cartographicLineSymbol = new CartographicLineSymbol();
                cartographicLineSymbol.Color = rgbColor;
                cartographicLineSymbol.Width = 4.0;


                //Define simple line decoration  
                ISimpleLineDecorationElement simpleLineDecorationElement = new SimpleLineDecorationElement();
                //Place the arrow at the end of the line (the "To" point in the geometry below)  
                simpleLineDecorationElement.AddPosition(1);
                simpleLineDecorationElement.MarkerSymbol = arrowMarkerSymbol;

                //Define line decoration  
                ILineDecoration lineDecoration = new LineDecoration();
                lineDecoration.AddElement(simpleLineDecorationElement);

                //Set line properties  
                ILineProperties lineProperties = (ILineProperties)cartographicLineSymbol;
                lineProperties.LineDecoration = lineDecoration;

                return (ISymbol)cartographicLineSymbol;

            } }
        };

        private static readonly Dictionary<esriGeometryType, Func<IDisplay, IGeometry, bool>> actionToFlash = new Dictionary<esriGeometryType, Func<IDisplay, IGeometry, bool>>()
        {
            { esriGeometryType.esriGeometryPoint, (display, geometry) => {

                    //for(int i =0; i < 4; i++ )
                    //{
                        display.DrawPoint(geometry);
                    //}
                    return true;
                }
            },
            { esriGeometryType.esriGeometryPolyline, (display, geometry) => {

                for(int i=0; i < 4; i++ )
                    {
                        display.DrawPolyline(geometry);
                    }
                    return true;
                }
            },
            { esriGeometryType.esriGeometryPolygon, (display, geometry) => {

                for(int i=0; i < 4; i++ )
                    {
                        display.DrawPolygon(geometry);
                    }
                    return true;
                }
            }
        };

        public static void ProjectToWgs84(IGeometry geometry)
        {
            try
            {
                geometry.Project(Wgs84Spatialreference);
            }
            catch (Exception ex)
            {
                logger.WarnEx($"> ProjectToWgs84. Exception: {ex.Message}");
            }

        }

        /// <summary>
        /// Clone the point with projectin to Wgs84
        /// </summary>
        /// <param name="point">Clonning point</param>
        /// <returns>Point projected to Wgs84 SC</returns>
        public static IPoint CloneWithProjecting(this IPoint point)
        {
            var clonedPoint = new Point() { X = point.X, Y = point.Y, Z = point.Z, SpatialReference = point.SpatialReference };
            ProjectToWgs84(clonedPoint);

            return clonedPoint;
        }

        public static IPoint Clone(this IPoint point)
        {
            if (point != null)
            {
                return new Point() { X = point.X, Y = point.Y, Z = point.Z, SpatialReference = point.SpatialReference };
            }
            else
            {
                return null;
            }
        }

        public static void ProjectToMapSpatialReference(IGeometry geometry, ISpatialReference mapSpatialReference)
        {
            try
            {
                geometry.Project(mapSpatialReference);
            }
            catch (Exception ex)
            {
                logger.ErrorEx(ex.Message);
            }
        }
        public static IFeatureRenderer GetCalclResultRender(IFeatureLayer polygonLayer, string valuesField, IColor fromColor = null, IColor toColor = null)
        {
            var attrTable = polygonLayer.FeatureClass as ITable;
            var fld = attrTable.FindField(valuesField);

            var filter = new QueryFilter();
            filter.SubFields = valuesField;
            IQueryFilterDefinition2 filterDefinition = (IQueryFilterDefinition2)filter;

            filterDefinition.PostfixClause = $"ORDER BY {valuesField}";
            filterDefinition.PrefixClause = $"DISTINCT  {valuesField}";
            var uniwuevaluesr = attrTable.Search(filter, false);

            var row = uniwuevaluesr.NextRow();
            List<int> ids = new List<int>();

            while (row != null)
            {
                ids.Add((int)row.Value[fld]);
                row = uniwuevaluesr.NextRow();
            }

            //Create the start and end colors
            IColor startColor = fromColor != null ? fromColor : new RgbColor()
            {
                Transparency = 33,
                Red = 255,
                Green = 255,
                Blue = 115
            };
            IColor endColor = toColor != null ? toColor : new RgbColor()
            {
                Red = 115,
                Green = 38,
                Blue = 0
            };

            IAlgorithmicColorRamp algColorRamp = ids.Count > 1 ? new AlgorithmicColorRampClass() : null;
            bool ok = true;
            if (algColorRamp != null)
            {
                //Set the Start and End Colors
                algColorRamp.ToColor = endColor;
                algColorRamp.FromColor = startColor;

                //Set the ramping Alglorithm 
                algColorRamp.Algorithm = esriColorRampAlgorithm.esriCIELabAlgorithm;

                //Set the size of the ramp (the number of colors to be derived)
                algColorRamp.Size = ids.Count;

                //Create the ramp
                algColorRamp.CreateRamp(out ok);
            }

            if (ok)
            {
                IUniqueValueRenderer uniqueRen = new UniqueValueRenderer();
                uniqueRen.FieldCount = 1;
                uniqueRen.Field[0] = valuesField;


                var fldIndex = uniwuevaluesr.FindField(valuesField);
                if (fldIndex < 0)
                {
                    throw new KeyNotFoundException(valuesField);
                }

                int valueClass = 0;

                foreach (var uniqueValue in ids)
                {
                    var classValue = uniqueValue;
                    ISimpleFillSymbol simpleFillSymbol = new SimpleFillSymbol();

                    simpleFillSymbol.Color = algColorRamp == null ? startColor : algColorRamp.Color[valueClass++];

                    simpleFillSymbol.Outline = new CartographicLineSymbol
                    {
                        Width = 0.4,
                        Color = new RgbColor()
                        {
                            Red = 100,
                            Green = 100,
                            Blue = 100
                        }
                    };

                    uniqueRen.AddValue($"{classValue}", string.Empty, simpleFillSymbol as ISymbol);
                    uniqueRen.Label[$"{classValue}"] = $"{classValue}";
                    uniqueRen.Symbol[$"{classValue}"] = simpleFillSymbol as ISymbol;
                }
                IGeoFeatureLayer geoFeatureLayer = (IGeoFeatureLayer)polygonLayer;
                return (IFeatureRenderer)uniqueRen;
            }

            return null;
        }
        public static IRasterRenderer GetCalclResultRender(IRaster raster, string valuesField = "Value", RgbColor fromColor = null, RgbColor toColor = null)
        {
            var bandCollection = raster as IRasterBandCollection;
            var band = bandCollection.Item(0);
            bool hasTable;
            band.HasTable(out hasTable);
            if (hasTable)
            {
                var attrTable = band.AttributeTable;
                var filter = new QueryFilter();
                filter.SubFields = valuesField;
                IQueryFilterDefinition2 filterDefinition = (IQueryFilterDefinition2)filter;

                filterDefinition.PrefixClause = $"DISTINCT {valuesField}";
                int cntRows = attrTable.RowCount(filter);
                filterDefinition.PostfixClause = $"ORDER BY {valuesField}";
                var uniwuevaluesr = attrTable.Search(filter, false);

                bool bOK = true;
                IAlgorithmicColorRamp ramp = new AlgorithmicColorRamp();

                if (fromColor == null)
                {
                    fromColor = new RgbColor()
                    {
                        Red = 255,
                        Green = 255,
                        Blue = 115
                    };
                }
                if (toColor == null)
                {
                    toColor = new RgbColor()
                    {
                        Red = 115,
                        Green = 38,
                        Blue = 0
                    };
                }

                if (cntRows > 1)
                {
                    ramp.FromColor = fromColor;
                    ramp.ToColor = toColor;
                    ramp.Size = cntRows;
                    ramp.Algorithm = esriColorRampAlgorithm.esriCIELabAlgorithm;
                    ramp.CreateRamp(out bOK);
                }
                else
                {
                    ramp = null;
                }

                //rasterLayer.Renderer = (IRasterRenderer)stretchRen;
                IRasterUniqueValueRenderer uniqueRen = new RasterUniqueValueRenderer();
                uniqueRen.HeadingCount = 1;
                uniqueRen.Heading[0] = "All Data Values";
                uniqueRen.ClassCount[0] = cntRows;
                uniqueRen.Field = valuesField;

                var row = uniwuevaluesr.NextRow();
                var fldIndex = uniwuevaluesr.FindField(valuesField);
                if (fldIndex < 0)
                {
                    throw new KeyNotFoundException(valuesField);
                }

                int valueClass = 0;
                while (row != null)
                {
                    var classValue = row.Value[fldIndex];
                    uniqueRen.AddValue(0, valueClass, classValue);
                    uniqueRen.Label[0, valueClass] = $"{classValue}";
                    var fillSymbol = new SimpleFillSymbol();
                    fillSymbol.Color = ramp == null ? fromColor : ramp.Color[valueClass];
                    uniqueRen.Symbol[0, valueClass++] = fillSymbol as ISymbol;
                    row = uniwuevaluesr.NextRow();
                }
                return (IRasterRenderer)uniqueRen;
            }

            return null;
        }

        public static ISpatialReference Wgs84Spatialreference
        {
            get
            {
                if (wgs84 == null)
                {
                    SpatialReferenceEnvironmentClass factory = new SpatialReferenceEnvironmentClass();
                    wgs84 = factory.CreateGeographicCoordinateSystem((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);
                    Marshal.ReleaseComObject(factory);
                }

                return wgs84;
            }
        }

        public static void PanToGeometry(IActiveView view, IGeometry geometry, bool setCenterAt = false)
        {
            IEnvelope env = view.Extent;

            IRelationalOperator operation = env as IRelationalOperator;
            logger.InfoEx($"PanToGeometry. Projecting to {view.FocusMap.SpatialReference.Name}");
            geometry.Project(view.FocusMap.SpatialReference);

            if (setCenterAt || !operation.Contains(geometry))
            {
                ISegmentCollection poly = new PolygonClass();
                IArea area = geometry.Envelope as IArea;
                env.CenterAt(area.Centroid);
                view.Extent = env;
                view.Refresh();
                view.ScreenDisplay.UpdateWindow();
            }
        }

        public static void ZoomToGeometry(IActiveView view, IGeometry geometry, bool chcekScale = false)
        {
            var transformation = view.ScreenDisplay.DisplayTransformation as IDisplayTransformationScales;
            var scale = transformation.CalculateScale(geometry.Envelope);
            if (chcekScale)
            {

                var currentScale = transformation.CalculateScale(view.Extent);

                if (scale > currentScale)
                {
                    transformation.ZoomTo(geometry.Envelope, scale);
                    view.Refresh();
                    view.ScreenDisplay.UpdateWindow();
                }
                else
                {
                    PanToGeometry(view, geometry);
                }
            }
            else
            {
                transformation.ZoomTo(geometry.Envelope, scale);
                view.Refresh();
                view.ScreenDisplay.UpdateWindow();
                PanToGeometry(view, geometry);
            }
        }

        public static void FlashGeometry(IScreenDisplay display, IEnumerable<IGeometry> geometries)
        {
            IRgbColor color = new RgbColor();
            color.Green = color.Blue = 0;
            color.Red = 255;

            short cacheId = display.AddCache();
            logger.InfoEx("FlashGeometry. Statring drawing..");
            display.StartDrawing(display.hDC, cacheId);

            geometries.ToList().ForEach(geometry =>
            {
                if (symbolsToFlash.ContainsKey(geometry.GeometryType))
                {
                    var symbol = symbolsToFlash[geometry.GeometryType].Invoke(color);
                    display.SetSymbol(symbol);
                    actionToFlash[geometry.GeometryType].Invoke(display, geometry);
                }
                else
                { throw new KeyNotFoundException("{0} cannot be found in the Symbol dictionary".InvariantFormat(geometry.GeometryType)); }
            });
            logger.InfoEx("FlashGeometry. Finishing drawing..");
            display.FinishDrawing();

            tagRECT rect = new tagRECT();
            display.DrawCache(display.hDC, cacheId, ref rect, ref rect);
            System.Threading.Thread.Sleep(300);
            display.Invalidate(rect: null, erase: true, cacheIndex: cacheId);
            display.RemoveCache(cacheId);

            logger.InfoEx("FlashGeometry. Geometries flashed.");
        }

        public static IEnumerable<IPolyline> CreatePolylineFromPointsArray(IPoint[] points, ISpatialReference spatialReference)
        {
            IGeometryBridge2 pGeoBrg = new GeometryEnvironment() as IGeometryBridge2;

            IPointCollection4 pPointColl = new PolylineClass();

            //foreach(var point in points)
            //{
            //    pPointColl.AddPoint(point);
            //}

            pGeoBrg.SetPoints(pPointColl, ref points);
            var polyline = pPointColl as IPolyline;
            polyline.SpatialReference = points[0].SpatialReference;
            polyline.Project(spatialReference);

            return new List<IPolyline> { polyline };
        }

        public static IEnumerable<IPolyline> CreateDefaultPolylinesForFun(IPoint centerPoint, IPoint[] points, IEnumerable<IGeometry> geometries, bool circle, bool isPointInside, double length,
                                                                   out double minAzimuth, out double maxAzimuth, out double maxLength, double centerAzimuth = -1)
        {
            double minAngle = 360;
            maxAzimuth = 0;
            minAzimuth = 360;
            maxLength = length;
            bool isCircle = true;
            double betweenAzimuth = 0;

            var linesAzimuths = new List<double>();

            for (int i = 0; i < points.Length; i++)
            {
                var line = new Line() { FromPoint = centerPoint, ToPoint = points[i], SpatialReference = centerPoint.SpatialReference };

                if (length == -1 && maxLength < line.Length)
                {
                    maxLength = line.Length;
                }

                linesAzimuths.Add(line.PosAzimuth());
            }

            var count = linesAzimuths.Count;

            for (int i = 0; i < count - 1; i++)
            {
                if (linesAzimuths.Count(az => az == linesAzimuths[i]) > 1)
                {
                    linesAzimuths.Remove(linesAzimuths[i]);
                    count--;
                }
            }


            if (geometries.Count() == 1 && points.Length == 2)
            {
                isCircle = false;

                if (linesAzimuths[0] < linesAzimuths[1])
                {
                    minAzimuth = linesAzimuths[0];
                    maxAzimuth = linesAzimuths[1];
                }
                else
                {
                    minAzimuth = linesAzimuths[1];
                    maxAzimuth = linesAzimuths[0];
                }

                if (centerAzimuth == -1)
                {
                    var angle = FindAngleBetweenAzimuths(maxAzimuth, minAzimuth, true);
                    betweenAzimuth = GetBetweenAzimuth(maxAzimuth, minAzimuth, angle / 2, true);
                }
            }

            if (!(geometries.Count() == 1 && points.Length < 3))
            {
                for (int i = 0; i < linesAzimuths.Count - 1; i++)
                {
                    for (int j = i + 1; j < linesAzimuths.Count; j++)
                    {
                        double maxAz;
                        double minAz;

                        if (linesAzimuths[i] < linesAzimuths[j])
                        {
                            minAz = linesAzimuths[i];
                            maxAz = linesAzimuths[j];
                        }
                        else
                        {
                            minAz = linesAzimuths[j];
                            maxAz = linesAzimuths[i];
                        }

                        var inAngle = FindAngleBetweenAzimuths(maxAz, minAz, true);
                        var inBetweenAzimuth = GetBetweenAzimuth(maxAz, minAz, inAngle / 2, true);

                        var outAngle = FindAngleBetweenAzimuths(maxAz, minAz, false);
                        var outBetweenAzimuth = GetBetweenAzimuth(maxAz, minAz, outAngle / 2, false);

                        var isInIntersect = IsAzimuthIntersectGeometry(geometries, centerPoint, maxLength, inBetweenAzimuth);
                        var isOutIntersect = IsAzimuthIntersectGeometry(geometries, centerPoint, maxLength, outBetweenAzimuth);

                        if (isPointInside && (isInIntersect && isOutIntersect))
                        {
                            continue;
                        }
                        var isAzimuthsBetweenExists = linesAzimuths.Any(azimuth => azimuth > minAz && azimuth < maxAz);

                        if (!isAzimuthsBetweenExists)
                        {
                            if (!isInIntersect)
                            {
                                if (minAngle > outAngle)
                                {
                                    maxAzimuth = maxAz;
                                    minAzimuth = minAz;

                                    minAngle = outAngle;

                                    if (centerAzimuth == -1)
                                    {
                                        betweenAzimuth = outBetweenAzimuth;
                                    }

                                    isCircle = false;
                                }
                            }
                        }

                        var isAzimuthsOutExists = linesAzimuths.Any(azimuth => azimuth < minAz || azimuth > maxAz);

                        if (!isAzimuthsOutExists)
                        {
                            if (!isOutIntersect)
                            {
                                if (minAngle > inAngle)
                                {
                                    maxAzimuth = maxAz;
                                    minAzimuth = minAz;

                                    minAngle = inAngle;

                                    if (centerAzimuth == -1)
                                    {
                                        betweenAzimuth = inBetweenAzimuth;
                                    }

                                    isCircle = false;
                                }
                            }
                        }
                    }
                }
            }

            if (isCircle)
            {
                if (minAzimuth > maxAzimuth)
                {
                    var buff = maxAzimuth;
                    maxAzimuth = minAzimuth;
                    minAzimuth = buff;
                }

                return CreatePolylinesFromPointAndAzimuths(centerPoint, maxLength, 6, 0, 360);
            }

            if (centerAzimuth != -1)
            {
                betweenAzimuth = centerAzimuth;
            }

            var polylines = new IPolyline[3];

            var startPoint = GetPointByAzimuthAndLength(centerPoint, minAzimuth, maxLength);
            var middlePoint = GetPointByAzimuthAndLength(centerPoint, betweenAzimuth, maxLength);
            var endPoint = GetPointByAzimuthAndLength(centerPoint, maxAzimuth, maxLength);

            polylines[0] = CreatePolylineFromPoints(centerPoint, startPoint);
            polylines[1] = CreatePolylineFromPoints(centerPoint, middlePoint);
            polylines[2] = CreatePolylineFromPoints(centerPoint, endPoint);

            if (betweenAzimuth > maxAzimuth || betweenAzimuth < minAzimuth)
            {
                var buff = maxAzimuth;
                maxAzimuth = minAzimuth;
                minAzimuth = buff;
            }

            return polylines;
        }

        public static IPolyline GetToGeometryCenterPolyline(IPoint fromPoint, IGeometry geometry)
        {
            var polylines = new List<IPolyline>();
            IPoint toPoint;

            if (geometry.GeometryType == esriGeometryType.esriGeometryPoint)
            {
                toPoint = geometry as IPoint;
            }
            else
            {
                toPoint = GetCenterPoint(geometry);
            }

            var polyline = CreatePolylineFromPoints(fromPoint, toPoint);

            return polyline;
        }

        public static IEnvelope GetEnvelopeOfGeometriesList(IEnumerable<IGeometry> geometries)
        {
            IGeometryCollection bag = new GeometryBagClass() as IGeometryCollection;
            var spatialRef = geometries.First().SpatialReference;

            foreach (var geometry in geometries)
            {
                bag.AddGeometry(geometry);
            }

            var bagClass = bag as IGeometryBag;
            bagClass.SpatialReference = spatialRef;

            return bagClass.Envelope;
        }

        private static double FindAngleBetweenAzimuths(double maxAzimuth, double minAzimuth, bool between)
        {
            if (!between)
            {
                return (360 - maxAzimuth) + minAzimuth;
            }
            else
            {
                return maxAzimuth - minAzimuth;
            }
        }

        private static double GetBetweenAzimuth(double maxAzimuth, double minAzimuth, double angle, bool between)
        {
            if (between)
            {
                return minAzimuth + angle;
            }
            else
            {
                var result = maxAzimuth + angle;
                if (result > 360)
                {
                    result -= 360;
                }

                return result;
            }
        }

        public static IEnumerable<IPolyline> CreatePolylinesFromPointAndAzimuths(
            IPoint centerPoint, double length, int count, double azimuth1, double azimuth2)
        {
            if (centerPoint == null)
            {
                return null;
            }
            if (count < 2)
            {
                //TODO: Localize error message
                throw new MilSpaceProfileLackOfParameterException("Line numbers", count);
            }

            double sector;
            int devider = count;
            //Check if it is a circle
            if ((azimuth1 == 0 && azimuth2 == 360) || (azimuth2 == 0 && azimuth1 == 360) || (azimuth2 == azimuth1))
            {
                if (count == 2)
                {
                    azimuth2 = azimuth1 + 180;
                }
                else
                {
                    devider += 1;
                }
            }

            if (azimuth1 > azimuth2) //clockwise
            {
                sector = (360 - azimuth1) + azimuth2;
            }
            else
            {
                sector = azimuth2 - azimuth1;
            }

            if (sector == 0)
            {
                sector = 360;
            }

            double step = sector / (devider - 1);
            List<IPolyline> result = new List<IPolyline>();
            for (int i = 0; i < count; i++)
            {
                double radian = (90 - (azimuth1 + (i * step))) * (Math.PI / 180);
                IPoint outPoint = GetPointFromAngelAndDistance(centerPoint, radian, length);
                result.Add(CreatePolylineFromPoints(centerPoint, outPoint as IPoint));
            }

            return result;
        }

        public static IEnumerable<IPolyline> CreateToVerticesPolylinesForFun(IPoint[] points, IPoint centerPoint, double length,
                                                                                out double minAzimuth, out double maxAzimuth, out double maxLength)
        {
            var polylines = new List<IPolyline>();

            maxLength = length;
            maxAzimuth = 0;
            minAzimuth = 360;

            foreach (var point in points)
            {
                var line = new Line()
                {
                    FromPoint = centerPoint,
                    ToPoint = point,
                    SpatialReference = centerPoint.SpatialReference
                };
                var azimuth = line.PosAzimuth();
                if (azimuth > maxAzimuth)
                {
                    maxAzimuth = azimuth;
                }

                if (azimuth < minAzimuth)
                {
                    minAzimuth = azimuth;
                }

                polylines.Add(CreatePolylineFromPoints(centerPoint, point));
            }

            return polylines;
        }

        /// <summary>
        /// Get new point by distance and angel
        /// </summary>
        /// <param name="basePoint">The base point</param>
        /// <param name="angel">dirrection in radians</param>
        /// <param name="length">distance to the new point</param>
        /// <returns>Esri point</returns>
        public static IPoint GetPointFromAngelAndDistance(IPoint basePoint, double angel, double length)
        {
            IConstructPoint outPoint = new PointClass();
            outPoint.ConstructAngleDistance(basePoint, angel, length);
            IPoint resilt = outPoint as IPoint;
            resilt.SpatialReference = basePoint.SpatialReference;
            return resilt;
        }


        /// <summary>
        /// Create a Polyline from two Points
        /// </summary>
        /// <param name="pointFrom">Start point</param>
        /// <param name="pointTo">End point</param>
        /// <returns>Esri poliline</returns>
        public static IPolyline CreatePolylineFromPoints(IPoint pointFrom, IPoint pointTo)
        {
            if (pointFrom == null || pointTo == null)
            {
                return null;
            }

            WKSPoint[] segmentWksPoints = new WKSPoint[2];
            segmentWksPoints[0].X = pointFrom.X;
            segmentWksPoints[0].Y = pointFrom.Y;
            segmentWksPoints[1].X = pointTo.X;
            segmentWksPoints[1].Y = pointTo.Y;

            IPointCollection4 trackLine = new PolylineClass();

            IGeometryBridge2 m_geometryBridge = new GeometryEnvironmentClass();
            m_geometryBridge.AddWKSPoints(trackLine, ref segmentWksPoints);


            var result = trackLine as IPolyline;

            if (pointFrom.SpatialReference != null && pointTo.SpatialReference != null && pointFrom.SpatialReference == pointTo.SpatialReference)
            {
                result.SpatialReference = pointFrom.SpatialReference;
            }

            return result;
        }

        public static IPolyline Create3DPolylineFromPoints(IPointCollection points)
        {
            object missing = Type.Missing;

            IGeometryCollection geometryCollection = new PolylineClass();
            IPointCollection pointCollection = new PathClass();

            pointCollection.AddPointCollection(points);
            geometryCollection.AddGeometry(pointCollection as IGeometry, ref missing, ref missing);

            var geometry = geometryCollection as IGeometry;
            IZAware zAware = geometry as IZAware;

            zAware.ZAware = true;

            return geometryCollection as IPolyline;
        }

        public static IPolyline Create3DPolylineFromPoints(IPoint pointLeft, IPoint pointRight)
        {
            object missing = Type.Missing;

            IGeometryCollection geometryCollection = new PolylineClass();
            IPointCollection pointCollection = new PathClass();

            pointCollection.AddPoint(pointLeft);
            pointCollection.AddPoint(pointRight);

            geometryCollection.AddGeometry(pointCollection as IGeometry, ref missing, ref missing);

            var geometry = geometryCollection as IGeometry;
            IZAware zAware = geometry as IZAware;

            zAware.ZAware = true;

            return geometryCollection as IPolyline;
        }

        public static IPoint GetObserverPoint(IPoint firstPoint, double observerHeight, ISpatialReference spatialReference)
        {
            ProjectToMapSpatialReference(firstPoint, spatialReference);
            var point = new Point()
            {
                X = firstPoint.X,
                Y = firstPoint.Y,
                Z = firstPoint.Z + observerHeight,
                SpatialReference = spatialReference
            } as IPoint;

            var geometry = point as IGeometry;
            IZAware zAware = geometry as IZAware;
            zAware.ZAware = true;

            return point;
        }

        public static IPolygon GetPolygonZByPointCollection(IPointCollection points)
        {
            IGeometryBridge2 geometryBridge2 = new GeometryEnvironmentClass();
            IPointCollection4 pointCollection4 = new PolygonClass();

            WKSPointZ[] aWKSPoints = new WKSPointZ[points.PointCount];

            for (int i = 0; i < aWKSPoints.Length; i++)
            {
                aWKSPoints[i] = points.Point[i].WKSPointZ();
            }

            geometryBridge2.SetWKSPointZs(pointCollection4, ref aWKSPoints);

            var geometry = pointCollection4 as IGeometry;
            IZAware zAware = geometry as IZAware;

            zAware.ZAware = true;

            var result = pointCollection4 as IPolygon;
            result.SpatialReference = points.Point[0].SpatialReference;

            return result;
        }

        public static IPolygon GetPolygonByPointCollection(IPointCollection points)
        {
            IGeometryBridge2 geometryBridge2 = new GeometryEnvironmentClass();
            IPointCollection4 pointCollection4 = new PolygonClass();

            WKSPoint[] aWKSPoints = new WKSPoint[points.PointCount];

            for (int i = 0; i < aWKSPoints.Length; i++)
            {
                aWKSPoints[i].X = points.Point[i].X;
                aWKSPoints[i].Y = points.Point[i].Y;
            }

            geometryBridge2.SetWKSPoints(pointCollection4, ref aWKSPoints);

            var geometry = pointCollection4 as IGeometry;
            IZAware zAware = geometry as IZAware;

            zAware.ZAware = false;

            var result = pointCollection4 as IPolygon;
            result.SpatialReference = points.Point[0].SpatialReference;

            return result;
        }

        public static IPolygon GetVisilityPolygon(List<IPolyline> polylines)
        {
            IGeometryCollection geometryCollection = new PolygonClass();
            ISegmentCollection ringSegColl1 = new RingClass();

            foreach (var polyline in polylines)
            {
                ILine line = new LineClass() { FromPoint = polyline.FromPoint, ToPoint = polyline.ToPoint, SpatialReference = polyline.SpatialReference };
                var polylineSeg = (ISegment)line;
                ringSegColl1.AddSegment(polylineSeg);
            }

            var ringGeometry = ringSegColl1 as IGeometry;
            IZAware zAwareRing = ringGeometry as IZAware;

            zAwareRing.ZAware = true;

            IRing ring1 = ringSegColl1 as IRing;
            ring1.Close();

            IGeometryCollection polygon = new PolygonClass();
            polygon.AddGeometry(ring1 as IGeometry);

            var geometry = polygon as IGeometry;
            IZAware zAware = geometry as IZAware;

            zAware.ZAware = true;

            var result = polygon as IPolygon;
            result.SpatialReference = polylines[0].SpatialReference;

            return result;
        }

        public static IPoint GetCenterPoint(IGeometry geometry)
        {
            var envelope = geometry.Envelope;
            var x = envelope.XMin + (envelope.XMax - envelope.XMin) / 2;
            var y = envelope.YMin + (envelope.YMax - envelope.YMin) / 2;

            return new Point { X = x, Y = y, SpatialReference = envelope.SpatialReference };
        }

        public static ILayer GetLayer(string layerName, IMap map)
        {
            var layers = map.Layers;
            var layer = layers.Next();

            while (layer != null && layer.Name != layerName)
            {
                layer = layers.Next() as ILayer;
            }

            return layer;
        }

        public static void ZoomToLayer(string layerName, IActiveView activeView)
        {
            var layer = GetLayer(layerName, activeView.FocusMap);
            var envelope = layer.AreaOfInterest;

            if (envelope != null)
            {
                activeView.Extent = layer.AreaOfInterest;
                activeView.Refresh();
            }
        }

        public static IEnumerable<ILayer> GetVisibiltyImgLayers(string layerName, IMap map)
        {
            var imgLayers = new List<ILayer>();

            for (int i = 0; i < map.LayerCount; i++)
            {
                if (!(map.Layer[i] is IGroupLayer) && map.Layer[i].Name.Contains(layerName))
                {
                    imgLayers.Add(map.Layer[i]);
                }
            }

            return imgLayers;
        }

        public static IEnumerable<int> GetSelectionByExtent(IFeatureClass featureClass, IActiveView activeView)
        {
            if (featureClass == null)
            {
                throw new NullReferenceException("Feature class cannot be null");
            }
            if (activeView == null)
            {
                throw new NullReferenceException("Active View cannot be null");
            }

            var curExtent = activeView.Extent;
            ISpatialFilter spatialFilter = new SpatialFilterClass();
            spatialFilter.Geometry = activeView.Extent;
            spatialFilter.GeometryField = featureClass.ShapeFieldName;
            spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;


            // Execute the query and iterate through the cursor's results.
            IFeatureCursor cursor = featureClass.Search(spatialFilter, false);
            IFeature observPoint = null;
            var results = new List<int>();
            while ((observPoint = cursor.NextFeature()) != null)
            {
                results.Add(Convert.ToInt32(observPoint.get_Value(0)));
            }

            // Discard the cursors as they are no longer needed.
            Marshal.ReleaseComObject(cursor);

            return results;
        }

        public static List<IPolyline> GetIntersections(IPolyline selectedLine, ILayer layer)
        {
            if (layer != null && selectedLine != null)
            {
                return GetIntersection(selectedLine, layer);
            }

            return null;
        }

        public static bool RemoveDataSet(string gdb, string name)
        {
            IWorkspaceFactory workspaceFactory = new FileGDBWorkspaceFactory();
            IWorkspace workspace = workspaceFactory.OpenFromFile(gdb, 0);
            IFeatureWorkspaceManage wspManage = (IFeatureWorkspaceManage)workspace;

            var result = false;
            var datasets = workspace.Datasets[esriDatasetType.esriDTAny];
            var currentDataset = datasets.Next();

            while (currentDataset != null && !currentDataset.Name.EndsWith(name))
            {
                currentDataset = datasets.Next();
            }

            if (currentDataset != null)
            {
                if (wspManage.CanDelete(currentDataset.FullName))
                {
                    try
                    {
                        currentDataset.Delete();
                        result = true;
                    }
                    catch (Exception ex)
                    {
                        logger.ErrorEx(ex.Message);
                    }
                }
            }
            else
            {
                result = true;
                logger.ErrorEx($"Dataset {name} doesn`t exist");
            }

            Marshal.ReleaseComObject(workspaceFactory);
            return result;
        }

        public static void RemoveLayer(string layerName, IMap map)
        {
            var layer = GetLayer(layerName, map);

            if (layer == null)
            {
                return;
            }

            var mapLayers = map as IMapLayers2;
            mapLayers.DeleteLayer(layer);
        }

        public static void AddTableToMap(ITableProperties tblProperties, string tableName, string gdb, IMxDocument mapDocument, IMxApplication application)
        {
            bool isTableExist = false;
            var enumProperties = tblProperties.IEnumTableProperties;
            enumProperties.Reset();

            ITableProperty3 tlbProperty3 = enumProperties.Next() as ITableProperty3;
            while (tlbProperty3 != null)
            {
                if (tlbProperty3.StandaloneTable != null)
                {
                    if (tlbProperty3.StandaloneTable.Name.EndsWith(tableName))
                    {
                        isTableExist = true;
                        break;
                    }
                }
                tlbProperty3 = enumProperties.Next() as ITableProperty3;
            }

            if (!isTableExist)
            {
                IWorkspaceFactory workspaceFactory = new FileGDBWorkspaceFactory();
                IWorkspace workspace = workspaceFactory.OpenFromFile(gdb, 0);
                IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspace;
                IWorkspace2 wsp2 = (IWorkspace2)workspace;

                if (wsp2.NameExists[esriDatasetType.esriDTTable, tableName])
                {
                    ITable table = featureWorkspace.OpenTable(tableName);
                    IStandaloneTable stndaloneTable = new StandaloneTable();
                    stndaloneTable.Table = table;
                    stndaloneTable.Name = tableName;

                    IStandaloneTableCollection tableCollection = mapDocument.FocusMap as IStandaloneTableCollection;
                    tableCollection.AddStandaloneTable(stndaloneTable);
                    mapDocument.UpdateContents();

                    ITableWindow tabwindow = new TableWindow();
                    tabwindow.Application = application;
                    tabwindow.Table = table;
                    tabwindow.Show(true);
                }
            }
        }

        public static void AddVisibilityGroupLayer(
            IEnumerable<IDataset> visibilityLayersNames,
            string sessionName,
            string calcRasterName,
            string gdb,
            string relativeLayerName,
            bool isLayerAbove,
            short transparency,
            IActiveView activeView)
        {
            var visibilityLayers = new List<ILayer>();
            foreach (var layerName in visibilityLayersNames)
            {
                if (layerName is IRasterDataset raster)
                {
                    visibilityLayers.Add(GetRasterLayer(raster));
                }
                if (layerName is IFeatureClass feature)
                {
                    var lr = GetFeatureLayer(feature);

                    visibilityLayers.Add(lr);
                }
            }

            MapLayersManager layersManager = new MapLayersManager(activeView);

            var relativeLayer =
                layersManager.FirstLevelLayers.FirstOrDefault(l => l.Name.Equals(relativeLayerName, StringComparison.InvariantCultureIgnoreCase));

            //var relativeLayer = GetLayer(relativeLayerName, activeView.FocusMap);
            var calcRasters = GetVisibiltyImgLayers(calcRasterName, activeView.FocusMap);

            IGroupLayer groupLayer = new GroupLayerClass
            { Name = sessionName };

            var layersToremove = new List<IRasterLayer>();
            foreach (var layer in visibilityLayers)
            {
                if (layer is IRasterLayer raster)
                {
                    var layerEffects = (ILayerEffects)layer;
                    layerEffects.Transparency = transparency;

                    var existenLayer =
                        layersManager.RasterLayers.FirstOrDefault(l => l.FilePath.Equals(raster.FilePath, StringComparison.InvariantCultureIgnoreCase));
                    if (existenLayer != null && !layersToremove.Any(l => l.Equals(existenLayer)))
                    {
                        layersToremove.Add(existenLayer);
                    }
                }
                groupLayer.Add(layer);
            }

            var mapLayers = activeView.FocusMap as IMapLayers2;
            int relativeLayerPosition = GetLayerIndex(relativeLayer, activeView);
            int groupLayerPosition = (isLayerAbove) ? relativeLayerPosition - 1 : relativeLayerPosition + 1;

            layersToremove.ForEach(l => mapLayers.DeleteLayer(l));
            mapLayers.InsertLayer(groupLayer, false, groupLayerPosition);
        }

        public static int GetLayerIndex(ILayer layer, IActiveView activeView)
        {
            for (int index = 0; index < activeView.FocusMap.LayerCount; index++)
            {
                ILayer layerAtIndex = activeView.FocusMap.get_Layer(index);
                if (layerAtIndex == layer)
                    return index;
            }
            return -1;
        }
        public static void IdentifyPixelValue(IRaster raster, double xMap, double yMap)
        {
        }

        public static IGeometry GetIntersection(IPolygon coverageArea, IGeometry observObject)
        {
            if (observObject.GeometryType != esriGeometryType.esriGeometryPoint && observObject.GeometryType != esriGeometryType.esriGeometryMultipoint
                    && observObject.GeometryType != esriGeometryType.esriGeometryPolyline && observObject.GeometryType != esriGeometryType.esriGeometryPolygon)
            {
                throw new ArgumentException("Intersection method must be applied on high-level geometries only");
            }

            ITopologicalOperator pTopo = coverageArea as ITopologicalOperator;
            var result = pTopo.Intersect(observObject, esriGeometryDimension.esriGeometryNoDimension);

            return result;
        }

        private static List<IPolyline> GetIntersection(IPolyline polyline, ILayer layer)
        {
            var resultPolylines = new List<IPolyline>();
            var layerWehereDef = (layer as IFeatureLayerDefinition).DefinitionExpression;
            ISpatialFilter spatialFilter = new SpatialFilter
            {
                Geometry = polyline,
                SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects,
                WhereClause = layerWehereDef
            };
            var featureClass = (layer as IFeatureLayer).FeatureClass;
            var highwayCursor = featureClass.Search(spatialFilter, false);
            var feature = highwayCursor.NextFeature();

            while (feature != null)
            {
                resultPolylines.AddRange(GetFeatureIntersection(feature, polyline));
                feature = highwayCursor.NextFeature();
            }
            Marshal.ReleaseComObject(highwayCursor);

            return resultPolylines;
        }

        private static bool IsAzimuthIntersectGeometry(IEnumerable<IGeometry> geometries, IPoint centerPoint, double length, double azimuth)
        {
            IPoint point = GetPointByAzimuthAndLength(centerPoint, azimuth, length);
            IPolyline polyline = CreatePolylineFromPoints(centerPoint, point);
            polyline.Project(geometries.First().SpatialReference);

            foreach (var geometry in geometries)
            {
                ITopologicalOperator pTopo = geometry as ITopologicalOperator;

                var result = pTopo.Intersect(polyline, esriGeometryDimension.esriGeometry0Dimension);
                if ((!result.IsEmpty))
                {
                    return true;
                }
            }
            return false;
        }

        private static List<IPolyline> GetFeatureIntersection(IFeature feature, IPolyline polyline)
        {
            var resultPolylines = new List<IPolyline>();
            var multipoint = new Multipoint();

            IGeometry geometry = feature.ShapeCopy;
            geometry.Project(polyline.SpatialReference);

            ITopologicalOperator pTopo = geometry as ITopologicalOperator;
            var result = pTopo.Intersect(polyline, esriGeometryDimension.esriGeometry0Dimension);
            var firstLinePointOnLayer = (IPoint)pTopo.Intersect(polyline.FromPoint, esriGeometryDimension.esriGeometry0Dimension);
            var lastLinePointOnLayer = (IPoint)pTopo.Intersect(polyline.ToPoint, esriGeometryDimension.esriGeometry0Dimension);

            if (!result.IsEmpty)
            {
                multipoint = (Multipoint)result;
                IPoint firstPoint = null;
                IPoint lastPoint = null;

                if (!firstLinePointOnLayer.IsEmpty)
                {
                    if (firstLinePointOnLayer.Y > multipoint.Point[0].Y) { firstPoint = firstLinePointOnLayer; }
                    else { lastPoint = firstLinePointOnLayer; }
                }

                if (!lastLinePointOnLayer.IsEmpty)
                {
                    if (lastLinePointOnLayer.Y > multipoint.Point[0].Y) { firstPoint = lastLinePointOnLayer; }
                    else { lastPoint = lastLinePointOnLayer; }
                }

                if (firstPoint != null)
                {
                    var buff = new Multipoint();
                    buff.AddPointCollection(multipoint);

                    multipoint.RemovePoints(0, multipoint.PointCount);
                    multipoint.AddPoint(firstPoint);
                    multipoint.AddPointCollection(buff);
                }

                if (lastPoint != null) { multipoint.AddPoint(lastPoint); }
            }

            if (result.IsEmpty && !firstLinePointOnLayer.IsEmpty)
            {
                if (!firstLinePointOnLayer.IsEmpty) { multipoint.AddPoint((IPoint)firstLinePointOnLayer); }
                if (!lastLinePointOnLayer.IsEmpty) { multipoint.AddPoint((IPoint)lastLinePointOnLayer); }
            }

            if (multipoint.PointCount == 1)
            {
                multipoint.Point[0].Project(polyline.SpatialReference);
                resultPolylines.Add(CreatePolylineFromPoints(multipoint.Point[0], multipoint.Point[0]));
            }
            else if (multipoint.PointCount > 0)
            {
                for (int i = 0; i < multipoint.PointCount - 1; i++)
                {
                    multipoint.Point[i].Project(polyline.SpatialReference);
                    multipoint.Point[i + 1].Project(polyline.SpatialReference);

                    resultPolylines.Add(CreatePolylineFromPoints(multipoint.Point[i], multipoint.Point[i + 1]));
                    i++;
                }
            }

            return resultPolylines;
        }

        public static Dictionary<int, IGeometry> GetGeometriesFromLayer(IFeatureLayer featureLayer, IActiveView activeView)
        {
            var geometries = new Dictionary<int, IGeometry>();
            var featureClass = featureLayer.FeatureClass;

            var idFieldIndex = featureClass.FindField(featureClass.OIDFieldName);

            if (idFieldIndex == -1)
            {
                logger.WarnEx($"> GetGeometriesFromLayer. Warning: Cannot find fild {featureClass.OIDFieldName} in featureClass {featureClass.AliasName}");
                throw new MissingFieldException();
            }

            IQueryFilter queryFilter = new QueryFilter();
            queryFilter.WhereClause = $"{featureClass.OIDFieldName} > 0";

            IFeatureCursor featureCursor = featureClass.Search(queryFilter, true);
            IFeature feature = featureCursor.NextFeature();
            try
            {
                while (feature != null)
                {
                    var shape = feature.ShapeCopy;
                    shape.Project(activeView.FocusMap.SpatialReference);
                    var id = (int)feature.Value[idFieldIndex];

                    geometries.Add(id, shape);

                    feature = featureCursor.NextFeature();
                }
            }
            catch (Exception ex)
            {
                logger.ErrorEx($"> GetGeometriesFromLayer Exception. ex.Message:{ex.Message}");
            }
            finally
            {
                Marshal.ReleaseComObject(featureCursor);
            }

            return geometries;
        }

        private static void AddLayersToMapAsGroupLayer(
            IEnumerable<ILayer> layers,
            string sessionName,
            short transparency,
            ILayer relativeLayer,
            bool isGroupLayerAbove,
            IActiveView activeView,
            IEnumerable<ILayer> calcRasters)
        {
            IGroupLayer groupLayer = new GroupLayerClass();
            groupLayer.Name = sessionName;

            foreach (var layer in layers)
            {
                if (layer is IRasterLayer)
                {
                    var layerEffects = (ILayerEffects)layer;
                    layerEffects.Transparency = transparency;
                }
                groupLayer.Add(layer);
            }

            var mapLayers = activeView.FocusMap as IMapLayers2;
            int relativeLayerPosition = GetLayerIndex(relativeLayer, activeView);
            int groupLayerPosition = (isGroupLayerAbove) ? relativeLayerPosition - 1 : relativeLayerPosition + 1;
            mapLayers.InsertLayer(groupLayer, false, groupLayerPosition);

            if (calcRasters != null)
            {
                foreach (var raster in calcRasters)
                {
                    var layerEffects = (ILayerEffects)raster;
                    layerEffects.Transparency = transparency;

                    IMoveLayersOperation moveOperation = new MoveLayersOperationClass();
                    moveOperation.AddLayerInfo(raster, activeView.FocusMap, null);
                    moveOperation.SetDestinationInfo(layers.Count(), activeView.FocusMap, groupLayer);

                    var doOperation = (IOperation)moveOperation;
                    doOperation.Do();
                }
            }
        }

        private static ILayer GetVisibilityLayer(string gdb, string datasetName)
        {
            IWorkspaceFactory workspaceFactory = new FileGDBWorkspaceFactory();
            IWorkspace workspace = workspaceFactory.OpenFromFile(gdb, 0);
            var datasets = workspace.Datasets[esriDatasetType.esriDTAny];
            var currentDataset = datasets.Next();

            while (currentDataset != null && !currentDataset.Name.EndsWith(datasetName))
            {
                currentDataset = datasets.Next();
            }
            Marshal.ReleaseComObject(workspaceFactory);

            if (currentDataset != null)
            {
                if (currentDataset.Type == esriDatasetType.esriDTRasterDataset)
                {
                    return GetRasterLayer(currentDataset as IRasterDataset);
                }

                if (currentDataset.Type == esriDatasetType.esriDTFeatureClass)
                {
                    return GetFeatureLayer(currentDataset as IFeatureClass);
                }
            }
            return null;
        }

        public static ILayer GetFeatureLayer(IFeatureClass dataset)
        {
            var featurelayer = new FeatureLayer();
            featurelayer.Name = dataset.AliasName;
            featurelayer.FeatureClass = dataset;

            return featurelayer;
        }

        public static ILayer GetRasterLayer(IRasterDataset rasterDataset)
        {
            IRasterLayer rasterLayer = new RasterLayer();
            rasterLayer.CreateFromDataset(rasterDataset);

            return rasterLayer;
        }

        public static double GetMinDistance(double minDistance, double minAngle, double height)
        {
            // If the inner radius (min distance) is 0 we didn`t need to check min distance restrictions
            // so return the distance to the intersection of the surface and the lower tilt limit
            if (minDistance == 0)
            {
                // If the min distance and the lower titl angle have the minimum values return 
                // the minimum possible distance (0 m)
                if (minAngle > -90)
                {
                    var minAngleRadians = (minAngle + 90) * (Math.PI / 180);
                    var radians = Math.Tan(minAngleRadians);
                    return radians * (180 / Math.PI) * height;
                }
                else
                {
                    return 0;
                }
            }

            // Find the tilt angle to the nearest point from observation point
            var angle = FindAngleByDistanceAndHeight(height, minDistance);
            
            // If founded angle less than the min angle from parameters return the distance to the 
            // intersection of the surface and the lower tilt limit
            if (angle < minAngle)
            {
                var minAngleRadians = (minAngle + 90) * (Math.PI / 180);
                var radians = Math.Tan(minAngleRadians);
                return radians * (180 / Math.PI) * height;
            }
            else
            {
                return minDistance;
            }
        }

        public static double GetMaxDistance(double maxDistance, double maxAngle, double height)
        {
            // Find the tilt angle to the farthest point from observation point
            var angle = FindAngleByDistanceAndHeight(height, maxDistance);

            // If founded angle more than the max angle from parameters return the distance to the 
            // intersection of the surface and the upper tilt limit
            if (angle > maxAngle)
            {
                var maxAngleRadians = (maxAngle + 90) * (Math.PI / 180);
                var radians = Math.Tan(maxAngleRadians);
                return radians * (180 / Math.PI) * height;
            }
            else
            {
                return maxDistance;
            }
        }

        public static double FindAngleByDistanceAndHeight(double height, double distance)
        {
            // Calculate a distance from point to the end of required distance - hypotenuse
            var toMaxPointDistance = Math.Sqrt(Math.Pow(height, 2) + Math.Pow(distance, 2));

            // Find the angle between perpendicular from observation point to surface (height), 
            // which is the tilt angle to the required point 
            //              |\
            //              | \ <---- this angle
            //      1 - >   |  \
            //              |   \
            //              |    \
            //              |_____\<-2     1- height 2- required distance
            var sin = distance / toMaxPointDistance;
            var angle = -90 + Math.Asin(sin) * (180 / Math.PI);

            return angle;
        }

        public static IPolygon GetCoverageArea(
            IPoint point,
            double azimuthB,
            double azimuthE,
            double minDistance,
            double maxDistance,
            IPolygon observObject = null)
        {
            logger.InfoEx("> GetCoverageArea START azimuthB:{0} azimuthE:{1} minDistance:{2} maxDistance:{3}",
                azimuthB, azimuthE, minDistance, maxDistance);

            IPolygon coverageArea;

            if (azimuthB == 0 && azimuthE == 360)
            {
                ICircularArc outArc = new CircularArcClass();
                outArc.PutCoordsByAngle(point, 0, 2 * Math.PI, maxDistance);
                ISegmentCollection outFullRing = new RingClass();
                ISegment segmentOut = (ISegment)outArc;
                outFullRing.AddSegment(segmentOut);
                IRing outFullRingGeometry = outFullRing as IRing;
                if (!outFullRingGeometry.IsExterior)
                {
                    outFullRingGeometry.ReverseOrientation();
                }

                ICircularArc innerArc = new CircularArcClass();
                innerArc.PutCoordsByAngle(point, 0, 2 * Math.PI, minDistance);
                ISegmentCollection innerFullRing = new RingClass();
                ISegment segmentIn = (ISegment)innerArc;
                innerFullRing.AddSegment(segmentIn);
                IRing innerFullRingGeometry = innerFullRing as IRing;
                if (innerFullRingGeometry.IsExterior)
                {
                    innerFullRingGeometry.ReverseOrientation();
                }

                IGeometryCollection polygonRound = new PolygonClass();
                polygonRound.AddGeometry(outFullRing as IGeometry);
                polygonRound.AddGeometry(innerFullRing as IGeometry);
                coverageArea = polygonRound as IPolygon;
            }
            else
            {
                ISegmentCollection outRing = new RingClass();

                var pointFromOutArc = GetPointByAzimuthAndLength(point, azimuthB, maxDistance);
                var pointToOutArc = GetPointByAzimuthAndLength(point, azimuthE, maxDistance);
                var pointFromInnerArc = GetPointByAzimuthAndLength(point, azimuthB, minDistance);
                var pointToInnerArc = GetPointByAzimuthAndLength(point, azimuthE, minDistance);

                ILine rightLine = new LineClass()
                {
                    FromPoint = pointFromInnerArc,
                    ToPoint = pointFromOutArc,
                    SpatialReference = point.SpatialReference
                };
                var rightLineSeg = (ISegment)rightLine;
                outRing.AddSegment(rightLineSeg);

                ICircularArc circularArc = new CircularArcClass();
                circularArc.PutCoords(point, pointFromOutArc, pointToOutArc, esriArcOrientation.esriArcClockwise);
                ISegment outArcSeg = (ISegment)circularArc;
                outRing.AddSegment(outArcSeg);

                ILine leftLine = new LineClass()
                {
                    FromPoint = pointToOutArc,
                    ToPoint = pointToInnerArc,
                    SpatialReference = point.SpatialReference
                };
                var leftLineSeg = (ISegment)leftLine;
                outRing.AddSegment(leftLineSeg);

                if (minDistance > 0)
                {
                    ICircularArc circularArcI = new CircularArcClass();
                    circularArcI.PutCoords(point, pointToInnerArc, pointFromInnerArc, esriArcOrientation.esriArcCounterClockwise);
                    ISegment outArcSegI = (ISegment)circularArcI;
                    outRing.AddSegment(outArcSegI);
                }

                IGeometryCollection outRoundPolygon = new PolygonClass();
                //IGeometry g = outRing as IGeometry;
                outRoundPolygon.AddGeometry(outRing as IGeometry);

                //IPolygon outPolygonGeometry = outRoundPolygon as IPolygon;

                coverageArea = outRoundPolygon as IPolygon;

                //if (minDistance > 0)
                //{
                //    ISegmentCollection innerRing = new RingClass();

                //    ILine innerRightLine = new LineClass()
                //    {
                //        FromPoint = point,
                //        ToPoint = pointFromInnerArc,
                //        SpatialReference = point.SpatialReference
                //    };
                //    var rightLineSegIn = (ISegment)innerRightLine;

                //    innerRing.AddSegment(rightLineSegIn);

                //    ICircularArc invisibleCircularArc = new CircularArcClass();
                //    invisibleCircularArc.PutCoords(point, pointFromInnerArc, pointToInnerArc, esriArcOrientation.esriArcClockwise);

                //    ISegment innerRingSeg = (ISegment)invisibleCircularArc;
                //    innerRing.AddSegment(innerRingSeg);

                //    ILine innerLeftLine = new LineClass()
                //    {
                //        FromPoint = pointToInnerArc,
                //        ToPoint = point,
                //        SpatialReference = point.SpatialReference
                //    };
                //    var leftLineSegIn = (ISegment)innerLeftLine;

                //    innerRing.AddSegment(leftLineSegIn);

                //    IGeometryCollection polygonIn = new PolygonClass();
                //    polygonIn.AddGeometry(innerRing as IGeometry);

                //    IPolygon innerPolygonGeometry = polygonIn as IPolygon;
                //    try
                //    {
                //        ITopologicalOperator arcTopoOp = outPolygonGeometry as ITopologicalOperator;
                //        var diff = arcTopoOp.Difference(innerPolygonGeometry);
                //        coverageArea = diff as IPolygon;
                //    }
                //    catch(Exception ex)
                //    {
                //        logger.ErrorEx($"GetCoverageArea Exception (1): {ex.Message}");
                //        coverageArea = null;
                //    }
                //}
                //else
                //{
                //    coverageArea = outPolygonGeometry;
                //}
            }

            if (observObject != null)
            {
                logger.InfoEx("GetCoverageArea. observObject IS NOT NULL");
                try
                {
                    observObject.Project(point.SpatialReference);

                    var polygonGeometry = observObject as IGeometry;
                    ITopologicalOperator polygonTopoOp = coverageArea as ITopologicalOperator;
                    IPolygon ip = polygonTopoOp.Intersect(polygonGeometry, esriGeometryDimension.esriGeometry2Dimension) as IPolygon;

                    logger.ErrorEx($"> GetCoverageArea END. polygonTopoOp.Intersect OK");
                    return ip;
                }
                catch (Exception ex)
                {
                    logger.ErrorEx($"> GetCoverageArea. Exception (2): {ex.Message}");
                    return null;
                }
            }
            else
            {
                logger.InfoEx("GetCoverageArea observObject. IS NULL");
            }

            logger.InfoEx("> GetCoverageArea END");
            return coverageArea;
        }

        public static double GetObjVisibilityArea(IFeatureClass visibility, IPolygon observObject, int gridCode = -1)
        {
            logger.InfoEx("> GetObjVisibilityArea START. FeatureClass visibility:{0} gridCode:{1}", visibility.AliasName, gridCode);

            var visibilityPolygon = GetTotalPolygonFromFeatureClass(visibility, gridCode);

            //for test only-------------------------------------------------------------
            //try
            //{
            //    var ff = visibility.CreateFeature();
            //    ff.Shape = visibilityPolygon;
            //    ff.set_Value(visibility.FindField("id"), 525);
            //    ff.set_Value(visibility.FindField("gridCode"), gridCode);
            //    ff.Store();
            //    logger.InfoEx("GetObjVisibilityArea save TEST Feature 1 OK ff.OID: {0} gridCode: {1}", ff.OID, gridCode);

            //    var ff1 = visibility.CreateFeature();
            //    ff1.Shape = observObject;
            //    ff1.set_Value(visibility.FindField("id"), 5252);
            //    ff1.set_Value(visibility.FindField("gridCode"), gridCode);
            //    ff1.Store();
            //    logger.InfoEx("GetObjVisibilityArea save TEST Feature 2 OK ff.OID: {0} gridCode: {1}", ff1.OID, gridCode);
            //}
            //catch (Exception ex)
            //{
            //    logger.ErrorEx($"GetObjVisibilityArea Exception: {ex.Message}");
            //}
            //for test only end

            try
            {
                if (visibilityPolygon == null || visibilityPolygon.IsEmpty)
                {
                    logger.ErrorEx($"> GetObjVisibilityArea Error. Visibility polygon from {visibility} is empty");
                    return 0;
                }

                var polygonGeometry = observObject as IGeometry;
                polygonGeometry.Project(visibilityPolygon.SpatialReference);

                ITopologicalOperator polygonTopoOp = visibilityPolygon as ITopologicalOperator;
                var resultPolygon = polygonTopoOp.Intersect(polygonGeometry, esriGeometryDimension.esriGeometry2Dimension) as IPolygon;
                var resultArea = (IArea)resultPolygon;

                //var resultArea = (IArea)visibilityPolygon;

                logger.InfoEx("> GetObjVisibilityArea END");
                return resultArea.Area;
            }
            catch (Exception ex)
            {
                logger.ErrorEx($"> GetObjVisibilityArea Exception: {ex.Message}");
                return 0;
            }
        }

        public static IPolygon GetTotalPolygon(List<IPolygon> polygons)
        {
            logger.InfoEx("> GetTotalPolygon START");

            if (polygons == null || polygons.Count == 0)
            {
                logger.ErrorEx("> GetTotalPolygon END. NULL poligons");
                return null;
            }

            IGeometry geometryBag = new GeometryBagClass();
            geometryBag.SpatialReference = polygons[0].SpatialReference;
            IGeometryCollection geometryCollection = geometryBag as IGeometryCollection;

            foreach (var polygon in polygons)
            {
                try
                {
                    object missing = Type.Missing;
                    geometryCollection.AddGeometry(polygon, ref missing, ref missing);
                }
                catch (Exception ex)
                {
                    logger.ErrorEx($"GetTotalPolygon. Exception (1): {0}", ex.Message);
                }
            }

            try
            {
                ITopologicalOperator unionedPolygon = new PolygonClass();
                unionedPolygon.ConstructUnion(geometryBag as IEnumGeometry);

                logger.InfoEx("> GetTotalPolygon END");
                return unionedPolygon as IPolygon;
            }
            catch (Exception ex)
            {
                logger.ErrorEx($"> GetTotalPolygon Exception (2): {ex.Message}");
                return null;
            }
        }

        public static double GetTotalAreaFromFeatureClass(IFeatureClass featureClass, int gridCode = -1)
        {
            logger.InfoEx($"> GetTotalAreaFromFeatureClass START");

            if (featureClass == null)
            {
                logger.InfoEx("> GetTotalAreaFromFeatureClass END featureClass IS NULL: {0}", featureClass.AliasName);
                return 0;
            }

            double result = 0;
            try
            {
                int gridCodeIndex = featureClass.FindField("gridcode");
                int areaCodeIndex = featureClass.FindField("Shape_Area");
                logger.InfoEx("GetTotalAreaFromFeatureClass. gridCodeIndex:{0} areaCodeIndex:{1}", gridCodeIndex, areaCodeIndex);

                IGeoDataset geoDataset = featureClass as IGeoDataset;

                IFeatureCursor featureCursor = featureClass.Search(null, false);
                IFeature currentFeature = featureCursor.NextFeature();

                if (gridCode != -1)
                {
                    while (currentFeature != null)
                    {
                        if ((int)currentFeature.Value[gridCodeIndex] == gridCode)
                        {
                            result += (double)currentFeature.Value[areaCodeIndex];
                        }
                        currentFeature = featureCursor.NextFeature();
                    }
                }
                else
                {
                    while (currentFeature != null)
                    {
                        result += (double)currentFeature.Value[areaCodeIndex];
                        currentFeature = featureCursor.NextFeature();
                    }
                }
                Marshal.ReleaseComObject(featureCursor);
            }
            catch (Exception ex)
            {
                logger.InfoEx("> GetTotalAreaFromFeatureClass Exception:{0}", ex.Message);
            }

            logger.InfoEx("> GetTotalAreaFromFeatureClass END result:{0}", result);
            return result;
        }

        public static IPolygon GetTotalPolygonFromFeatureClass(IFeatureClass featureClass, int gridCode = -1)
        {
            logger.InfoEx("> GetTotalPolygonFromFeatureClass START. gridCode:{0}", gridCode);

            if (featureClass == null)
            {
                logger.InfoEx("> GetTotalPolygonFromFeatureClass END. featureClass IS NULL");
                return null;
            }

            IGeometry geometryBag = new GeometryBagClass();
            try
            {
                int gridCodeIndex = featureClass.FindField("gridcode");
                IGeoDataset geoDataset = featureClass as IGeoDataset;
                geometryBag.SpatialReference = geoDataset.SpatialReference;
                IGeometryCollection geometryCollection = geometryBag as IGeometryCollection;
                IFeatureCursor featureCursor = featureClass.Search(null, false);
                IFeature currentFeature = featureCursor.NextFeature();

                while (currentFeature != null)
                {
                    if (gridCode == -1 || (int)currentFeature.Value[gridCodeIndex] == gridCode)
                    {
                        object missing = Type.Missing;
                        geometryCollection.AddGeometry(currentFeature.Shape, ref missing, ref missing);
                    }
                    currentFeature = featureCursor.NextFeature();
                }
                Marshal.ReleaseComObject(featureCursor);

                try
                {
                    ITopologicalOperator unionedPolygon = new PolygonClass();
                    unionedPolygon.ConstructUnion(geometryBag as IEnumGeometry);

                    logger.InfoEx("> GetTotalPolygonFromFeatureClass END");

                    return unionedPolygon as IPolygon;
                }
                catch (Exception ex)
                {
                    logger.ErrorEx($"> GetTotalPolygonFromFeatureClass Exception (1): {ex.Message}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                logger.ErrorEx($"> GetTotalPolygonFromFeatureClass Exception (2): {ex.Message}");
                return null;
            }
        }

        private static IPoint GetPointByAzimuthAndLength(IPoint centerPoint, double azimuth, double distance)
        {
            double radian = (90 - azimuth) * (Math.PI / 180);
            return GetPointFromAngelAndDistance(centerPoint, radian, distance);
        }

        public static void SetFeatureLayerStyle(IFeatureLayer feaureLayer, ISymbol featureLayerSymbol)
        {
            if (feaureLayer == null)
            {
                return;
            }

            IGeoFeatureLayer geoFeatureLayer = (IGeoFeatureLayer)feaureLayer;
            ISimpleRenderer simpleRenderer = (ISimpleRenderer)geoFeatureLayer.Renderer;
            //Create a new renderer
            simpleRenderer = new SimpleRendererClass();
            //Set its symbol from the styleGalleryItem
            simpleRenderer.Symbol = featureLayerSymbol;
            //Set the renderer into the geoFeatureLayer
            geoFeatureLayer.Renderer = (IFeatureRenderer)simpleRenderer;
        }
        public static void SetRasterLayerStyle(ILayer layer, ISymbol featureLayerSymbol)
        {
            if (layer == null)
            {
                return;
            }


            if (layer is IFeatureLayer feaureLayer)
            {
                IGeoFeatureLayer geoFeatureLayer = (IGeoFeatureLayer)feaureLayer;
                ISimpleRenderer simpleRenderer = (ISimpleRenderer)geoFeatureLayer.Renderer;
                //Create a new renderer
                simpleRenderer = new SimpleRendererClass();
                //Set its symbol from the styleGalleryItem
                simpleRenderer.Symbol = featureLayerSymbol;
                //Set the renderer into the geoFeatureLayer
                geoFeatureLayer.Renderer = (IFeatureRenderer)simpleRenderer;
            }
        }

        public static bool IsRasterEmpty(IRasterDataset2 rasterDataset)
        {
            if (rasterDataset == null)
            {
                return true;
            }

            IRaster2 inputRaster = (IRaster2)rasterDataset.CreateFullRaster();
            IRasterBandCollection bands = (IRasterBandCollection)inputRaster;

            if (bands.Count == 0)
            {
                return true;
            }

            //int i = 0;
            //for (i = 0; i <=bands.Count ; i++)
            IRasterBand rasterBand = bands.Item(0);
            IRasterStatistics rs = rasterBand.Statistics;

            if(rs == null)
            {
                return true;
            }

            var max = rs.Maximum;
            var min = rs.Minimum;

            return rs.Minimum == double.MinValue || rs.Maximum == double.MinValue;
        }

        public static void FlashGeometry(
            IGeometry geometry,
            Int32 delay,
            IApplication application)
        {
            var mxdoc = application.Document as IMxDocument;
            if (mxdoc == null)
                return;

            var av = (IActiveView)mxdoc.FocusMap;
            var display = av.ScreenDisplay;
            var envelope = av.Extent.Envelope;

            IRgbColor color = new RgbColorClass();
            color.Green = 255;
            color.Red = 0;
            color.Blue = 0;

            if ((geometry == null)
                || (geometry.GeometryType != ESRI.ArcGIS.Geometry.esriGeometryType.esriGeometryPoint)
                || (color == null)
                || (display == null)
                || (envelope == null)
                || (delay < 0))
            {
                return;
            }

            display.StartDrawing(display.hDC, (System.Int16)ESRI.ArcGIS.Display.esriScreenCache.esriNoScreenCache); // Explicit Cast

            //Set the flash geometry's symbol.
            ESRI.ArcGIS.Display.ISimpleMarkerSymbol simpleMarkerSymbol = new ESRI.ArcGIS.Display.SimpleMarkerSymbolClass();
            simpleMarkerSymbol.Style = ESRI.ArcGIS.Display.esriSimpleMarkerStyle.esriSMSCircle;
            simpleMarkerSymbol.Size = 12;
            simpleMarkerSymbol.Color = color;
            ESRI.ArcGIS.Display.ISymbol markerSymbol = (ESRI.ArcGIS.Display.ISymbol)simpleMarkerSymbol;
            markerSymbol.ROP2 = ESRI.ArcGIS.Display.esriRasterOpCode.esriROPNotXOrPen;

            ESRI.ArcGIS.Display.ISimpleLineSymbol simpleLineSymbol = new ESRI.ArcGIS.Display.SimpleLineSymbolClass();
            simpleLineSymbol.Width = 1;
            simpleLineSymbol.Color = color;
            ESRI.ArcGIS.Display.ISymbol lineSymbol = (ESRI.ArcGIS.Display.ISymbol)simpleLineSymbol;
            lineSymbol.ROP2 = ESRI.ArcGIS.Display.esriRasterOpCode.esriROPNotXOrPen;

            DrawCrossHair(geometry, display, envelope, markerSymbol, lineSymbol, application);

            //Flash the input point geometry.
            display.SetSymbol(markerSymbol);
            display.DrawPoint(geometry);
            System.Threading.Thread.Sleep(delay);
            display.DrawPoint(geometry);

            display.FinishDrawing();
        }

        private static void DrawCrossHair(
            ESRI.ArcGIS.Geometry.IGeometry geometry,
            ESRI.ArcGIS.Display.IDisplay display,
            IEnvelope extent,
            ISymbol markerSymbol,
            ISymbol lineSymbol,
            IApplication application)
        {
            try
            {
                var point = geometry as IPoint;

                if ((point == null) || (display == null) || (extent == null) || (markerSymbol == null) ||
                    (lineSymbol == null) || (application == null))
                    return;

                var numSegments = 10;

                var latitudeMid = point.Y;//envelope.YMin + ((envelope.YMax - envelope.YMin) / 2);
                var longitudeMid = point.X;
                var leftLongSegment = (point.X - extent.XMin) / numSegments;
                var rightLongSegment = (extent.XMax - point.X) / numSegments;
                var topLatSegment = (extent.YMax - point.Y) / numSegments;
                var bottomLatSegment = (point.Y - extent.YMin) / numSegments;
                var fromLeftLong = extent.XMin;
                var fromRightLong = extent.XMax;
                var fromTopLat = extent.YMax;
                var fromBottomLat = extent.YMin;
                var av = (application.Document as IMxDocument).ActiveView;
                if (av == null)
                    return;

                var leftPolyline = new PolylineClass();
                var rightPolyline = new PolylineClass();
                var topPolyline = new PolylineClass();
                var bottomPolyline = new PolylineClass();

                leftPolyline.SpatialReference = geometry.SpatialReference;
                rightPolyline.SpatialReference = geometry.SpatialReference;
                topPolyline.SpatialReference = geometry.SpatialReference;
                bottomPolyline.SpatialReference = geometry.SpatialReference;

                var leftPC = (IPointCollection)leftPolyline;
                var rightPC = (IPointCollection)rightPolyline;
                var topPC = (IPointCollection)topPolyline;
                var bottomPC = (IPointCollection)bottomPolyline;

                leftPC.AddPoint(new PointClass() { X = fromLeftLong, Y = latitudeMid });
                rightPC.AddPoint(new PointClass() { X = fromRightLong, Y = latitudeMid });
                topPC.AddPoint(new PointClass() { X = longitudeMid, Y = fromTopLat });
                bottomPC.AddPoint(new PointClass() { X = longitudeMid, Y = fromBottomLat });

                for (int x = 1; x <= numSegments; x++)
                {
                    //Flash the input polygon geometry.
                    display.SetSymbol(markerSymbol);
                    display.SetSymbol(lineSymbol);

                    leftPC.AddPoint(new PointClass() { X = fromLeftLong + leftLongSegment * x, Y = latitudeMid });
                    rightPC.AddPoint(new PointClass() { X = fromRightLong - rightLongSegment * x, Y = latitudeMid });
                    topPC.AddPoint(new PointClass() { X = longitudeMid, Y = fromTopLat - topLatSegment * x });
                    bottomPC.AddPoint(new PointClass() { X = longitudeMid, Y = fromBottomLat + bottomLatSegment * x });

                    // draw
                    display.DrawPolyline(leftPolyline);
                    display.DrawPolyline(rightPolyline);
                    display.DrawPolyline(topPolyline);
                    display.DrawPolyline(bottomPolyline);

                    System.Threading.Thread.Sleep(15);
                    display.FinishDrawing();
                    av.PartialRefresh(esriViewDrawPhase.esriViewForeground, null, null);
                    System.Windows.Forms.Application.DoEvents();
                    display.StartDrawing(display.hDC, (System.Int16)ESRI.ArcGIS.Display.esriScreenCache.esriNoScreenCache); // Explicit Cast
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        public static bool IsPointOnExtent(IEnvelope envelope, IPoint inPoint)
        {
            var point = inPoint.Clone();
            point.Project(envelope.SpatialReference);

            if (point.X >= envelope.XMin && point.X <= envelope.XMax && point.Y >= envelope.YMin && point.Y <= envelope.YMax)
            {
                return true;
            }

            return false;
        }

        public static double GetExtentHeightInMapUnits(IEnvelope envelope, double extentHeight)
        {
            return envelope.Height * 0.1;
        }

        public static List<IPoint> GetPointsFromGeometries(IEnumerable<IGeometry> geometries, ISpatialReference spatialReference, out bool isCircle)
        {
            isCircle = false;
            var points = new List<IPoint>();

            if (geometries.First().GeometryType == esriGeometryType.esriGeometryPoint)
            {

                foreach (var geometry in geometries)
                {
                    var point = geometry as IPoint;
                    point.Project(spatialReference);
                    points.Add(point);
                }

                return points;
            }
            else
            {
                foreach (var geometry in geometries)
                {
                    var path = geometry as IPointCollection;

                    for (int i = 0; i < path.PointCount; i++)
                    {
                        if (geometries.Count() == 1 && i == path.PointCount - 1 && path.Point[i].X == path.Point[0].X && path.Point[0].Y == path.Point[i].Y)
                        {
                            isCircle = true;
                            continue;
                        }

                        var point = path.Point[i].Clone();
                        point.Project(spatialReference);
                        points.Add(point);
                    }
                }

                return points;
            }
        }

        public static double GetFormattedAzimuth(double azimuth)
        {
            if (azimuth > 180)
            {
                return azimuth -= 360;
            }

            return azimuth;
        }

    }
}
