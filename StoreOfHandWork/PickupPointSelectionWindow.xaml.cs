using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using StoreOfHandWork.Models;
using StoreOfHandWork.Data;
using System.Linq;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using Microsoft.EntityFrameworkCore;

namespace StoreOfHandWork
{
    public partial class PickupPointSelectionWindow : Window
    {
        private readonly ApplicationDbContext _context;
        public PickupPoint SelectedPickupPoint { get; private set; }
        private List<PickupPoint> _pickupPoints;
        private Dictionary<GMapMarker, PickupPoint> _markers;

        public PickupPointSelectionWindow()
        {
            InitializeComponent();
            _context = new ApplicationDbContext();
            _markers = new Dictionary<GMapMarker, PickupPoint>();
            
            InitializeMap();
            LoadPickupPoints();
        }

        private void InitializeMap()
        {
            try
            {
                // Настройка провайдера карт (OpenStreetMap)
                GMaps.Instance.Mode = AccessMode.ServerAndCache;
                MapControl.MapProvider = OpenStreetMapProvider.Instance;
                
                // Центр Ульяновска (смещаем чуть севернее, чтобы охватить оба берега)
                MapControl.Position = new PointLatLng(54.32, 48.4);
                MapControl.MinZoom = 10;
                MapControl.MaxZoom = 18;
                MapControl.Zoom = 11; // Уменьшаем зум для отображения обоих берегов

                // Включаем элементы управления
                MapControl.ShowCenter = false;
                MapControl.DragButton = System.Windows.Input.MouseButton.Left;
                MapControl.MouseWheelZoomEnabled = true;
                MapControl.CanDragMap = true;

                // Устанавливаем ограничения для области просмотра (расширяем для Заволжья)
                MapControl.SetPositionByKeywords("Ульяновск");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации карты: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPickupPoints()
        {
            try 
            {
                // Загружаем существующие пункты выдачи
                _pickupPoints = _context.PickupPoints.ToList();

                // Если пунктов выдачи нет, создаем их
                if (!_pickupPoints.Any())
                {
                    _pickupPoints = new List<PickupPoint>
                    {
                        // Правый берег
                        new PickupPoint 
                        { 
                            Address = "ул. Гончарова, 27, Ульяновск",
                            WorkingHours = "Пн-Вс: 10:00-21:00",
                            Phone = "+7 (8422) 42-42-42",
                            Latitude = 54.31724,
                            Longitude = 48.39876
                        },
                        new PickupPoint 
                        { 
                            Address = "ул. Минаева, 11, Ульяновск",
                            WorkingHours = "Пн-Вс: 09:00-21:00",
                            Phone = "+7 (8422) 43-43-43",
                            Latitude = 54.31089,
                            Longitude = 48.39462
                        },
                        new PickupPoint 
                        { 
                            Address = "пр-т Нариманова, 75, Ульяновск",
                            WorkingHours = "Пн-Вс: 10:00-19:00",
                            Phone = "+7 (8422) 44-44-44",
                            Latitude = 54.33256,
                            Longitude = 48.38569
                        },
                        new PickupPoint 
                        { 
                            Address = "ул. Карла Маркса, 13, Ульяновск",
                            WorkingHours = "Пн-Вс: 09:00-20:00",
                            Phone = "+7 (8422) 45-45-45",
                            Latitude = 54.31679,
                            Longitude = 48.40123
                        },
                        new PickupPoint 
                        { 
                            Address = "ул. Рябикова, 70, Ульяновск",
                            WorkingHours = "Пн-Вс: 10:00-21:00",
                            Phone = "+7 (8422) 46-46-46",
                            Latitude = 54.28988,
                            Longitude = 48.32146
                        },
                        // Заволжский район
                        new PickupPoint 
                        { 
                            Address = "пр-т Созидателей, 116, Ульяновск",
                            WorkingHours = "Пн-Вс: 10:00-20:00",
                            Phone = "+7 (8422) 48-48-48",
                            Latitude = 54.3689,
                            Longitude = 48.5824
                        }
                    };

                    // Добавляем новые пункты
                    _context.PickupPoints.AddRange(_pickupPoints);
                    _context.SaveChanges();
                }

                // Проверяем координаты и добавляем маркеры
                foreach (var point in _pickupPoints)
                {
                    // Расширяем границы проверки для Заволжского района
                    if (point.Latitude < 54.2 || point.Latitude > 54.4 ||
                        point.Longitude < 48.2 || point.Longitude > 48.6)
                    {
                        MessageBox.Show($"Предупреждение: Координаты пункта выдачи '{point.Address}' находятся за пределами Ульяновска.\n" +
                            $"Широта: {point.Latitude}, Долгота: {point.Longitude}", 
                            "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                    AddMarker(point);
                }
                
                PickupPointsListBox.ItemsSource = _pickupPoints;

                // Выбираем первый пункт по умолчанию
                if (_pickupPoints.Any())
                {
                    PickupPointsListBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке пунктов выдачи: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddMarker(PickupPoint point)
        {
            try
            {
                var marker = new GMapMarker(new PointLatLng(point.Latitude, point.Longitude));
                
                // Создаем содержимое маркера
                var markerContent = new StackPanel { Margin = new Thickness(5) };
                markerContent.Children.Add(new TextBlock 
                { 
                    Text = point.Address,
                    FontWeight = FontWeights.Bold,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 200
                });
                markerContent.Children.Add(new TextBlock 
                { 
                    Text = point.WorkingHours,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    TextWrapping = TextWrapping.Wrap
                });
                markerContent.Children.Add(new TextBlock 
                { 
                    Text = point.Phone,
                    Foreground = System.Windows.Media.Brushes.Gray
                });

                // Создаем границу для маркера
                var border = new Border
                {
                    Child = markerContent,
                    Background = System.Windows.Media.Brushes.White,
                    BorderBrush = System.Windows.Media.Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(5),
                    CornerRadius = new CornerRadius(3)
                };

                marker.Shape = border;
                marker.Offset = new Point(-10, -10);
                
                _markers[marker] = point;
                MapControl.Markers.Add(marker);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении маркера для {point.Address}: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PickupPointsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedPoint = PickupPointsListBox.SelectedItem as PickupPoint;
            if (selectedPoint != null)
            {
                try
                {
                    // Центрируем карту на выбранной точке
                    MapControl.Position = new PointLatLng(selectedPoint.Latitude, selectedPoint.Longitude);
                    MapControl.Zoom = 15;

                    // Подсвечиваем соответствующий маркер
                    var marker = _markers.FirstOrDefault(m => m.Value == selectedPoint).Key;
                    if (marker != null)
                    {
                        foreach (var m in _markers.Keys)
                        {
                            var border = m.Shape as Border;
                            if (border != null)
                            {
                                border.Background = m == marker 
                                    ? System.Windows.Media.Brushes.LightBlue 
                                    : System.Windows.Media.Brushes.White;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при выборе пункта на карте: {ex.Message}", 
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedPickupPoint = PickupPointsListBox.SelectedItem as PickupPoint;
            if (SelectedPickupPoint != null)
            {
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите пункт выдачи",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _context.Dispose();
        }
    }
} 