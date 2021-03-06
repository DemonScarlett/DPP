﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MilSpace.Core;
using MilSpace.DataAccess.Facade;
using MilSpace.DataAccess.DataTransfer.Sentinel;
using System.Net;
using MilSpace.Configurations;
using System.Web;
using System.Threading.Tasks;
using System.Net.Http;
using System.ComponentModel;
using MilSpace.Core.Actions.Base;
using MilSpace.Core.Actions.Interfaces;
using MilSpace.Core.Actions;
using MilSpace.Core.Actions.ActionResults;

namespace MilSpace.Tools.Sentinel
{
    public static class SentinelImportManager
    {
        public delegate void SentinelProductsDownloaded(string product);
        public static event SentinelProductsDownloaded OnProductDownloaded;
        public static event SentinelProductsDownloaded OnProductDownloadingError;

        private static Logger logger = Logger.GetLoggerEx("SentinelImportManager");
        public static Dictionary<IndexesEnum, string> IndexesDictionary = typeof(IndexesEnum).GetEnumToDictionary<IndexesEnum>();//(  Enum.GetValues(typeof(IndexesEnum)).Cast<IndexesEnum>().ToDictionary(k => k, v => v.ToString());
        public static Dictionary<ValuebaleProductEnum, string> productItemsDictionary = Enum.GetValues(typeof(ValuebaleProductEnum)).Cast<ValuebaleProductEnum>().ToDictionary(k => k, v => v.ToString().Replace("_", " ").Replace("9", "(").Replace("0", ")"));
        public static Dictionary<ValuebaleProductSummaryEnum, string> productSummaryItemsDictionary = Enum.GetValues(typeof(ValuebaleProductSummaryEnum) ).Cast<ValuebaleProductSummaryEnum>().ToDictionary(k => k, v => v.ToString().Replace("_", " ").Replace("9", "(").Replace("0", ")"));
        public static IEnumerable<SentinelProduct> ReadJsonFromFile(string fileName = @"E:\Data\S1\Tiles_S1B_UA-EXT_2044434443542054_1.json") //e:\Data\S1\Tiles_S1A_UA-EXT_2044434443542054.json
        {

            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException("Santilel Json file was not found.", fileName);
            }
            using (StreamReader r = new StreamReader(fileName))
            {
                string json = r.ReadToEnd();
                return ReadJson(json);
            }
        }
        public static IEnumerable<SentinelProduct> ReadJson(string json) //e:\Data\S1\Tiles_S1A_UA-EXT_2044434443542054.json
        {

            var items = JsonConvert.DeserializeObject<Rootobject>(json);
            var imports = new List<SentinelProduct>();

            items.products.ToList().ForEach(p =>
            {
                var import = new SentinelProduct();

                var child = p.indexes.FirstOrDefault(i => i.name == IndexesDictionary[IndexesEnum.product])?.children;

                import.Uuid = p.uuid;
                import.Id = p.id;
                import.Identifier = p.identifier;
                import.Instrument = p.instrument;
                

                if (child != null)
                {
                    import.DateTime = Helper.Convert<DateTime>(child.FirstOrDefault(c => c.name == productItemsDictionary[ValuebaleProductEnum.Sensing_start])?.value);
                    import.Footprint = child.FirstOrDefault(c => c.name == productItemsDictionary[ValuebaleProductEnum.Footprint])?.value;
                    import.JTSfootprint = child.FirstOrDefault(c => c.name == productItemsDictionary[ValuebaleProductEnum.JTS_footprint])?.value;
                    import.RelativeOrbit = Helper.Convert<int>(child.FirstOrDefault(c => c.name == productItemsDictionary[ValuebaleProductEnum.Relative_orbit_9start0])?.value);
                    import.PassDirection = child.FirstOrDefault(c => c.name == productItemsDictionary[ValuebaleProductEnum.Pass_direction])?.value;
                    import.SliceNumber= Helper.Convert<int>(child.FirstOrDefault(c => c.name == productItemsDictionary[ValuebaleProductEnum.Slice_number])?.value);
                    import.OrbitNumber = Helper.Convert<int>(child.FirstOrDefault(c => c.name == productItemsDictionary[ValuebaleProductEnum.Relative_orbit_9start0])?.value);
                }

                imports.Add(import);

            });

            return imports;
        }

        public static IEnumerable<SentinelProduct> GetProductsMetadata(SentineWeblRequestBuilder metadataRequest)
        {
            var url = metadataRequest.GetMetadataUrl;
            WebRequest myWebRequest = WebRequest.Create(url);
            logger.InfoEx($"Getting metadata: {HttpUtility.UrlDecode(url)}");

            myWebRequest.Credentials = new NetworkCredential(MilSpaceConfiguration.DemStorages.ScihubUserName, MilSpaceConfiguration.DemStorages.ScihubPassword);
            string requestContent;
            // Send the 'WebRequest' and wait for response.
            try
            {
                using (WebResponse myWebResponse = myWebRequest.GetResponse())
                {
                    using (var reader = new StreamReader(myWebResponse.GetResponseStream()))
                    {
                        requestContent = reader.ReadToEnd();
                    }

                    myWebResponse.Close();
                }

                return ReadJson(requestContent).OrderBy( s => s.DateTime) ;
            }
            catch (Exception ex)
            {
                logger.ErrorEx(ex.Message);
            }
            
            return null;
        }

