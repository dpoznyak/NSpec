﻿using System.Collections.Generic;
using Orchard.Widgets.Models;

namespace Orchard.Widgets.Services {
    public interface IWidgetsService : IDependency {
        
        IEnumerable<string> GetZones();

        IEnumerable<LayerPart> GetLayers();

        IEnumerable<string> GetWidgetTypes();
        IEnumerable<WidgetPart> GetWidgets();
        IEnumerable<WidgetPart> GetWidgets(int layerId);

        WidgetPart GetWidget(int widgetId);
        WidgetPart CreateWidget(int layerId, string widgetType, string title, string position, string zone);
        void UpdateWidget(int widgetId, string title, string position, string zone);
        void DeleteWidget(int widgetId);

        LayerPart GetLayer(int layerId);
        LayerPart CreateLayer(string name, string description, string layerRule);
        void UpdateLayer(int layerId, string name, string description, string layerRule);
        void DeleteLayer(int layerId);

        bool MoveWidgetUp(int widgetId);
        bool MoveWidgetUp(WidgetPart widgetPart);
        bool MoveWidgetDown(int widgetId);
        bool MoveWidgetDown(WidgetPart widgetPart);
    }
}