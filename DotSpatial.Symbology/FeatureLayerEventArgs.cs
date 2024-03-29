// ********************************************************************************************************
// Product Name: DotSpatial.Symbology.dll
// Description:  Contains the business logic for symbology layers and symbol categories.
// ********************************************************************************************************
// The contents of this file are subject to the MIT License (MIT)
// you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://dotspatial.codeplex.com/license
//
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF
// ANY KIND, either expressed or implied. See the License for the specific language governing rights and
// limitations under the License.
//
// The Original Code is from MapWindow.dll version 6.0
//
// The Initial Developer of this Original Code is Ted Dunsford. Created 5/18/2009 1:28:27 PM
//
// Contributor(s): (Open source contributors should list themselves and their modifications here).
//
// ********************************************************************************************************

using System;

namespace DotSpatial.Symbology
{
    /// <summary>
    /// FeatureLayerEventArgs
    /// </summary>
    public class FeatureLayerEventArgs : EventArgs
    {
        #region Private Variables

        private IFeatureLayer _featureLayer;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of FeatureLayerEventArgs
        /// </summary>
        public FeatureLayerEventArgs(IFeatureLayer featureLayer)
        {
            _featureLayer = featureLayer;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the feature layer for this event
        /// </summary>
        public IFeatureLayer FeatureLayer
        {
            get { return _featureLayer; }
            protected set { _featureLayer = value; }
        }

        #endregion
    }
}