        public static void DownloadProducs(IEnumerable<SentinelProduct> products, string tileFolderName)
        {
            foreach(var product in  products)
            {
                DownloadProbuct(product, tileFolderName);
            }
            
        }

        public static void DoPreProcessing()
        {
            string commandFile = @"E:\SourceCode\40copoka\DPP\Source\UnitTests\CommandLineAction.UnitTest\Output\CommandLineAction.UnitTest.exe";

            var action = new ActionParam<string>()
            {
                ParamName = ActionParamNamesCore.Action,
                Value = ActionsCore.RunCommandLine
            };

            var prm = new IActionParam[]
             {
                  action,
                    new ActionParam<string>() { ParamName = ActionParamNamesCore.PathToFile, Value = commandFile},
                    new ActionParam<string>() { ParamName = ActionParamNamesCore.DataValue, Value = string.Empty},
                    new ActionParam<ActionProcessCommandLineDelegate>() { ParamName = ActionParamNamesCore.OutputDataReceivedDelegate, Value = OnOutputCommandLine},
                    new ActionParam<ActionProcessCommandLineDelegate>() { ParamName = ActionParamNamesCore.ErrorDataReceivedDelegate, Value = OnErrorCommandLine}

             };

            var procc = new ActionProcessor(prm);
            var res = procc.Process<StringActionResult>();
        }

        public static void OnErrorCommandLine(string consoleMessage, ActironCommandLineStatesEnum state)
        {
            logger.ErrorEx(consoleMessage);
        }

        public static void OnOutputCommandLine(string consoleMessage, ActironCommandLineStatesEnum state)
        {
            logger.InfoEx(consoleMessage);
        }

        public static void SavePropertiesData(string sourceFileName, int b1, int b2,
                                              string outFileName, string path, string fileName = "gpt_Split_Orbit.properties",  string IWN = "IW1")
        {
            var extention = ".properties";
            var fullName = Path.Combine(path, fileName);
            var fileInfo = new FileInfo(fullName);

            if (!fileInfo.Extension.Equals(extention, StringComparison.InvariantCultureIgnoreCase))
            {
                fullName = Path.ChangeExtension(fileInfo.FullName, extention);
            }

            if(!File.Exists(sourceFileName))
            {
                throw new FileNotFoundException($"Source file {sourceFileName} doesn`t exist");
            }

            var outFileInfo = new FileInfo(outFileName);

            if (!Directory.Exists(outFileInfo.DirectoryName))
            {
                throw new DirectoryNotFoundException($"Directory {outFileInfo.DirectoryName} doesn`t exist");
            }

            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"Directory {path} doesn`t exist");
            }

            var text = new StringBuilder();
            text.AppendLine($"sourcefilename = {sourceFileName}");
            text.AppendLine($"IWN = {IWN}");
            text.AppendLine($"B1 = {b1}");
            text.AppendLine($"B2 = {b2}");
            text.AppendLine($"outfilename = {outFileName}");

            try
            {
                 File.WriteAllText(fullName, text.ToString());
            }
            catch(Exception ex)
            {
                logger.ErrorEx($"Saving to file {fullName} ends with exception {ex.Message}");
            }
        }

        private static void DownloadProbuct(SentinelProduct product, string tileFolderName)
        {

            using (WebClient client = new WebClient())
            {
                client.Credentials = new NetworkCredential(MilSpaceConfiguration.DemStorages.ScihubUserName, MilSpaceConfiguration.DemStorages.ScihubPassword);
                client.QueryString.Add("Id", product.Identifier);
                SentinelProductrequestBuildercs builder = new SentinelProductrequestBuildercs(product.Uuid);

                string tileFolder = Path.Combine(MilSpaceConfiguration.DemStorages.SentinelDownloadStorage, tileFolderName);
                if (!Directory.Exists(tileFolder))
                {
                    try
                    {
                        Directory.CreateDirectory(tileFolder);
                    }
                    catch (Exception ex)
                    {
                        logger.ErrorEx($"Cannot create the folder {tileFolder}");
                        logger.ErrorEx(ex.Message);
                        Client_DownloadFileCompleted(client, new AsyncCompletedEventArgs(ex, true, null));
                        return;
                    }
                }

                string fileName = Path.Combine(MilSpaceConfiguration.DemStorages.SentinelDownloadStorage, tileFolderName, product.Identifier + ".zip");
                client.DownloadFileCompleted += Client_DownloadFileCompleted;
                client.DownloadFileAsync(builder.Url, fileName);
            }
            
        }


        private static void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (sender is WebClient client)
            {
                //TODO: write message
                if (e.Error != null)
                {

                    OnProductDownloadingError?.Invoke(client.QueryString["Id"]);
                    logger.ErrorEx($"Error on download. Product {client.QueryString["Id"]} ");
                    logger.ErrorEx(e.Error.Message);
                }
                else
                {
                    OnProductDownloaded?.Invoke(client.QueryString["Id"]);
                    logger.InfoEx($"Download completed. Product {client.QueryString["Id"]}");
                }
            }
        }
    }
}
