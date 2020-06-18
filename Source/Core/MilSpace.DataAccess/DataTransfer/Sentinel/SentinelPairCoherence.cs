﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MilSpace.DataAccess.DataTransfer.Sentinel
{
    public class SentinelPairCoherence
    {

        public int IdRow;

        public string IdSceneBase;
        public string IdScentSlave;
        public double Mean;
        public double Deviation;
        public double Min;
        public string Max;
        public DateTime Dto;
        public string Operator;
    }
}