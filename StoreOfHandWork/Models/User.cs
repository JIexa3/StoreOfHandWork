using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace StoreOfHandWork.Models
{
    public class User : INotifyPropertyChanged
    {
        private string _name;
        private string _email;
        private string _phone;
        private string _address;
        private string _role;
        private string _status;
        private bool _isAdmin;
        private bool _twoFactorEnabled;
        private bool _emailNotificationsEnabled;
        private bool _smsNotificationsEnabled;
        private bool _isEmailVerified;

        public User()
        {
            Orders = new List<Order>();
            CartItems = new List<CartItem>();
            WishListItems = new List<WishListItem>();
            Reviews = new List<Review>();
            Status = "Активен";
            Role = "User";
            CreatedDate = DateTime.UtcNow;
            LastLoginDate = DateTime.UtcNow;
            IsEmailVerified = false;
            EmailNotificationsEnabled = true;
            SmsNotificationsEnabled = true;
        }

        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Имя обязательно для заполнения")]
        [MaxLength(100)]
        public string Name 
        { 
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        [Required(ErrorMessage = "Email обязателен для заполнения")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        [Column(TypeName = "varchar(256)")]
        [MaxLength(256)]
        public string Email 
        { 
            get => _email;
            set
            {
                if (_email != value)
                {
                    _email = value;
                    OnPropertyChanged();
                }
            }
        }

        [Required(ErrorMessage = "Номер телефона обязателен для заполнения")]
        [Phone(ErrorMessage = "Некорректный формат телефона")]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string Phone 
        { 
            get => _phone;
            set
            {
                if (_phone != value)
                {
                    _phone = value;
                    OnPropertyChanged();
                }
            }
        }

        [Required(ErrorMessage = "Адрес обязателен для заполнения")]
        [MaxLength(500)]
        public string Address 
        { 
            get => _address;
            set
            {
                if (_address != value)
                {
                    _address = value;
                    OnPropertyChanged();
                }
            }
        }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string Role 
        { 
            get => _role;
            set
            {
                if (_role != value)
                {
                    _role = value;
                    OnPropertyChanged();
                }
            }
        }

        [Required]
        [MaxLength(20)]
        public string Status 
        { 
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        [Required]
        public DateTime CreatedDate { get; set; }

        [Required]
        public DateTime LastLoginDate { get; set; }

        public bool IsAdmin
        {
            get => _isAdmin;
            set
            {
                if (_isAdmin != value)
                {
                    _isAdmin = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool TwoFactorEnabled
        {
            get => _twoFactorEnabled;
            set
            {
                if (_twoFactorEnabled != value)
                {
                    _twoFactorEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool EmailNotificationsEnabled
        {
            get => _emailNotificationsEnabled;
            set
            {
                if (_emailNotificationsEnabled != value)
                {
                    _emailNotificationsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool SmsNotificationsEnabled
        {
            get => _smsNotificationsEnabled;
            set
            {
                if (_smsNotificationsEnabled != value)
                {
                    _smsNotificationsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsEmailVerified
        {
            get => _isEmailVerified;
            set
            {
                if (_isEmailVerified != value)
                {
                    _isEmailVerified = value;
                    OnPropertyChanged();
                }
            }
        }

        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<CartItem> CartItems { get; set; }
        public virtual ICollection<WishListItem> WishListItems { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
