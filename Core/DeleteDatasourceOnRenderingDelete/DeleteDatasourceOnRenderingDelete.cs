using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Events;
using Sitecore.Layouts;
using System;
using System.Linq;


namespace Core.DeleteDatasourceOnRenderingDelete
{
    public class ItemSavedHandler
    {
        /// <summary>
        /// Intercepts item:saved event on sitecore item save. Checks if datasource values have been removed for each sublayout of the item.
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void OnRenderingDeleteDeleteDatasource(object sender, EventArgs args)
        {
            var args1 = (SitecoreEventArgs)args;
            var itemChanges = (ItemChanges)args1.Parameters[1];


            if (itemChanges.HasFieldsChanged)
            {
                var layoutField = itemChanges.FieldChanges.OfType<FieldChange>().SingleOrDefault(f => f.FieldID == FieldIDs.FinalLayoutField);
                if (layoutField == null) return; // presentation hasn't changed

                string originalLayoutFieldValue = layoutField.OriginalValue;
                string newLayoutFieldValue = layoutField.Value;
                var originalLayoutDefinition = LayoutDefinition.Parse(originalLayoutFieldValue);
                var newLayoutDefinition = LayoutDefinition.Parse(newLayoutFieldValue);
                var originalRenderings = originalLayoutDefinition.GetDevice(Constants.Sitecore.LayoutDefinitionIDs.Default).Renderings.OfType<RenderingDefinition>().ToArray();
                var newRenderings = newLayoutDefinition.GetDevice(Constants.Sitecore.LayoutDefinitionIDs.Default).Renderings.OfType<RenderingDefinition>().ToArray();

                // find renderings in originalRenderings collection that do not exist in newRenderings
                var removedRenderings =
                  originalRenderings.Where(
                    candidateRendering => newRenderings.All(otherRendering => otherRendering.UniqueId != candidateRendering.UniqueId)).ToArray();


                foreach (var removedRendering in removedRenderings)
                {
                    var datasourceValue = removedRendering.Datasource;
                    if (string.IsNullOrEmpty(datasourceValue))
                    {
                        var property = removedRendering.DynamicProperties.SingleOrDefault(dp => dp.Name == Constants.Sitecore.DynamicPropertyNames.DataSource);
                        if (property != null)
                        {
                            datasourceValue = property.Value;
                        }
                    }
                    if (string.IsNullOrEmpty(datasourceValue))
                    {
                        return;
                    }
                    var datasourceItem = Sitecore.Context.ContentDatabase.GetItem(datasourceValue);

                    //check if item is references elsewhere
                    var itemReferers = Globals.LinkDatabase.GetItemReferrers(datasourceItem, true);
                    if (itemReferers.Any())
                    {
                        return; //item is referenced return. DO NOT DELETE.
                    }
                    datasourceItem.Delete();
                }
            }
        }
    }
}