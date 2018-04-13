﻿using System;
using System.Drawing;
using System.Linq;
using LiveCharts.Core.Abstractions;
using LiveCharts.Core.Abstractions.DataSeries;
using LiveCharts.Core.Charts;
using LiveCharts.Core.Coordinates;
using LiveCharts.Core.Interaction;
using LiveCharts.Core.Updater;
using LiveCharts.Core.ViewModels;

namespace LiveCharts.Core.DataSeries
{
    /// <summary>
    /// The Pie series class.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <seealso cref="LiveCharts.Core.DataSeries.Series{TModel, PieCoordinate, PieViewModel, TPoint}" />
    /// <seealso cref="LiveCharts.Core.Abstractions.DataSeries.IPieSeries" />
    public class PieSeries<TModel> :
        Series<TModel, StackedPointCoordinate, PieViewModel, Point<TModel, StackedPointCoordinate, PieViewModel>>, IPieSeries
    {
        private static ISeriesViewProvider<TModel, StackedPointCoordinate, PieViewModel> _provider;
        private double _pushOut;
        private double _cornerRadius;

        /// <summary>
        /// Initializes a new instance of the <see cref="PieSeries{TModel}"/> class.
        /// </summary>
        public PieSeries()
        {
            Charting.BuildFromSettings<IPieSeries>(this);
        }

        /// <inheritdoc />
        public double PushOut
        {
            get => _pushOut;
            set
            {
                _pushOut = value; 
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        public double CornerRadius
        {
            get => _cornerRadius;
            set
            {
                _cornerRadius = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        public override Type ResourceKey => typeof(IPieSeries);

        /// <inheritdoc />
        public override float[] DefaultPointWidth => new[] {0f, 0f};

        /// <inheritdoc />
        public override float[] PointMargin => new[] {0f, 0f};

        /// <inheritdoc />
        protected override ISeriesViewProvider<TModel, StackedPointCoordinate, PieViewModel>
            DefaultViewProvider => _provider ?? (_provider = Charting.Current.UiProvider.PieViewProvider<TModel>());

        /// <inheritdoc />
        public override void UpdateView(ChartModel chart, UpdateContext context)
        {
            var pieChart = (IPieChartView) chart.View;
            
            var maxPushOut = context.GetMaxPushOut();

            var innerRadius = pieChart.InnerRadius;
            var outerDiameter = pieChart.ControlSize[0] < pieChart.ControlSize[1]
                ? pieChart.ControlSize[0]
                : pieChart.ControlSize[1];

            outerDiameter -= (float) maxPushOut *2f;

            var centerPoint = new PointF(pieChart.ControlSize[0] / 2f, pieChart.ControlSize[1] / 2f);

            var startsAt = pieChart.StartingRotationAngle > 360f
                ? 360f
                : (pieChart.StartingRotationAngle < 0
                    ? 0f
                    : (float) pieChart.StartingRotationAngle);

            Point<TModel, StackedPointCoordinate, PieViewModel> previous = null;

            foreach (var current in Points)
            {
                var range = current.Coordinate.To - current.Coordinate.From;

                float stacked;

                unchecked
                {
                    stacked = context.GetStack(-1, (int) current.Coordinate.Key, true);
                }

                var vm = new PieViewModel
                {
                    To = new SliceViewModel
                    {
                        Wedge = range * 360f / stacked,
                        InnerRadius = (float) innerRadius,
                        OuterRadius = outerDiameter / 2,
                        Rotation = startsAt + current.Coordinate.From * 360f / stacked
                    },
                    ChartCenter = centerPoint
                };

                if (current.View == null)
                {
                    current.View = ViewProvider.Getter();
                }

                current.InteractionArea = new PolarInteractionArea(
                    vm.To.OuterRadius, vm.To.Rotation, vm.To.Wedge, centerPoint);
                current.ViewModel = vm;
                current.View.DrawShape(current, previous);
                Mapper.EvaluateModelDependentActions(current.Model, current.View.VisualElement, current);

                previous = current;
            }
        }
    }
}