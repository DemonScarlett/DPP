﻿using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;
using MilSpace.Core;
using MilSpace.Core.Tools;
using MilSpace.DataAccess.DataTransfer;
using MilSpace.DataAccess.Facade;
using MilSpace.Visualization3D.Models;
using MilSpace.Visualization3D.ReferenceData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace MilSpace.Visualization3D
{
    internal partial class ProfilesVisualizationForm : Form
    {
        private ProfilesTreeView profilesTreeView;
        private LocalizationContext context;
        private List<Models.TreeViewNodeModel> profilesModels = new List<Models.TreeViewNodeModel>();

        internal ProfilesVisualizationForm()
        {
            InitializeComponent();
            LocalizeComponent();
            SubscribeForArcMapEvents();
            OnDocumentOpenFillDropdowns();
        }

        #region Private methods
        private void SubscribeForArcMapEvents()
        {
            ArcMap.Events.OpenDocument += OnDocumentOpenFillDropdowns;
        }

        private void LocalizeComponent()
        {
            try
            {
                context = new LocalizationContext();

                //Captions
                this.Text = context.WindowCaption;

                //Labels
                this.SurfaceLabel.Text = context.SurfaceLabel;
                this.lbl3DProfiles.Text = context.ArcSceneParamsLabel;
                this.lblProfiles.Text = context.ProfilesLabel;
                this.BuildingsHightLabel.Text = context.HightLablel;
                this.HydroHightLabel.Text = context.HightLablel;
                this.PlantsHightLablel.Text = context.HightLablel;
                this.TransportHightLabel.Text = context.HightLablel;
                this.BuildingsLayerLabel.Text = context.BuildingsLayerLabel;
                this.HydroLayerLabel.Text = context.HydroLayerLabel;
                this.PlantsLayerLabel.Text = context.PlantsLayerLabel;
                this.TransportLayerLabel.Text = context.TransportLayerLabel;
                this.ProfilesTabPage.Text = context.Generate3DSceneTabHeader;
                this.GenerateImageTab.Text = context.GenerateImageTabHeader;

                this.surfaceLabels.Text = context.SurfacessLabel;


                //Buttons
                this.GenerateButton.Text = context.GenerateButton;
            }
            catch { MessageBox.Show("No Localization.xml found or there is an error during loading. Coordinates Converter window is not fully localized."); }
        }

        private void OnDocumentOpenFillDropdowns()
        {
            this.SurfaceComboBox.Items.Clear();
            this.TransportLayerComboBox.Items.Clear();
            this.HydroLayerComboBox.Items.Clear();
            this.BuildingsLayerComboBox.Items.Clear();
            this.PlantsLayerComboBox.Items.Clear();

            var mapLayerManager = new MapLayersManager(ArcMap.Document.ActiveView);


            PopulateComboBox(SurfaceComboBox, mapLayerManager.RasterLayers);
            PopulateComboBox(TransportLayerComboBox, mapLayerManager.RasterLayers);
            PopulateComboBox(HydroLayerComboBox, mapLayerManager.PolygonLayers);
            PopulateComboBox(BuildingsLayerComboBox, mapLayerManager.PolygonLayers);
            PopulateComboBox(PlantsLayerComboBox, mapLayerManager.PolygonLayers);
        }

        private static void PopulateComboBox(ComboBox comboBox, IEnumerable<ILayer> layers)
        {
            comboBox.Items.AddRange(layers.Select(l => l.Name).ToArray());
        }
        #endregion

        #region Control Event Handlers
        private void ProfilesVisualizationForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Visible = false;
        }

        private void ToolBars_ButtonClick(object sender, ToolBarButtonClickEventArgs e)
        {
            if (AddProfile.Equals(e.Button))
            {
                profilesTreeView = new ProfilesTreeView(context);
                profilesTreeView.LoadProfiles();              

                var dialogResult = profilesTreeView.ShowDialog(this);

                if (dialogResult == DialogResult.OK)
                {
                    profilesModels.AddRange(profilesTreeView.SelectedTreeViewNodes.Where(item => !profilesModels.Contains(item)));

                    ProfilesListBox.DataSource = profilesModels;
                    ProfilesListBox.DisplayMember = "Name";
                    ProfilesListBox.ValueMember = "NodeProfileSession";                    

                    profilesTreeView.Dispose();
                }
            }
            else if (RemoveProfile.Equals(e.Button))
            {
                foreach(var item in ProfilesListBox.SelectedItems)
                {
                    profilesModels.Remove(item as Models.TreeViewNodeModel);
                }
                //TODO: Remove SPIKE with DataBindings reassigning
                ProfilesListBox.DataSource = null;
                ProfilesListBox.DataSource = profilesModels;
                ProfilesListBox.DisplayMember = "Name";
                ProfilesListBox.ValueMember = "NodeProfileSession";
            }            
        }

        private void GenerateButton_Click(object sender, EventArgs e)
        {
            try
            {
                if(SurfaceComboBox.SelectedItem != null)
                {
                    var profilesSets = new List<ProfileSession>();

                    foreach(var profileSetModel in profilesModels)
                    {
                        var profilesSet = profileSetModel.NodeProfileSession;
                        profilesSet.ConvertLinesToEsriPolypile(ArcMap.Document.FocusMap.SpatialReference);

                        profilesSets.Add(profilesSet);
                    }

                    var arcSceneArguments = Feature3DManager.Get3DFeatures(SurfaceComboBox.SelectedItem.ToString(), profilesSets);
                    if(!String.IsNullOrEmpty(tbCoef.Text)) arcSceneArguments.ZFactor = Convert.ToDouble(tbCoef.Text);
                    Visualization3DHandler.OpenProfilesSetIn3D(arcSceneArguments);
                }
                else
                {
                    //TODO: Exception message
                }
            }
            catch(Exception ex) {
                //TODO: Log Error

            }
        }

        private void ProfilesVisualizationForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Visualization3DHandler.ClosingHandler();
        }

        protected override void OnLoad(EventArgs e)
        {
            Helper.SetConfiguration();
        }

        private void TbCoef_TextChanged(object sender, EventArgs e)
        {
            if(!Double.TryParse(tbCoef.Text, out double res) && tbCoef.Text != string.Empty)
            {
                tbCoef.Text = string.Empty;
                MessageBox.Show("Enter decimal number");
            }
        }



        #endregion

        private void lbl3DProfiles_Click(object sender, EventArgs e)
        {

        }
    }
}
