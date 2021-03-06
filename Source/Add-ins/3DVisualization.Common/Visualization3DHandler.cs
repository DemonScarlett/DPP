﻿using ESRI.ArcGIS.Analyst3D;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.ArcScene;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using MilSpace.Configurations;
using MilSpace.Core;
using MilSpace.Core.Tools;
using MilSpace.DataAccess.DataTransfer;
using MilSpace.Visualization3D.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace MilSpace.Visualization3D
{
    public static class Visualization3DHandler
    {
        private static IApplication m_application;
        private static Logger logger = Logger.GetLoggerEx("MilSpace.Visualization3D");

        //Application removed event
        private static IAppROTEvents_Event m_appROTEvent;
        private static int m_appHWnd = 0;
        private static double _zFactor;
        private static readonly string _profileGdb = MilSpaceConfiguration.ConnectionProperty.TemporaryGDBConnection;
        private static IActiveView _map;
        private static List<ILayer> _viewCalcLayers = new List<ILayer>();
        private static List<string> _layersWithDefaultRenderer = new List<string>();
        private static string _demLayerName;

        private enum LayerTypeEnum
        {
            Raster,
            LineFeature,
            PointFeature,
            PolygonFeature
        }
        
        static Visualization3DHandler()
        {
        }

        internal static void OpenProfilesSetIn3D(ArcSceneArguments layers, IActiveView map)
        {
            OpenArcScene();

            try
            {
                UpdateTemporaryDataStorage();

                IObjectFactory objFactory = m_application as IObjectFactory;
                var document = (IBasicDocument)m_application.Document;

                _zFactor = layers.ZFactor;
                _map = map;

                var baseSurface = AddBaseLayers(layers, objFactory, document);
                AddVisibilityLayers(layers.VisibilityResultsInfo, objFactory, document, baseSurface);
                AddExtraLayers(layers.AdditionalLayers, objFactory, document, baseSurface);

                if(!String.IsNullOrEmpty(layers.DraperyLayer))
                {
                    AddDraperyLayer(layers.DraperyLayer, objFactory, baseSurface, document);
                }

                var surfaceLayer = EsriTools.GetLayer(_demLayerName, document.ActiveView.FocusMap);

                if (surfaceLayer != null)
                {
                    SetSceneView(document, surfaceLayer as IRasterLayer);
                }
            }
            catch (Exception ex)
            {
                logger.ErrorEx(ex.Message);
            }

        }

        internal static void ClosingHandler()
        {
            if (m_appROTEvent != null)
            {
                m_appROTEvent.AppRemoved -= new IAppROTEvents_AppRemovedEventHandler(m_appROTEvent_AppRemoved);
                m_appROTEvent = null;
            }
        }


        public static string GetWorkspacePathForLayer(ILayer layer)
        {
            if (layer == null || !(layer is IDataset))
            {
                return null;
            }

            IDataset dataset = (IDataset)(layer);

            return dataset.Workspace.PathName;
        }

        private static IFunctionalSurface AddBaseLayers(ArcSceneArguments layers, IObjectFactory objFactory, IBasicDocument document)
        {
            var preparedLayers = GetLayers(layers, objFactory);

            var surface = (IRasterSurface)objFactory.Create("esrianalyst3d.RasterSurface");
            var rasterLayer = (IRasterLayer)preparedLayers[LayerTypeEnum.Raster];
             SetFromMapRendererToRasterLayer(rasterLayer, objFactory, rasterLayer.Name);

            surface.PutRaster(rasterLayer.Raster, 0);
            var functionalSurface = (IFunctionalSurface)surface;
            _demLayerName = rasterLayer.Name;

            SetSurface3DProperties(preparedLayers[LayerTypeEnum.Raster], objFactory, functionalSurface);

            if (preparedLayers.Count > 1)
            {
                SetFeatures3DProperties((IFeatureLayer)preparedLayers[LayerTypeEnum.LineFeature], objFactory, functionalSurface);
                SetHightFeatures3DProperties((IFeatureLayer)preparedLayers[LayerTypeEnum.PointFeature], objFactory);
                SetHightFeatures3DProperties((IFeatureLayer)preparedLayers[LayerTypeEnum.PolygonFeature], objFactory);

                _viewCalcLayers.Add(preparedLayers[LayerTypeEnum.LineFeature]);
                _layersWithDefaultRenderer.AddRange(preparedLayers.Values.Select(layer => layer.Name));
            }

            foreach (var layer in preparedLayers)
            {
                try
                {
                    document.AddLayer(layer.Value);
                }
                catch (Exception ex)
                {
                    logger.ErrorEx(ex.Message);
                }
            }

            document.UpdateContents();

            if (preparedLayers.Count > 1)
            {
                document.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography,
                                                   VisibilityColorsRender((IFeatureLayer)preparedLayers[LayerTypeEnum.LineFeature], objFactory), document.ActiveView.Extent);

                document.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography,
                                                      PointsRender((IFeatureLayer)preparedLayers[LayerTypeEnum.PointFeature], new RgbColor() { Red = 255, Blue = 24, Green = 198 }, objFactory), document.ActiveView.Extent);

                document.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography,
                                                   VisibilityColorsRender((IFeatureLayer)preparedLayers[LayerTypeEnum.PolygonFeature], objFactory), document.ActiveView.Extent);
            }

            return functionalSurface;
        }

        private static void AddExtraLayers(Dictionary<ILayer, double> additionalLayers, IObjectFactory objFactory,
                                            IBasicDocument document, IFunctionalSurface surface)
        {
            foreach (var extraLayer in additionalLayers)
            {
                var featureLayer = CreateLayerCopy((IFeatureLayer)extraLayer.Key, objFactory);
                SetFeatures3DProperties(featureLayer, objFactory, surface, extraLayer.Value);

                try
                {
                    document.AddLayer(featureLayer);
                }
                catch (Exception ex)
                {
                    logger.ErrorEx(ex.Message);
                }
            }

            document.UpdateContents();
        }

        private static void AddVisibilityLayers(IEnumerable<VisibilityResultInfo> info,
                                                IObjectFactory objFactory, IBasicDocument document,
                                                IFunctionalSurface baseSurface)
        {
            Dictionary<ILayer, LayerTypeEnum> layers = new Dictionary<ILayer, LayerTypeEnum>();

            foreach (var resultInfo in info)
            {
                var layer = GetVisibilityLayer(resultInfo, objFactory, baseSurface);

                if (layer.Key != null)
                {
                    layers.Add(layer.Key, layer.Value);

                    try
                    {
                        document.AddLayer(layer.Key);
                    }
                    catch (Exception ex)
                    {
                        logger.ErrorEx(ex.Message);
                    }
                }
            }

            _viewCalcLayers.AddRange(layers.Keys);

            if (layers.ContainsValue(LayerTypeEnum.PointFeature))
            {
                document.UpdateContents();

                foreach (var layer in layers)
                {
                    if (layer.Value == LayerTypeEnum.PointFeature
                            && _layersWithDefaultRenderer.Contains(layer.Key.Name))
                    {
                        document.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography,
                                                   PointsRender((IFeatureLayer)layer.Key, new RgbColor() { Red = 24, Blue = 255, Green = 163 }, objFactory), document.ActiveView.Extent);
                    }
                }
            }
        }

        private static KeyValuePair<ILayer, LayerTypeEnum> GetVisibilityLayer(VisibilityResultInfo info, IObjectFactory objFactory, IFunctionalSurface baseSurface)
        {
            KeyValuePair<ILayer, LayerTypeEnum> layerKeyValuePair = new KeyValuePair<ILayer, LayerTypeEnum>();
            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            string typeFactoryID = factoryType.GUID.ToString("B");

            IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)objFactory.Create(typeFactoryID);
            IWorkspace2 workspace = (IWorkspace2)workspaceFactory.OpenFromFile(info.GdbPath, 0);

            var rastersTypes = VisibilityCalcResults.GetRasterResults();

            if (rastersTypes.Any(type => type == info.RessutType))
            {
                var rasterLayer = CreateRasterLayer(info.ResultName, workspace, objFactory, info.GdbPath);
                if (rasterLayer != null)
                {
                    SetVisibilitySessionRaster3DProperties(rasterLayer, objFactory, baseSurface);
                    layerKeyValuePair = new KeyValuePair<ILayer, LayerTypeEnum>(rasterLayer, LayerTypeEnum.Raster);
                }
            }

            if (info.RessutType == VisibilityCalculationResultsEnum.ObservationPoints || info.RessutType == VisibilityCalculationResultsEnum.ObservationPointSingle)
            {
                var pointFeatureLayer = CreateFeatureLayer(info.ResultName, workspace, objFactory);
                if (pointFeatureLayer != null)
                {
                    SetFeatures3DProperties(pointFeatureLayer, objFactory, baseSurface);
                    layerKeyValuePair = new KeyValuePair<ILayer, LayerTypeEnum>(pointFeatureLayer, LayerTypeEnum.PointFeature);
                }
            }

            if (info.RessutType == VisibilityCalculationResultsEnum.VisibilityAreaPolygons || info.RessutType == VisibilityCalculationResultsEnum.ObservationObjects)
            {
                var polygonFeatureLayer = CreateFeatureLayer(info.ResultName, workspace, objFactory);
                if (polygonFeatureLayer != null)
                {
                    SetFeatures3DProperties(polygonFeatureLayer, objFactory, baseSurface);
                    layerKeyValuePair = new KeyValuePair<ILayer, LayerTypeEnum>(polygonFeatureLayer, LayerTypeEnum.PolygonFeature);
                }
            }

            Marshal.ReleaseComObject(workspaceFactory);

            return layerKeyValuePair;
        }

        private static Dictionary<LayerTypeEnum, ILayer> GetLayers(ArcSceneArguments layers, IObjectFactory objFactory)
        {
            var preparedLayers = new Dictionary<LayerTypeEnum, ILayer>();

            Type rasterLayerType = typeof(RasterLayerClass);
            string typeRasterLayerID = rasterLayerType.GUID.ToString("B");

            var elevationRasterLayer = (IRasterLayer)objFactory.Create(typeRasterLayerID);
            elevationRasterLayer.CreateFromFilePath(layers.DemLayer);
            preparedLayers.Add(LayerTypeEnum.Raster, elevationRasterLayer);

            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            string typeFactoryID = factoryType.GUID.ToString("B");

            IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)objFactory.Create(typeFactoryID);
            IWorkspace2 workspace = (IWorkspace2)workspaceFactory.OpenFromFile(_profileGdb, 0);

            if (!string.IsNullOrEmpty(layers.Line3DLayer))
            {
                preparedLayers.Add(LayerTypeEnum.LineFeature, CreateFeatureLayer(layers.Line3DLayer, workspace, objFactory));
                preparedLayers.Add(LayerTypeEnum.PointFeature, CreateFeatureLayer(layers.Point3DLayer, workspace, objFactory));

                var polygon3DLayer = CreateFeatureLayer(layers.Polygon3DLayer, workspace, objFactory);


                var polygonLayerEffects = (ILayerEffects)polygon3DLayer;
                polygonLayerEffects.Transparency = 50;

                preparedLayers.Add(LayerTypeEnum.PolygonFeature, polygon3DLayer);
            }

            Marshal.ReleaseComObject(workspaceFactory);

            return preparedLayers;
        }


        private static IFeatureLayer CreateFeatureLayer(string featureClass, IWorkspace2 workspace, IObjectFactory objFactory)
        {
            if (workspace.NameExists[esriDatasetType.esriDTFeatureClass, featureClass])
            {
                IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspace;

                var featureLayer = (IFeatureLayer)objFactory.Create("esriCarto.FeatureLayer");
                var featureClassC = featureWorkspace.OpenFeatureClass(featureClass);

                featureLayer.FeatureClass = featureClassC;
                featureLayer.Name = featureLayer.FeatureClass.AliasName;

                SetFromMapRendererToFeatureLayer(featureLayer, objFactory, featureClass);

                return featureLayer;
            }

            return null;
        }

        private static IRasterLayer CreateRasterLayer(string layerName,
                                                      IWorkspace2 workspace,
                                                      IObjectFactory objFactory,
                                                      string gdb)
        {
            if (workspace.NameExists[esriDatasetType.esriDTRasterDataset, layerName])
            {
                return CreateRasterLayer(objFactory, $"{gdb}\\{layerName}");
            }

            return null;
        }

        private static IGeoFeatureLayer CreateLayerCopy(IFeatureLayer layer, IObjectFactory objFactory)
        {
            var workspacePath = GetWorkspacePathForLayer(layer);

            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            string typeFactoryID = factoryType.GUID.ToString("B");

            IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)objFactory.Create(typeFactoryID);
            IFeatureWorkspace workspace = (IFeatureWorkspace)workspaceFactory.OpenFromFile(workspacePath, 0);

            var featureLayer = (IFeatureLayer)objFactory.Create("esriCarto.FeatureLayer");
            var featureClassC = workspace.OpenFeatureClass(layer.FeatureClass.AliasName);

            featureLayer.FeatureClass = featureClassC;
            featureLayer.Name = layer.Name;

            var arcMapLayerDefinition = layer as IFeatureLayerDefinition2;
            var layerDefinition = featureLayer as IFeatureLayerDefinition2;
            layerDefinition.DefinitionExpression = arcMapLayerDefinition.DefinitionExpression;

            IGeoFeatureLayer geoFeatureLayer = featureLayer as IGeoFeatureLayer;

            SetFromMapRendererToFeatureLayer(featureLayer, objFactory, layer.FeatureClass.AliasName);

            Marshal.ReleaseComObject(workspaceFactory);

            return geoFeatureLayer;
        }

        private static IGeoFeatureLayer PointsRender(IFeatureLayer layer, RgbColor color, IObjectFactory objFactory)
        {
            Type renderType = typeof(SimpleRendererClass);
            string typeRenderID = renderType.GUID.ToString("B");

            ISimpleRenderer renderer = (ISimpleRenderer)objFactory.Create(typeRenderID);
            renderer.Symbol = GetSymbol(esriGeometryType.esriGeometryPoint, color, objFactory);

            IGeoFeatureLayer geoFL = layer as IGeoFeatureLayer;
            geoFL.Renderer = renderer as IFeatureRenderer;

            return geoFL;
        }

        private static IGeoFeatureLayer VisibilityColorsRender(IFeatureLayer layer, IObjectFactory objFactory)
        {
            const string fieldName = "IS_VISIBLE";
            string[] uniqueValues = new string[2];

            int fieldIndex = layer.FeatureClass.Fields.FindField(fieldName);

            uniqueValues[0] = "0";
            uniqueValues[1] = "1";

            Type renderType = typeof(UniqueValueRendererClass);
            string typeRenderID = renderType.GUID.ToString("B");

            IUniqueValueRenderer uVRenderer = (IUniqueValueRenderer)objFactory.Create(typeRenderID);
            uVRenderer.FieldCount = 1;
            uVRenderer.Field[0] = fieldName;

            var invisibleSymbol = GetSymbol(layer.FeatureClass.ShapeType, new RgbColor() { Red = 255, Blue = 0, Green = 0 });
            var visibleSymbol = GetSymbol(layer.FeatureClass.ShapeType, new RgbColor() { Red = 0, Blue = 0, Green = 255 });

            uVRenderer.AddValue(uniqueValues[0], "Visibility", invisibleSymbol);
            uVRenderer.AddValue(uniqueValues[1], "Visibility", visibleSymbol);

            IGeoFeatureLayer geoFL = layer as IGeoFeatureLayer;
            geoFL.Renderer = uVRenderer as IFeatureRenderer;

            return geoFL;

        }

        private static void SetVisibilitySessionRaster3DProperties(IRasterLayer rasterLayer,
                                                                   IObjectFactory objFactory,
                                                                   IFunctionalSurface surface,
                                                                   bool isDrapperyLayer = false)
        {
            var properties3D = (I3DProperties3)objFactory.Create("esrianalyst3d.Raster3DProperties");
            properties3D.BaseOption = esriBaseOption.esriBaseSurface;
            properties3D.BaseSurface = surface;

            if (!isDrapperyLayer)
            {
                properties3D.OffsetExpressionString = "2";
                properties3D.DepthPriorityValue = 1;
            }
            else
            {
                properties3D.DepthPriorityValue = 9;
            }

            properties3D.ZFactor = _zFactor;
            properties3D.RenderVisibility = esriRenderVisibility.esriRenderAlways;
            properties3D.RenderMode = esriRenderMode.esriRenderCache;
            properties3D.TextureDownsamplingFactor = 0.7;
            properties3D.AlphaThreshold = 0.1;
            properties3D.RenderRefreshRate = 0.75;
            properties3D.Illuminate = true;

            ILayerExtensions layerExtensions = (ILayerExtensions)rasterLayer;
            layerExtensions.AddExtension(properties3D);
            properties3D.Apply3DProperties(rasterLayer);
        }


        private static void SetFeatures3DProperties(IFeatureLayer layer, IObjectFactory objFactory,
                                                    IFunctionalSurface surface, double height = double.NaN)
        {
            var properties3D = (I3DProperties)objFactory.Create("esrianalyst3d.Feature3DProperties");
            properties3D.BaseOption = esriBaseOption.esriBaseSurface;
            properties3D.BaseSurface = surface;
            properties3D.ZFactor = _zFactor;
            properties3D.OffsetExpressionString = (height == double.NaN) ? "3" : height.ToString();

            ILayerExtensions layerExtensions = (ILayerExtensions)layer;
            layerExtensions.AddExtension(properties3D);
            properties3D.Apply3DProperties(layer);
        }

        private static void SetHightFeatures3DProperties(IFeatureLayer layer, IObjectFactory objFactory)
        {
            var properties3D = (I3DProperties)objFactory.Create("esrianalyst3d.Feature3DProperties");
            properties3D.BaseOption = esriBaseOption.esriBaseShape;
            properties3D.ZFactor = _zFactor;
            properties3D.OffsetExpressionString = "3";

            ILayerExtensions layerExtensions = (ILayerExtensions)layer;
            layerExtensions.AddExtension(properties3D);
            properties3D.Apply3DProperties(layer);
        }

        private static void SetSurface3DProperties(ILayer layer, IObjectFactory objFactory, IFunctionalSurface surface)
        {
            var properties3D = (I3DProperties)objFactory.Create("esrianalyst3d.Raster3DProperties");
            properties3D.BaseOption = esriBaseOption.esriBaseSurface;
            properties3D.BaseSurface = surface;
            properties3D.ZFactor = _zFactor;
            properties3D.DepthPriorityValue = 10;

            ILayerExtensions layerExtensions = (ILayerExtensions)layer;
            layerExtensions.AddExtension(properties3D);
            properties3D.Apply3DProperties(layer);
        }

        private static ISymbol GetSymbol(esriGeometryType featureGeometryType, RgbColor color, IObjectFactory objFactory = null)
        {
            if (featureGeometryType == esriGeometryType.esriGeometryPolygon)
            {
                ISimpleFillSymbol simplePolygonSymbol = new SimpleFillSymbolClass();
                simplePolygonSymbol.Color = color;

                ISimpleLineSymbol outlineSymbol = new SimpleLineSymbolClass();
                var outLine = color;
                color.Transparency = 255;
                outlineSymbol.Color = outLine;
                outlineSymbol.Width = 1;
                simplePolygonSymbol.Outline = outlineSymbol;

                return simplePolygonSymbol as ISymbol;
            }

            if (featureGeometryType == esriGeometryType.esriGeometryPolyline)
            {
                ISimpleLineSymbol simplePolylineSymbol = new SimpleLineSymbolClass();
                simplePolylineSymbol.Color = color;
                simplePolylineSymbol.Width = 4;

                return simplePolylineSymbol as ISymbol;
            }

            if (featureGeometryType == esriGeometryType.esriGeometryPoint)
            {
                Type factoryType = Type.GetTypeFromProgID("esriDisplay.SimpleMarkerSymbol");
                string typeFactoryID = factoryType.GUID.ToString("B");

                ISimpleMarkerSymbol pointMarkerSymbol = (ISimpleMarkerSymbol)objFactory.Create(typeFactoryID);
                pointMarkerSymbol.Color = color;
                pointMarkerSymbol.Style = esriSimpleMarkerStyle.esriSMSCircle;
                pointMarkerSymbol.Size = 30;

                return pointMarkerSymbol as ISymbol;
            }

            ISimpleMarkerSymbol simpleMarkerSymbol = new SimpleMarkerSymbolClass();
            simpleMarkerSymbol.Color = color;
            return simpleMarkerSymbol as ISymbol;
        }

        private static bool OpenArcScene()
        {
            IDocument doc = null;
            try
            {
                doc = new SxDocument();
            }
            catch
            {
                return false;
            }

            if (doc != null)
            {
                m_appROTEvent = new AppROTClass();
                m_appROTEvent.AppRemoved += new IAppROTEvents_AppRemovedEventHandler(m_appROTEvent_AppRemoved);

                m_application = doc.Parent;
                m_application.Visible = true;
                m_appHWnd = m_application.hWnd;
            }
            else
            {
                m_appROTEvent = null;
                m_application = null;

                return false;
            }

            return true;
        }

        private static void SetFromMapRendererToFeatureLayer(IFeatureLayer featureLayer,
                                                             IObjectFactory objFactory,
                                                             string featureClassName)
        {
            MapLayersManager mapLayersManager = new MapLayersManager(_map);
            var layerName = mapLayersManager.GetLayerAliasByFeatureClass(featureClassName);

            if (!String.IsNullOrEmpty(layerName))
            {
                var fromMapGeoFeatureLayer = EsriTools.GetLayer(layerName, _map.FocusMap) as IGeoFeatureLayer;

                if (fromMapGeoFeatureLayer == null)
                {
                    _layersWithDefaultRenderer.Add(featureLayer.Name);
                    return;
                }

                var geoFeatureLayer = featureLayer as IGeoFeatureLayer;

                try
                {
                    Type renderType = typeof(SimpleRendererClass);
                    string typeRenderID = renderType.GUID.ToString("B");

                    var objCopy = (IObjectCopy)objFactory.Create("esriSystem.ObjectCopy");
                    var rendereCopy = objCopy.Copy(fromMapGeoFeatureLayer.Renderer) as IFeatureRenderer;
                    geoFeatureLayer.Renderer = rendereCopy;
                }
                catch (Exception ex)
                {
                    _layersWithDefaultRenderer.Add(featureLayer.Name);
                    logger.WarnEx($"Cannot set rendrer from map for {featureLayer.Name} layer. Exception: {ex.Message}");
                }
            }
        }

        private static void SetFromMapRendererToRasterLayer(IRasterLayer rasterLayer,
                                                     IObjectFactory objFactory,
                                                     string mapLayerName)
        {
            MapLayersManager layersManager = new MapLayersManager(_map);
            var fromMapRasterLayer = EsriTools.GetLayer(mapLayerName, _map.FocusMap) as IRasterLayer;

            if (fromMapRasterLayer == null)
            {
                _layersWithDefaultRenderer.Add(rasterLayer.Name);
                return;
            }

            try
            {
                Type renderType = typeof(SimpleRendererClass);
                string typeRenderID = renderType.GUID.ToString("B");

                var symbol = (ISimpleRenderer)objFactory.Create(typeRenderID);
                var objCopy = (IObjectCopy)objFactory.Create("esriSystem.ObjectCopy");

                var copyS = objCopy.Copy(fromMapRasterLayer.Renderer) as IRasterRenderer;
                rasterLayer.Renderer = copyS;
            }
            catch (Exception ex)
            {
                _layersWithDefaultRenderer.Add(rasterLayer.Name);
                logger.WarnEx($"Cannot set rendrer from map for {rasterLayer.Name} layer. Exception: {ex.Message}");
            }
        }

        private static void SetSceneView(IBasicDocument document, IRasterLayer surface)
        {
            IEnvelope unionEnvelope = new EnvelopeClass();

            foreach (var layer in _viewCalcLayers)
            {
                IEnvelope envelope = null;
              
                try
                {
                    envelope = EsriTools.GetLayerExtent(layer, document.ActiveView);
                }
                catch(Exception ex)
                {
                    logger.WarnEx($"Cannot to get envelope from {layer.Name} layer");
                }

                if (envelope != null)
                {
                    unionEnvelope.Union(envelope);
                }
            }

            var pSxDoc = document as ISxDocument;
            var camera = pSxDoc.Scene.SceneGraph.ActiveViewer.Camera;

            var centerPoint = EsriTools.GetCenterPoint(unionEnvelope);
            centerPoint.AddZCoordinate(surface.Raster);

            camera.Target = centerPoint;

            var observerPoint = unionEnvelope.LowerRight.Clone();
            observerPoint.AddZCoordinate(surface.Raster);
            observerPoint.Z += 1000;

            camera.Observer = observerPoint;
            camera.Zoom(-2);

            camera.RecalcUp();
            pSxDoc.Scene.SceneGraph.RefreshViewers();
        }

        private static void UpdateTemporaryDataStorage()
        {
            _viewCalcLayers = new List<ILayer>();
            _layersWithDefaultRenderer = new List<string>();
        }

        private static void AddDraperyLayer(string draperyLayerName, IObjectFactory objFactory,
                                            IFunctionalSurface baseSurface, IBasicDocument document)
        {
            var rasterLayer = CreateRasterLayer(objFactory, draperyLayerName);
            if (rasterLayer != null)
            {
                SetVisibilitySessionRaster3DProperties(rasterLayer, objFactory, baseSurface, true);
            }

            document.AddLayer(rasterLayer);
        }

        private static IRasterLayer CreateRasterLayer(IObjectFactory objFactory, string layerPath)
        {
            Type rasterLayerType = typeof(RasterLayerClass);
            string typeRasterLayerID = rasterLayerType.GUID.ToString("B");

            var rasterLayer = (IRasterLayer)objFactory.Create(typeRasterLayerID);
            rasterLayer.CreateFromFilePath(layerPath);

            SetFromMapRendererToRasterLayer(rasterLayer, objFactory, rasterLayer.Name);

            return rasterLayer;
        }

        #region "Handle the case when the application is shutdown by user manually"

        static void m_appROTEvent_AppRemoved(AppRef pApp)
        {
            //Application manually shuts down. Stop listening
            if (pApp.hWnd == m_appHWnd)
            {
                m_appROTEvent.AppRemoved -= new IAppROTEvents_AppRemovedEventHandler(m_appROTEvent_AppRemoved);
                m_appROTEvent = null;
                m_application = null;
                m_appHWnd = 0;
            }
        }
        #endregion
    }

}
