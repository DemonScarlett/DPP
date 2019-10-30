﻿using MilSpace.DataAccess.DataTransfer;
using MilSpace.DataAccess.Facade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MilSpace.Visibility.ViewController
{
    public class VisibilitySessionsController
    {
        private IObservationPointsView _view;
        private List<VisibilitySession> _visibilitySessions = new List<VisibilitySession>();
        private static Dictionary<VisibilitySessionStateEnum, string> states = Enum.GetValues(typeof(VisibilitySessionStateEnum)).Cast<VisibilitySessionStateEnum>().ToDictionary(t => t, ts => ts.ToString());

        internal void SetView(IObservationPointsView view)
        {
            _view = view;
        }

        public IEnumerable<string> GetVisibilitySessionStateTypes()
        {
            return states.Select(t => t.Value);
        }

        public string GetStringForStateType(VisibilitySessionStateEnum type)
        {
            return states[type];
        }

        internal void UpdateVisibilitySessionsList()
        {
            _visibilitySessions = VisibilityZonesFacade.GetAllVisibilitySessions(true).ToList();
            _view.FillVisibilitySessionsList(_visibilitySessions);
        }

        internal VisibilitySession GetSession(string id)
        {
            return _visibilitySessions.FirstOrDefault(session => session.Id == id);
        }

        internal void RemoveSession(string id)
        {
            VisibilityZonesFacade.DeleteVisibilitySession(id);
            _visibilitySessions.Remove(_visibilitySessions.First(session => session.Id == id));
        }
    }
}