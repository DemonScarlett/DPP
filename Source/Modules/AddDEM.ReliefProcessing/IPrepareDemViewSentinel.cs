﻿using MilSpace.AddDem.ReliefProcessing.GuiData;
using MilSpace.DataAccess.DataTransfer.Sentinel;
using MilSpace.Tools.Sentinel;
using System;
using System.Collections.Generic;
using System.IO;


namespace MilSpace.AddDem.ReliefProcessing
{
    public interface IPrepareDemViewSentinel
    {
        string SentinelSrtorage { get; set; }
        string TileLatitude { get; }

        string TileLongtitude { get; }

        IEnumerable<Tile> TilesToImport { get; }

        DateTime  SentinelRequestDate { get; }

        IEnumerable<SentinelProductGui> SentinelProductsToDownload { get; set; }
        IEnumerable<SentinelProduct> SentinelProducts { get; set; }

    }
}
