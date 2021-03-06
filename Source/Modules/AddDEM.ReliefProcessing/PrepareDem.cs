﻿using MilSpace.AddDem.ReliefProcessing.Exceptions;
using MilSpace.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using MilSpace.Core.DataAccess;
using MilSpace.DataAccess.DataTransfer.Sentinel;
using MilSpace.Tools.Sentinel;
using MilSpace.AddDem.ReliefProcessing.GuiData;
using System.Drawing;
using System.ComponentModel;

namespace MilSpace.AddDem.ReliefProcessing
{
    public partial class PrepareDem : Form, IPrepareDemViewSrtm, IPrepareDemViewSentinel
    {
        Logger log = Logger.GetLoggerEx("PrepareDem");
        PrepareDemControllerSrtm controllerSrtm = new PrepareDemControllerSrtm();
        PrepareDemControllerSentinel controllerSentinel = new PrepareDemControllerSentinel();
        private IEnumerable<SentinelProduct> sentinelProducts = null;
        public PrepareDem()
        {
            controllerSrtm.SetView(this);
            controllerSentinel.SetView(this);

            controllerSentinel.OnProductsDownloaded += OnProductsDownloaded;


            InitializeComponent();
            InitializeData();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (controllerSentinel.DownloadStarted && MessageBox.Show("Downloading process in progress./n Do tou realy want to close form?", "Milspace Message title", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                e.Cancel = true;
            }
        }

        private void OnProductsDownloaded(IEnumerable<SentinelProduct> products)
        {
            MessageBox.Show("Products were sucessfully downloaded.", "Milspace Message title", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ShowButtons();
        }

        private void InitializeData()
        {
            controllerSrtm.ReadConfiguration();
            controllerSentinel.ReadConfiguration();

            lstSrtmFiles.DataSourceChanged += LstSrtmFiles_DataSourceChanged;
            lstSrtmFiles.DataSource = SrtmFilesInfo;
            lstSrtmFiles.DisplayMember = "Name";

            lstSentilenProducts.DataSourceChanged += LstSentilenProducts_DataSourceChanged;
            lstSentilenProducts.DisplayMember = "Identifier";

            controllerSentinel.OnProductLoaded += OnSentinelProductLoaded;

            lstSentinelProductsToDownload.Items.Clear();

            ShowButtons();
        }

        private void LstSentilenProducts_DataSourceChanged(object sender, EventArgs e)
        {
            var curSentineltile = SelectedTile;

            if (SelectedTile.DownloadingScenes != null)
            {
                foreach (var sg in SelectedTile.DownloadingScenes)
                {
                    AddSceneToDownload(sg);
                }
            }
        }

        private void OnSentinelProductLoaded(IEnumerable<SentinelProduct> products)
        {
            var selectedIndx = lstSentilenProducts.SelectedIndex;
            lstSentinelProductProps.Items.Clear();
            lstSentinelProductsToDownload.Items.Clear();

            var ttt = products.ToArray();
            lstSentilenProducts.DataSource = ttt;
            lstSentilenProducts.DisplayMember = "Identifier";
            lstSentilenProducts.Update();
            lstSentilenProducts.Refresh();

            if (products != null && products.Any())
            {
                if (products.Count() < selectedIndx)
                {
                    selectedIndx = products.Count() - 1;
                }
                lstSentilenProducts.SelectedItem = products.First();
            }

            lstSentilenProducts_SelectedIndexChanged(lstSentilenProducts, null);

            ShowButtons();
        }

        private void FillTileSource()
        {
            lstTiles.Items.Clear();
            TilesToImport?.ToList().ForEach(t => lstTiles.Items.Add(t.ParentTile.Name));
        }

        private void LstSrtmFiles_DataSourceChanged(object sender, EventArgs e)
        {

        }
        #region IPrepareDemViewSentinel
        public string SentinelSrtorage { get => lblSentinelStorage.Text; set => lblSentinelStorage.Text = value; }

        public IEnumerable<SentinelTile> TilesToImport { get => controllerSentinel.TilesToImport; }

        public SentinelTile SelectedTile => controllerSentinel.GetTileByName(lstTiles.SelectedItem?.ToString());

        public string TileLatitude { get => txtLatitude.Text; }
        public string TileLongtitude { get => txtLongtitude.Text; }

        public IEnumerable<SentinelProduct> SentinelProducts { get => sentinelProducts; set => sentinelProducts = value; }

        public DateTime SentinelRequestDate { get => dtSentinelProductes.Value; }
        #endregion
        #region IPrepareDemViewSrtm

        public string SrtmSrtorage { get => lblSrtmStorage.Text; set => lblSrtmStorage.Text = value; }
        public IEnumerable<FileInfo> SrtmFilesInfo { get; set; } = new List<FileInfo>();

        #endregion

        private void btnImportSrtm_Click(object sender, EventArgs e)
        {
            if (controllerSrtm.CopySrtmFilesToStorage())
            {
                MessageBox.Show("The files were imported sucessfully.", "Milspace Message title", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Something went wrong. /n for more detailed infor go to the log file",
                                "Milspace Message title", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void btnSelectSrtm_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog selectFolder = new FolderBrowserDialog())
            {
                if (selectFolder.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(selectFolder.SelectedPath))
                {
                    string message = "The {0} files were found.";
                    MessageBoxIcon icon = MessageBoxIcon.Information;
                    try
                    {
                        controllerSrtm.ReadSrtmFilesFromFolder(selectFolder.SelectedPath);
                        lstSrtmFiles.Visible = true;
                        lstSrtmFiles.DataSource = SrtmFilesInfo;
                        lstSrtmFiles.Refresh();
                        lstSrtmFiles.Update();
                        message = message.InvariantFormat(SrtmFilesInfo.Count());

                        return;
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        log.ErrorEx(ex.Message);
                        message = ex.Message;
                        icon = MessageBoxIcon.Error;
                    }
                    catch (NothingToImportException ex)
                    {
                        log.ErrorEx(ex.Message);
                        message = ex.Message;
                        icon = MessageBoxIcon.Exclamation;
                    }
                    catch (Exception ex)
                    {
                        message = "Unexpected error";
                        log.ErrorEx(ex.Message);
                        icon = MessageBoxIcon.Error;
                    }
                    MessageBox.Show(message, "Mislspace Msg Cation", MessageBoxButtons.OK, icon);
                }
            }
        }


        private bool CheckDouble(TextBox textBox, char keyChar)
        {
            return (char.IsNumber(keyChar) || keyChar == (char)Keys.Back || keyChar == (char)KeyCodesEnum.Minus
                            || (keyChar == (int)KeyCodesEnum.DecimalPoint && textBox.Text.IndexOf(".") == -1));
        }

        private void txtLongtitude_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !CheckDouble(sender as TextBox, e.KeyChar);
            btnAddTileToList.Enabled = !e.Handled && controllerSentinel.GetTilesByPoint() != null;

        }

        private void txtLatitude_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !CheckDouble(sender as TextBox, e.KeyChar);
            btnAddTileToList.Enabled = !e.Handled && controllerSentinel.GetTilesByPoint() != null;
        }

        private void btnAddTileToList_Click(object sender, EventArgs e)
        {
            controllerSentinel.AddTileForImport();
            FillTileSource();
        }

        private void btnGetScenes_Click(object sender, EventArgs e)
        {
            controllerSentinel.GetScenes();
        }

        private void lstSentilenProducts_SelectedIndexChanged(object sender, EventArgs e)
        {
            var props = controllerSentinel.GetSentinelProductProperties(lstSentilenProducts.SelectedItem as SentinelProduct);

            lstSentinelProductProps.Items.Clear();

            foreach (var prop in props)
            {
                var item = new ListViewItem(prop[0]);
                item.SubItems.Add(prop[1]);
                lstSentinelProductProps.Items.Add(item);
            }

            ShowButtons();
        }

        private void ShowButtons()
        {
            bool selectedProduct = controllerSentinel.CheckProductExistanceToDownload(lstSentilenProducts.SelectedItem as SentinelProduct);
            btnGetScenes.Enabled = SelectedTile != null;
            btnAddSentinelProdToDownload.Enabled = lstSentilenProducts.SelectedItem != null && !selectedProduct;
            btnSetSentinelProdAsBase.Enabled = false;
            btnDownloadSentinelProd.Enabled = SelectedTile != null && SelectedTile.DownloadingScenes.Count() >= 2 && !controllerSentinel.DownloadStarted;
        }

        private void btnAddSentinelProdToDownload_Click(object sender, EventArgs e)
        {
            var pg = lstSentilenProducts.SelectedItem as SentinelProduct;
            if (pg != null)
            {
                var pairs = controllerSentinel.GetScenePairProduct(pg);
                lstSentinelProductsToDownload.Items.Clear();
                var pgl = controllerSentinel.AddProductsToDownload(pairs, pg);

                foreach (var p in pgl)
                {
                    AddSceneToDownload(p);
                }
                ShowButtons();
            }
        }

        private void AddSceneToDownload(SentinelProductGui sentinelProduct)
        {
            if (sentinelProduct != null)
            {
                ListViewItem item = new ListViewItem(sentinelProduct.Identifier);
                if (sentinelProduct.BaseScene)
                {
                    item.Font = new Font(item.Font, FontStyle.Bold);
                }

                lstSentinelProductsToDownload.Items.Add(item);
            }
        }

        private void btnDownloadSentinelProd_Click(object sender, EventArgs e)
        {
            controllerSentinel.DownloadProducts();
            ShowButtons();
        }

        private void FillScenesList()
        {
            var selectedTile = SelectedTile;
            if (selectedTile != null)
            {
                OnSentinelProductLoaded(selectedTile.TileScenes);
            }
        }

        private void lstTiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedTile = SelectedTile;
            if (selectedTile != null)
            {
                OnSentinelProductLoaded(selectedTile.TileScenes);
            }
        }

        private void btnSetSentinelProdAsBase_Click(object sender, EventArgs e)
        {

        }

        private void button17_Click(object sender, EventArgs e)
        {
            controllerSentinel.ProcessPreliminary();
        }
    }
}
