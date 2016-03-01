﻿using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using OxyPlot.Wpf;
using SidebarDiagnostics.Framework;
using SidebarDiagnostics.Monitoring;

namespace SidebarDiagnostics.Models
{
    public class GraphModel : INotifyPropertyChanged, IDisposable
    {
        public GraphModel(Plot plot)
        {
            _plot = plot;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _monitorItems = null;
                    _monitor = null;

                    _hardwareItems = null;
                    _hardware = null;

                    _metricItems = null;

                    if (_metrics != null)
                    {
                        _metrics.CollectionChanged -= Metrics_CollectionChanged;

                        foreach (iMetric _metric in _metrics)
                        {
                            _metric.PropertyChanged -= Metric_PropertyChanged;
                        }

                        _metrics = null;
                    }

                    _plot = null;
                    _data = null;
                }

                _disposed = true;
            }
        }

        ~GraphModel()
        {
            Dispose(false);
        }

        public void BindData(MonitorManager manager)
        {
            BindMonitors(manager.MonitorPanels);

            ExpandConfig = true;
        }

        public void SetupPlot()
        {
            _data = new Dictionary<iMetric, ObservableCollection<MetricRecord>>();

            _plot.Series.Clear();

            foreach (iMetric _metric in Metrics)
            {
                ObservableCollection<MetricRecord> _records = new ObservableCollection<MetricRecord>();

                _data.Add(_metric, _records);
                
                _metric.PropertyChanged += Metric_PropertyChanged;

                _plot.Series.Add(
                    new LineSeries()
                    {
                        Title = _metric.Label,
                        TrackerFormatString = string.Format("{0}\r\n{{Value:#,##0.##}}{1}\r\n{{Recorded:T}}", _metric.Label, _metric.Append),
                        ItemsSource = _records,
                        DataFieldX = "Recorded",
                        DataFieldY = "Value"
                    });
            }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void BindMonitors(MonitorPanel[] panels)
        {
            MonitorItems = panels;

            if (panels.Length > 0)
            {
                Monitor = panels[0];
            }
            else
            {
                Monitor = null;
            }
        }

        private void BindHardware(iMonitor[] monitors)
        {
            HardwareItems = monitors;

            if (monitors.Length > 0)
            {
                Hardware = monitors[0];
            }
            else
            {
                Hardware = null;
            }
        }

        private void BindMetrics(iMetric[] metrics)
        {
            MetricItems = metrics;
            Metrics = new ObservableCollection<iMetric>();
        }

        private void Metrics_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (iMetric _metric in e.OldItems)
                {
                    _metric.PropertyChanged -= Metric_PropertyChanged;
                }
            }

            SetupPlot();
        }

        private void Metric_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_disposed)
            {
                (sender as iMetric).PropertyChanged -= Metric_PropertyChanged;
                return;
            }

            if (e.PropertyName != "Value")
            {
                return;
            }

            iMetric _metric = (iMetric)sender;

            if (_data == null || !_data.ContainsKey(_metric))
            {
                _metric.PropertyChanged -= Metric_PropertyChanged;
                return;
            }

            DateTime _now = DateTime.Now;

            try
            {
                foreach (MetricRecord _record in _data[_metric].Where(r => (_now - r.Recorded).TotalSeconds > Duration).ToArray())
                {
                    _data[_metric].Remove(_record);
                }

                _data[_metric].Add(new MetricRecord(_metric.Value, _now));
            }
            catch
            {
                _metric.PropertyChanged -= Metric_PropertyChanged;
            }
        }

        private string _title { get; set; } = Resources.Graph;

        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;

                NotifyPropertyChanged("Title");
            }
        }

        private MonitorPanel[] _monitorItems { get; set; }

        public MonitorPanel[] MonitorItems
        {
            get
            {
                return _monitorItems;
            }
            set
            {
                _monitorItems = value;

                NotifyPropertyChanged("MonitorItems");
            }
        }

        private MonitorPanel _monitor { get; set; }

        public MonitorPanel Monitor
        {
            get
            {
                return _monitor;
            }
            set
            {
                _monitor = value;

                if (_monitor == null)
                {
                    BindHardware(new iMonitor[0]);
                }
                else
                {
                    BindHardware(_monitor.Monitors);
                }

                NotifyPropertyChanged("Monitor");
            }
        }

        private iMonitor[] _hardwareItems { get; set; }

        public iMonitor[] HardwareItems
        {
            get
            {
                return _hardwareItems;
            }
            set
            {
                _hardwareItems = value;

                NotifyPropertyChanged("HardwareItems");
            }
        }

        private iMonitor _hardware { get; set; }

        public iMonitor Hardware
        {
            get
            {
                return _hardware;
            }
            set
            {
                _hardware = value;

                if (_hardware == null)
                {
                    BindMetrics(new iMetric[0]);

                    Title = Resources.Graph;
                }
                else
                {
                    BindMetrics(_hardware.Metrics);

                    Title = string.Format("{0} - {1}", Resources.Graph, _hardware.Name);
                }

                NotifyPropertyChanged("Hardware");
            }
        }

        private iMetric[] _metricItems { get; set; }

        public iMetric[] MetricItems
        {
            get
            {
                return _metricItems;
            }
            set
            {
                _metricItems = value;

                NotifyPropertyChanged("MetricItems");
            }
        }

        private ObservableCollection<iMetric> _metrics { get; set; }

        public ObservableCollection<iMetric> Metrics
        {
            get
            {
                return _metrics;
            }
            set
            {
                if (_metrics != null)
                {
                    foreach (iMetric _metric in _metrics)
                    {
                        _metric.PropertyChanged -= Metric_PropertyChanged;
                    }
                }

                _metrics = value;

                if (_metrics != null)
                {
                    SetupPlot();

                    _metrics.CollectionChanged += Metrics_CollectionChanged;
                }

                NotifyPropertyChanged("Metrics");
            }
        }

        public DurationItem[] DurationItems
        {
            get
            {
                return new DurationItem[5]
                {
                    new DurationItem(15, "15 Seconds"),
                    new DurationItem(30, "30 Seconds"),
                    new DurationItem(60, "1 Minute"),
                    new DurationItem(300, "5 Minutes"),
                    new DurationItem(900, "15 Minutes")
                };
            }
        }

        private int _duration { get; set; } = 15;

        public int Duration
        {
            get
            {
                return _duration;
            }
            set
            {
                _duration = value;

                NotifyPropertyChanged("Duration");
            }
        }

        private bool _expandConfig { get; set; } = true;

        public bool ExpandConfig
        {
            get
            {
                return _expandConfig;
            }
            set
            {
                _expandConfig = value;

                NotifyPropertyChanged("ExpandConfig");
            }
        }

        private Plot _plot { get; set; }

        private Dictionary<iMetric, ObservableCollection<MetricRecord>> _data { get; set; }

        private bool _disposed { get; set; } = false;
    }

    public class DurationItem
    {
        public DurationItem(int seconds, string text)
        {
            Seconds = seconds;
            Text = text;
        }

        public int Seconds { get; set; }

        public string Text { get; set; }
    }

    public class MetricRecord
    {
        public MetricRecord(double value, DateTime recorded)
        {
            Value = value > 0 ? value : 0.001d;
            Recorded = recorded;
        }

        public double Value { get; set; }

        public DateTime Recorded { get; set; }
    }
}