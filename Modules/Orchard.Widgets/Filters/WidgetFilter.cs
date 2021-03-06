﻿using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Aspects;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Mvc.Filters;
using Orchard.UI.Admin;
using Orchard.Widgets.Models;
using Orchard.Widgets.Services;

namespace Orchard.Widgets.Filters {
    public class WidgetFilter : FilterProvider, IResultFilter {
        private readonly IContentManager _contentManager;
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly IRuleManager _ruleManager;

        public WidgetFilter(IContentManager contentManager, IWorkContextAccessor workContextAccessor, IRuleManager ruleManager) {
            _contentManager = contentManager;
            _workContextAccessor = workContextAccessor;
            _ruleManager = ruleManager;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; private set; }

        public void OnResultExecuting(ResultExecutingContext filterContext) {
            // layers and widgets should only run on a full view rendering result
            var viewResult = filterContext.Result as ViewResult;
            if (viewResult == null)
                return;

            var workContext = _workContextAccessor.GetContext(filterContext);

            if (workContext == null ||
                workContext.Layout == null ||
                workContext.CurrentSite == null ||
                AdminFilter.IsApplied(filterContext.RequestContext)) {
                return;
            }

            // Once the Rule Engine is done:
            // Get Layers and filter by zone and rule
            IEnumerable<WidgetPart> widgetParts = _contentManager.Query<WidgetPart, WidgetPartRecord>().List();
            IEnumerable<LayerPart> activeLayers = _contentManager.Query<LayerPart, LayerPartRecord>().List();

            var activeLayerIds = new List<int>();
            foreach (var activeLayer in activeLayers) {
                // ignore the rule if it fails to execute
                try {
                    if (_ruleManager.Matches(activeLayer.Record.LayerRule)) {
                        activeLayerIds.Add(activeLayer.ContentItem.Id);
                    }
                }
                catch(Exception e) {
                    Logger.Warning(e, T("An error occured during layer evaluation on: {0}", activeLayer.Name).Text);
                }
            }

            // Build and add shape to zone.
            var zones = workContext.Layout.Zones;
            foreach (var widgetPart in widgetParts) {
                if (activeLayerIds.Contains(widgetPart.As<ICommonPart>().Container.ContentItem.Id)) {
                    var widgetShape = _contentManager.BuildDisplay(widgetPart);
                    zones[widgetPart.Record.Zone].Add(widgetShape, widgetPart.Record.Position);
                }
            }
        }

        public void OnResultExecuted(ResultExecutedContext filterContext) {
        }
    }
}
