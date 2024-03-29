﻿namespace DotSpatial.Examples.DemoCustomLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using DotSpatial.Controls;
    using DotSpatial.Controls.Header;

    public class DemoCustomLayerPlugin : Extension
    {
        public override void Activate()
        {
            App.HeaderControl.Add(new SimpleActionItem("Add Custom Layer", ButtonClick));
            base.Activate();
        }

        public override void Deactivate()
        {
            App.HeaderControl.RemoveAll();
            base.Deactivate();
        }

        public void ButtonClick(object sender, EventArgs e)
        {
            MyCustomLayer2 lay = new MyCustomLayer2();
            lay.LegendText = "My Custom Layer";
            App.Map.Layers.Add(lay);
        }
    }
}
