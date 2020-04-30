﻿using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;
using MilSpace.Core.Tools;
using MilSpace.DataAccess.DataTransfer;
using MilSpace.DataAccess.Facade;
using MilSpace.Tools;
using MilSpace.Visibility.Localization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MilSpace.Visibility.ViewController
{
    public class VisibilitySessionsController
    {
        private IObservationPointsView _view;
        private List<VisibilityTask> _calculationTasks = new List<VisibilityTask>();
        private List<VisibilityCalcResults> _visibilityResults = new List<VisibilityCalcResults>();
        private static Dictionary<VisibilityTaskStateEnum, string> _states = null; //Enum.GetValues(typeof(VisibilityTaskStateEnum)).Cast<VisibilityTaskStateEnum>().ToDictionary(t => t, ts => ts.ToString());
        private static Dictionary<VisibilityCalcTypeEnum, string> _calcTypes = null;// Enum.GetValues(typeof(VisibilityCalcTypeEnum)).Cast<VisibilityCalcTypeEnum>().ToDictionary(t => t, ts => ts.ToString());
        private IMxDocument mapDocument;
        private IMxApplication application;

        public VisibilitySessionsController(IMxDocument mapDocument, IMxApplication application)
        {
            VisibilityManager.OnGenerationStarted += UpdateVisibilitySessionsList;
            this.mapDocument = mapDocument;
            this.application = application;

            _calcTypes = LocalizationContext.Instance.CalcTypeLocalisationShort;
            _states = LocalizationContext.Instance.CalculationStates;
        }

        internal void SetView(IObservationPointsView view)
        {
            _view = view;
        }

        internal IEnumerable<string> GetVisibilitySessionStateTypes()
        {
            return _states.Select(t => t.Value);
        }

        internal IEnumerable<string> GetVisibilityCalcTypesStrings()
        {
            return _calcTypes.Select(t => t.Value);
        }

        internal string GetStringForStateType(VisibilityTaskStateEnum type)
        {
            return _states[type];
        }

        internal string GetStringForCalcType(VisibilityCalcTypeEnum type)
        {
            return _calcTypes[type];
        }

        internal string GetImgName(VisibilityCalculationResultsEnum resultType)
        {
            if (resultType == VisibilityCalculationResultsEnum.ObservationPoints || resultType == VisibilityCalculationResultsEnum.ObservationPointSingle)
            {
                return "Flag.png";
            }

            if (resultType == VisibilityCalculationResultsEnum.ObservationObjects)
            {
                return "Target.png";
            }

            if (resultType == VisibilityCalculationResultsEnum.VisibilityAreaRaster || resultType == VisibilityCalculationResultsEnum.VisibilityAreaRasterSingle
                || resultType == VisibilityCalculationResultsEnum.VisibilityAreaPolygons || resultType == VisibilityCalculationResultsEnum.VisibilityObservStationClip)
            {
                return "Dots Up.png";
            }

            return string.Empty;
        }

        internal void UpdateVisibilitySessionsList(bool isNewSessionAdded = false, string newSessionName = null)
        {
            _calculationTasks = VisibilityZonesFacade.GetAllVisibilityTasks(true).ToList();
            _view.FillVisibilitySessionsList(_calculationTasks, isNewSessionAdded, newSessionName);
        }
        internal void UpdateVisibilityResultsTree(bool isNewSessionAdded = false)
        {
            _visibilityResults = VisibilityZonesFacade.GetAllVisibilityResults(true).ToList();

            _view.FillVisibilityResultsTree(_visibilityResults);
        }

        internal VisibilityTask GetCalcTask(string id)
        {
            return _calculationTasks.FirstOrDefault(session => session.Id == id);
        }

        internal Dictionary<VisibilityCalcTypeEnum, string> GetCalcTypes() => _calcTypes;
        internal IEnumerable<VisibilityCalcResults> GetAllResults() => _visibilityResults;

        public IEnumerable<VisibilityTask> GetAllSessions() => _calculationTasks;

        internal bool RemoveSession(string id)
        {
            if (_calculationTasks.Count == 0)
            {
                UpdateVisibilitySessionsList();
            }

            var removedSession = _calculationTasks.FirstOrDefault(session => session.Id == id);

            if (removedSession == null)
            {
                return false;
            }

            var result = VisibilityZonesFacade.DeleteVisibilitySession(id);

            if (result)
            {
                _calculationTasks.Remove(removedSession);
                _view.RemoveSessionFromList(id);
            }

            return result;
        }


        internal VisibilityresultSummary GetSummaryResultById(string id)
        {
            var result = _visibilityResults.FirstOrDefault(res => res.Id == id);
            if (result != null)
            {
                return result.Summary;
            }

            return null;
        }

        internal bool RemoveResult(string id, IActiveView activeView)
        {
            var selectedResults = _visibilityResults.First(res => res.Id == id);
            var results = selectedResults.ValueableResults();
            var removingResult = true;

            if (VisibilityZonesFacade.IsResultsBelongToUser(id))
            {
                foreach (var result in results)
                {
                    if (result != selectedResults.Id && !EsriTools.RemoveDataSet(selectedResults.ReferencedGDB, result))
                    {
                        return false;
                    }
                }

                removingResult = VisibilityZonesFacade.DeleteVisibilityResults(id);

                if (removingResult)
                {
                    RemoveSession(id);
                    EsriTools.RemoveLayer(selectedResults.Name, activeView.FocusMap);
                }
            }
            else
            {
                VisibilityZonesFacade.DeleteVisibilityResultsFromUserSession(id);
            }

            if (removingResult)
            {
                _visibilityResults.Remove(selectedResults);
            }

            return removingResult;
        }

        internal void RemoveResultsFromSession(string id, bool removeLayers, IActiveView activeView)
        {
            var selectedResults = _visibilityResults.First(res => res.Id == id);

            if (removeLayers)
            {
                EsriTools.RemoveLayer(selectedResults.Name, activeView.FocusMap);
            }

            _visibilityResults.Remove(selectedResults);
        }

        internal bool ShareResults(string id)
        {
            try
            {
                var selectedResults = _visibilityResults.First(res => res.Id == id);
                VisibilityZonesFacade.UpdateVisibilityResults(selectedResults);
                selectedResults.Shared = true;
                return true;
            }
            catch
            {
                return false;
            }

        }

        internal bool AddSharedResults(IEnumerable<VisibilityCalcResults> results)
        {
            var res = true;

            foreach (var result in results)
            {
                if (!VisibilityZonesFacade.AddSharedVisibilityResultsToUserSession(result))
                {
                    res = false;
                }
                else
                {
                    _visibilityResults.Add(result);
                }
            }

            return res;
        }

        internal bool IsResultsLayerExist(string resultsId, IActiveView activeView)
        {
            try
            {
                var selectedResults = _visibilityResults.First(res => res.Id == resultsId).Name;
                var layer = EsriTools.GetLayer(selectedResults, activeView.FocusMap);

                return (layer != null);

            }
            catch { return false; }

        }

        internal void AddResultsGroupLayer(string id, IActiveView activeView)
        {
            var selectedResults = _visibilityResults.First(res => res.Id == id);

            //It can be used for adding tables
            var tbls = mapDocument.TableProperties;

            //var datasets = GdbAccess.Instance.GetDatasetsFromCalcWorkspace(selectedResults.ResultsInfo);


            //EsriTools.AddVisibilityGroupLayer(datasets, selectedResults.Name, selectedResults.Id, selectedResults.ReferencedGDB, GetLastLayer(activeView),
            //                                    true, 33, activeView);

            if(selectedResults.CalculationType == VisibilityCalcTypeEnum.BestObservationParameters)
            {
                EsriTools.AddTableToMap(tbls,
                                VisibilityTask.GetResultName(VisibilityCalculationResultsEnum.BestParametersTable,
                                                                selectedResults.Name),
                                selectedResults.ReferencedGDB, mapDocument, application);
            }
            else
            {
                ArcMapHelper.AddResultsToMapAsGroupLayer(selectedResults, activeView, null, true, 33, null);
                EsriTools.AddTableToMap(tbls, 
                                VisibilityTask.GetResultName(VisibilityCalculationResultsEnum.CoverageTable,
                                                                selectedResults.Name),
                                selectedResults.ReferencedGDB, mapDocument, application);
            }
        }

        internal void ZoomToLayer(string id, IActiveView activeView)
        {
            var resName = _visibilityResults.First(res => res.Id == id).Name;

            EsriTools.ZoomToLayer(resName, activeView);
        }

        internal bool IsResultsShared(string id)
        {
            try
            {
                return _visibilityResults.First(res => res.Id == id).Shared;
            }
            catch
            {
                return false;
            }
        }

        private string GetLastLayer(IActiveView activeView)
        {
            var map = activeView.FocusMap;
            return map.Layer[map.LayerCount - 1].Name;
        }
    }
}
