﻿using CoreSampleAnnotation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace CoreSampleAnnotation.PhotoMarkup
{
    /// <summary>
    /// Represents a region on the photo that correponds to the part of the core sample
    /// </summary>
    [Serializable]
    public class CalibratedRegionVM :ViewModel, ISerializable
    {
        private Point up;
        public Point Up {
            get {
                return up;
            }
            set {
                if (up != value) {
                    up = value;
                    RaisePropertyChanged(nameof(Up));
                }
            }
        }

        private Point bottom;
        public Point Bottom {
            get {
                return bottom;
            }
            set {
                if (bottom != value) {
                    bottom = value;
                    RaisePropertyChanged(nameof(Bottom));
                }
            }
        }

        private Point side;
        public Point Side {
            get {
                return side;
            }
            set {
                if (side != value) {
                    side = value;
                    RaisePropertyChanged(nameof(Side));
                }
            }
        }

        private bool isFocused = false;
        public bool IsFocused {
            get {
                return isFocused;
            }
            set {
                if (isFocused != value) {
                    isFocused = value;
                    RaisePropertyChanged(nameof(IsFocused));
                }
            }
        }
        


        private int order = -1;
        /// <summary>
        /// The order according to which the core sample parts are aligned between each other
        /// </summary>
        public int Order {
            get {
                return order;
            }
            set {
                if (order != value) {
                    order = value;
                    RaisePropertyChanged(nameof(Order));
                }
            }
        }

        private double length;
        /// <summary>
        /// The real (measured with ruller) length (in cm) of current core sample part
        /// </summary>
        public double Length {
            get {
                return length;
            }
            set {
                if (length != value) {
                    length = value;
                    RaisePropertyChanged(nameof(Length));
                }
            }
        }

        private ICommand moveUp;
        public ICommand MoveUp {
            get { return moveUp; }
            set {
                if (moveUp != value) {
                    moveUp = value;
                    RaisePropertyChanged(nameof(MoveUp));
                }
            }
        }
        public bool CanMoveUp {
            get { return MoveUp.CanExecute(Order); }
        }

        private ICommand moveDown;
        public ICommand MoveDown {
            get { return moveDown; }
            set {
                if (moveDown != value) {
                    moveDown = value;
                    RaisePropertyChanged(nameof(MoveDown));
                }
            }
        }
        public bool CanMoveDown
        {
            get { return MoveDown.CanExecute(Order); }

        }

        private ICommand remove;
        public ICommand RemoveCommand {
            get { return remove; }
            set {
                if (remove != value) {
                    remove = value;
                    RaisePropertyChanged(nameof(RemoveCommand));
                }
            }
        }

        public void RecalcMoveRelatedProps() {
            DelegateCommand dc = MoveDown as DelegateCommand;
            if(dc != null)
                dc.RaiseCanExecuteChanged();
            dc = MoveUp as DelegateCommand;
            if (dc != null)
                dc.RaiseCanExecuteChanged();


            RaisePropertyChanged(nameof(CanMoveDown));
            RaisePropertyChanged(nameof(CanMoveUp));
        }
        
        public CalibratedRegionVM()
        {

        }

        #region Serialization
        public CalibratedRegionVM(SerializationInfo info, StreamingContext context) {
            up = (Point)info.GetValue("Up", typeof(Point));
            bottom = (Point)info.GetValue("Bottom", typeof(Point));
            side = (Point)info.GetValue("Side", typeof(Point));
            order = info.GetInt32("Order");
            length = info.GetDouble("Length");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Up", Up);
            info.AddValue("Bottom", Bottom);
            info.AddValue("Side", Side);
            info.AddValue("Order", Order);
            info.AddValue("Length", length);
        }

        #endregion
    }
}