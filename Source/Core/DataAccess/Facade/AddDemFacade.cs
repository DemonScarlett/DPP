﻿using MilSpace.Core;
using MilSpace.DataAccess.DataTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MilSpace.DataAccess.Facade
{
    public static class AddDemFacade
    {
        private static readonly Logger log = Logger.GetLoggerEx("AddDemFacade");

        public static IEnumerable<SrtmGrid> GetSrtmGrids()
        {
            using (var accessor = new AddDemDataAccess())
            {
                return accessor.GetSrtmGrids();
            }
        }

        public static IEnumerable<SrtmGrid> GetLoadedSrtmGrids()
        {
            using (var accessor = new AddDemDataAccess())
            {
                return accessor.GetLoadedSrtmGrids();
            }
        }

        public static IEnumerable<SrtmGrid> GetNotLoadedSrtmGrids()
        {
            using (var accessor = new AddDemDataAccess())
            {
                return accessor.GetLoadedSrtmGrids();
            }
        }
    }
}
