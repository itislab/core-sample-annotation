﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace All
{
    interface IInfoLayerElement
    {
        void ChangeCoordTransform(Transform oldTransformInv, Transform newTransform);
    }
}