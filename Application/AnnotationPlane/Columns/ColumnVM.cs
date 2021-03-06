﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace CoreSampleAnnotation.AnnotationPlane
{
    public abstract class ColumnVM : ViewModel
    {
        private string heading = "Заголовок";
        public string Heading
        {
            get
            {
                return heading;
            }
            set
            {
                if (heading != value)
                {
                    heading = value;
                    RaisePropertyChanged(nameof(Heading));
                }
            }
        }

        private double height = 0.0;
        /// <summary>
        /// In WPF units
        /// </summary>
        public double ColumnHeight
        {
            get
            {
                return height;
            }
            set
            {
                if (height != value)
                {
                    height = value;
                    RaisePropertyChanged(nameof(ColumnHeight));
                }
            }
        }

        private double upperBound = 0;
        /// <summary>
        /// Units: meters (positive value)
        /// </summary>
        public double UpperBound
        {
            get
            {
                return upperBound;
            }
            set
            {
                if (upperBound != value)
                {
                    upperBound = value;
                    RaisePropertyChanged(nameof(UpperBound));
                }
            }
        }

        private double lowerBound = 0;
        /// <summary>
        /// Units: meters (positive value)
        /// </summary>
        public double LowerBound
        {
            get
            {
                return lowerBound;
            }
            set
            {
                if (lowerBound != value)
                {
                    lowerBound = value;
                    RaisePropertyChanged(nameof(LowerBound));
                }
            }
        }

        /// <summary>
        /// How many WPF units in 1 real meter
        /// </summary>
        public double CurrentScaleFactor {
            get {
                return ColumnHeight / (lowerBound - upperBound);
            }
        }

        public ColumnVM(string heading)
        {
            this.heading = heading;
        }

        protected double WPFtoRealDepth(double wpfDepth) {
            double scaleFactor = (lowerBound - upperBound) / ColumnHeight;
            return wpfDepth * scaleFactor + upperBound;
        }

        /// <param name="depth">in meters</param>
        /// <returns></returns>
        protected double RealDepthToWPF(double depth) {
            double scaleFactor = (lowerBound - upperBound) / ColumnHeight;
            return (depth - upperBound) / scaleFactor;
        }
    }

    public class DepthAxisColumnVM : ColumnVM
    {

        public InteractiveDataDisplay.WPF.Range Range
        {
            get
            {
                return new InteractiveDataDisplay.WPF.Range(-LowerBound, -UpperBound);
            }
        }

        public DepthAxisColumnVM(string heading) : base(heading)
        {
            this.PropertyChanged += DepthAxisColumnVM_PropertyChanged;
        }

        private void DepthAxisColumnVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if ((e.PropertyName == nameof(UpperBound)) || (e.PropertyName == nameof(LowerBound)))
            {
                DepthAxisColumnVM dacVM = (DepthAxisColumnVM)sender;
                dacVM.RaisePropertyChanged(nameof(Range));
            }
        }
    }

    public class LayeredColumnVM : ColumnVM, ILayerColumn
    {
        private ObservableCollection<LayerVM> layers = new ObservableCollection<LayerVM>();

        public ObservableCollection<LayerVM> Layers
        {
            get
            {
                return layers;
            }
            set
            {
                if (layers != value)
                {
                    layers = value;
                    RaisePropertyChanged(nameof(Layers));
                }
            }
        }

        public LayeredColumnVM(string heading) : base(heading) { }
    }

    /// <summary>
    /// Wraps LayeredColumnVM, presenting each LayerVM as adapter wrapped
    /// </summary>
    public class LayeredPresentationColumnVM : ColumnVM, ILayerColumn
    {
        private LayeredColumnVM target;
        private Func<LayerVM, LayerVM> adapter;
        public LayeredPresentationColumnVM(string heading, LayeredColumnVM target, Func<LayerVM, LayerVM> adapter) : base(heading)
        {
            this.target = target;
            this.adapter = adapter;
            target.Layers.CollectionChanged += Layers_CollectionChanged;
            target.PropertyChanged += Target_PropertyChanged;
            foreach (LayerVM vm in target.Layers)
                layers.Add(adapter(vm));
        }

        private void Target_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(target.LowerBound):
                    LowerBound = target.LowerBound;
                    break;
                case nameof(target.UpperBound):
                    UpperBound = target.UpperBound;
                    break;
                case nameof(target.ColumnHeight):
                    ColumnHeight = target.ColumnHeight;
                    break;
            }
        }

        private void Layers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        LayerVM addedVM = (LayerVM)e.NewItems[i];
                        LayerVM craftedVM = adapter(addedVM);
                        if (e.NewItems.Count == 0)
                            Layers.Add(craftedVM);
                        else
                            Layers.Insert(e.NewStartingIndex + i, craftedVM);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    if (e.OldItems.Count != 1)
                        throw new InvalidOperationException();
                    Layers.RemoveAt(e.OldStartingIndex);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private ObservableCollection<LayerVM> layers = new ObservableCollection<LayerVM>();

        public ObservableCollection<LayerVM> Layers
        {
            get
            {
                return layers;
            }
        }
    }

    public class ImageColumnVM : ColumnVM
    {
        public ImageColumnVM(string heading) : base(heading)
        {

        }

        /// <summary>
        /// The wpf width of the widest image among <seealso cref="ImageRegions"/>
        /// </summary>
        public double MaxImageWidth
        {
            get
            {
                if (ImageRegions != null)
                {
                    var regions = ImageRegions.ToArray();
                    if (regions.Length > 0)
                    {
                        return regions.Max(r => r.WpfWidth);
                    }
                    else
                        return 0;
                }
                else
                    return 0;
            }
        }

        private IEnumerable<Intervals.PhotoRegion> imageRegions = null;
        public IEnumerable<Intervals.PhotoRegion> ImageRegions
        {
            get
            {
                return imageRegions;
            }
            set
            {
                if (imageRegions != value)
                {
                    if (imageRegions != null)
                    {
                        foreach (Intervals.PhotoRegion region in imageRegions)
                        {
                            region.PropertyChanged -= Region_PropertyChanged;
                        }
                    }

                    imageRegions = value;
                    if (imageRegions != null)
                    {
                        foreach (Intervals.PhotoRegion region in imageRegions)
                        {
                            region.PropertyChanged += Region_PropertyChanged;
                        }
                    }
                    RaisePropertyChanged(nameof(ImageRegions));
                    RaisePropertyChanged(nameof(MaxImageWidth));
                }
            }
        }

        private void Region_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Intervals.PhotoRegion region = sender as Intervals.PhotoRegion;
            switch (e.PropertyName)
            {
                case nameof(region.WpfWidth):
                    RaisePropertyChanged(nameof(MaxImageWidth));
                    break;
            }
        }
    }

    /// <summary>
    /// This is a spectial case of LayeredColumnVM which maintains the up-to-date RealLength property in every LengthLayerVM among layers
    /// </summary>
    public class LayerRealLengthColumnVM : LayeredColumnVM
    {
        public LayerRealLengthColumnVM(string heading) : base(heading)
        {
            this.PropertyChanged += LayerRealSizeColumnVM_PropertyChanged;
            Layers.CollectionChanged += Layers_CollectionChanged;
            foreach (LayerVM vm in Layers)
            {
                vm.PropertyChanged += LayerVm_PropertyChanged;
            }
        }

        private void LayerVm_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LayerVM.Length))
            {
                UpdateRealLength();
            }
        }

        private void Layers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (LayerVM vm in e.OldItems)
                {
                    vm.PropertyChanged -= LayerVm_PropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (LayerVM vm in e.NewItems)
                {
                    vm.PropertyChanged += LayerVm_PropertyChanged;
                }
            }
            UpdateRealLength();
        }

        private void LayerRealSizeColumnVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(UpperBound):
                case nameof(LowerBound):
                case nameof(ColumnHeight):
                    UpdateRealLength();
                    break;
            }
        }

        private void UpdateRealLength()
        {
            double realToWpfFactor = (LowerBound - UpperBound) / ColumnHeight;
            for (int i = 0; i < Layers.Count; i++)
            {
                LengthLayerVM llvm = Layers[i] as LengthLayerVM;
                if (llvm != null)
                {
                    llvm.RealLength = llvm.Length * realToWpfFactor;
                }
            }
        }


    }
}
