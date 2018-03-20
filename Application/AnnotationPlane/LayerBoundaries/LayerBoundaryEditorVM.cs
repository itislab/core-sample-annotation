﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CoreSampleAnnotation.AnnotationPlane.LayerBoundaries
{
    [Serializable]
    /// <summary>
    /// correspond to enternal boundary (not the two side boundaris of the column)
    /// </summary>    
    public class LayerBoundary: ISerializable {
    
        /// <summary>
        /// In WPF units
        /// </summary>
        public double Level { get; private set; }
    
        public ICommand DragStarted { get; set; }

        /// <summary>
        /// 0 - layer, 1 - pack, 2- pack group, etc.
        /// </summary>
        public int Rank { get; private set; }

        /// <summary>
        /// Order nuber
        /// </summary>
        public int Number { get; set; }

        private Guid id;

        public Guid ID {
            get { return id; }
        }

        public LayerBoundary(double level,  int rank) {
            Level = level;
            Rank = rank;
            id = Guid.NewGuid();
        }

        #region Serialization

        protected LayerBoundary(SerializationInfo info, StreamingContext context) {            
            Rank = info.GetInt32("Rank");
            Level = info.GetDouble("Level");
            id = Guid.NewGuid();
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Rank", Rank);
            info.AddValue("Level", Level);
        }

        #endregion
    }

    [Serializable]
    public class LayerBoundaryEditorVM : ViewModel, ILayerBoundariesVM, ISerializable
    {
        private LayerBoundary[] boundaries;

        /// <summary>
        /// Puts the ascending numeration of Number field in the objects with the same Rank field value
        /// </summary>
        /// <param name="boundaries"></param>
        /// <returns></returns>
        private static LayerBoundary[] RecalcBoundaryNumbers(LayerBoundary[] boundaries) {
            if (boundaries.Length == 0)
                return boundaries;
            int minRank = boundaries.Select(b => b.Rank).Min();
            int maxRank = boundaries.Select(b => b.Rank).Max();
            int[] numbers = Enumerable.Repeat(2,maxRank-minRank+1).ToArray();

            int N = boundaries.Length;

            LayerBoundary[] result = new LayerBoundary[N];

            
            for (int i = 0; i < N; i++)
            {
                LayerBoundary lb = boundaries[i];
                int idx = lb.Rank - minRank;
                int num = numbers[idx]++;
                lb.Number = num;
                for (int j = 0; j < idx; j++)                
                    numbers[j] = 2;                
                result[i] = lb;
            }

            return result;
        }

        /// <summary>
        /// Inner (inter layer) boundaries
        /// </summary>
        public LayerBoundary[] Boundaries {
            get {
                return boundaries;
            }
            set {
                if (boundaries != value) {
                    if (boundaries != null) {
                        foreach (LayerBoundary vm in boundaries)
                        {
                            vm.DragStarted = null;
                        }
                    }
                    boundaries = RecalcBoundaryNumbers(value.OrderBy(b => b.Level).ToArray());
                    if (value != null) {
                        foreach (LayerBoundary vm in value) {
                            vm.DragStarted = dragStart;
                        }
                    }
                    RaisePropertyChanged(nameof(Boundaries));
                }
            }
        }

        private ICommand dragStart;

        public ICommand DragStart {
            get {
                return dragStart;
            }
            set {
                if (dragStart != value) {
                    dragStart = value;
                    RaisePropertyChanged(nameof(DragStart));

                    if (boundaries != null)
                    {
                        foreach (LayerBoundary vm in boundaries)
                        {
                            vm.DragStarted = dragStart;
                        }
                    }
                }
            }
        }

        public LayerBoundaryEditorVM() {
            boundaries = new LayerBoundary[0];            
        }

        public void ChangeRank(Guid boundaryID, int rank) {
            LayerBoundary toUpdate = boundaries.Single(b => b.ID == boundaryID);
            List<LayerBoundary> newVal = new List<LayerBoundary>(boundaries.Where(b => b.ID != boundaryID));
            newVal.Add(new LayerBoundary(toUpdate.Level, rank));
            Boundaries = newVal.ToArray();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="idx">internal (inter layer) boundary index</param>
        public void RemoveBoundary(int idx) {
            Boundaries = Boundaries.Take(idx).Concat(Boundaries.Skip(idx + 1)).ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="idx">internal (inter layer) boundary index</param>
        /// <param name="level"></param>
        public void MoveBoundary(int idx, double level) {
            var copy = Boundaries.ToArray();
            copy[idx] = new LayerBoundary(level,copy[idx].Rank);
            Boundaries = copy;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="idx">internal (inter layer) boundary index used to access new created elem</param>
        /// <param name="rank"></param>
        /// <param name="level"></param>
        public void AddBoundary(int rank, double level) {
            List<LayerBoundary> l = new List<LayerBoundary>(Boundaries);
            l.Add(new LayerBoundary(level,rank));
            Boundaries = l.ToArray();
        }

        #region Serialization
        protected LayerBoundaryEditorVM(SerializationInfo info, StreamingContext context) {
            Boundaries = (LayerBoundary[])info.GetValue("Boundaries",typeof(LayerBoundary[]));

        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Boundaries",boundaries);
        }

        #endregion
    }


    public class BoundaryEditorColumnVM: ColumnVM {

        public BoundaryEditorColumnVM(ColumnVM targetColumn, ILayerBoundariesVM boundariesVM) :base(targetColumn.Heading) {
            ColumnVM = targetColumn;
            BoundariesVM = boundariesVM;            
        }
               

        public ILayerBoundariesVM BoundariesVM { get; private set; }
        public ColumnVM ColumnVM { get; private set; }
    }
}

