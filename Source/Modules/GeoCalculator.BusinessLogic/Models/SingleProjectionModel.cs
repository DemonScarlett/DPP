﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MilSpace.GeoCalculator.BusinessLogic.Models
{
    public class CoordinateSystemModel
    {
        private CoordinateSystemModel() { }

        public CoordinateSystemModel(int esriWellKnownID, double falseOriginX, double falseOriginY, string name, double units = 1000)
        {
            ESRIWellKnownID = esriWellKnownID;
            FalseOriginX = falseOriginX;
            FalseOriginY = falseOriginY;
            Units = units;
            Name = name;
        }

        public int ESRIWellKnownID { get; private set; }
        public double FalseOriginX { get; private set; }
        public double FalseOriginY { get; private set; }
        public double Units { get; private set; }
        public string Name { get; private set; }
    }
}
