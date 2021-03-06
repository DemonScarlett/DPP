﻿using ESRI.ArcGIS.Geometry;
using MilSpace.DataAccess.DataTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MilSpace.GeoCalculator
{
    public interface IGeoCalculatorView
    {
        void SetController(GeoCalculatorController controller);
        void AddPointsToGrid(IEnumerable<IPoint> points);
        void AddPointsToGrid(IEnumerable<GeoCalcPoint> points);
        Dictionary<int, IPoint> GetPointsList();
        void UpdateGraphics();
    }
}
