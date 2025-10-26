using StoreOfHandWork.Data;
using StoreOfHandWork.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace StoreOfHandWork.Windows
{
    public partial class TagSelectionWindow : Window
    {
        private readonly ApplicationDbContext _context;
        private readonly List<Tag> _existingSelectedTags;
        private ObservableCollection<TagViewModel> _tags = new ObservableCollection<TagViewModel>();

        public List<Tag> SelectedTags { get; private set; }

        public TagSelectionWindow(ApplicationDbContext context, List<Tag> existingSelectedTags)
        {
            InitializeComponent();
            _context = context;
            _existingSelectedTags = existingSelectedTags;
            LoadTags();
            TagsListView.ItemsSource = _tags;
        }

        private void LoadTags()
        {
            var allTags = _context.Tags.OrderBy(t => t.Name).ToList();
            foreach (var tag in allTags)
            {
                _tags.Add(new TagViewModel
                {
                    Id = tag.Id,
                    Name = tag.Name,
                    IsSelected = _existingSelectedTags.Any(t => t.Id == tag.Id)
                });
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedTagViewModels = _tags.Where(t => t.IsSelected).ToList();
            if (selectedTagViewModels.Any())
            {
                // Получаем все ID выбранных тегов
                var selectedTagIds = selectedTagViewModels.Select(t => t.Id).ToList();
                
                // Загружаем все выбранные теги одним запросом из контекста
                // Это обеспечит, что мы используем существующие экземпляры тегов из базы данных
                SelectedTags = _context.Tags
                    .Where(t => selectedTagIds.Contains(t.Id))
                    .ToList();
                
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите хотя бы один тег или нажмите 'Отмена'",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class TagViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsSelected { get; set; }
    }
}
